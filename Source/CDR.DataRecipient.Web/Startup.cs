using CDR.DataRecipient.API.Logger;
using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.Repository.SQL;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.SDK.Services.Tokens;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private string AllowSpecificOrigins = "_allowSpecificOrigins";

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<RecipientDatabaseContext>(options => options.UseSqlServer(_configuration.GetConnectionString(DbConstants.ConnectionStringNames.Default)));
            var dbContext = services.BuildServiceProvider().GetService<RecipientDatabaseContext>();

            services.AddControllersWithViews().AddRazorRuntimeCompilation()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddSingleton<IServiceConfiguration>(x => new ServiceConfiguration() { 
                AcceptAnyServerCertificate = _configuration.IsAcceptingAnyServerCertificate(),
                EnforceHttpsEndpoints = _configuration.IsEnforcingHttpsEndpoints()
            });
            services.AddTransient<SDK.Services.Register.IInfosecService, SDK.Services.Register.InfosecService>();
            services.AddTransient<IAccessTokenService, AccessTokenService>();
            services.AddTransient<IMetadataService, MetadataService>();
            services.AddTransient<ISsaService, SsaService>();
            services.AddTransient<IDynamicClientRegistrationService, DynamicClientRegistrationService>();
            services.AddSingleton<ISqlDataAccess>(x => new SqlDataAccess(_configuration, dbContext));
            services.AddSingleton<IDataHoldersRepository>(x => new SqlDataHoldersRepository(_configuration, dbContext));
            services.AddSingleton<IConsentsRepository>(x => new SqlConsentsRepository(_configuration, dbContext));
            services.AddSingleton<IRegistrationsRepository>(x => new SqlRegistrationsRepository(_configuration, dbContext));
            services.AddTransient<SDK.Services.DataHolder.IInfosecService, SDK.Services.DataHolder.InfosecService>();
            services.AddSingleton<ICacheManager, CacheManager>();
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IDataHolderDiscoveryCache, DataHolderDiscoveryCache>();
            services.AddScoped<LogActionEntryAttribute>();

            // if the distributed cache connection string has been set then use it, otherwise fall back to in-memory caching.
            if (UseDistributedCache())
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache);
                    options.InstanceName = "datarecipient-cache-";
                });

                services.AddDataProtection()
                    .SetApplicationName("mdh-idsvr")
                    .PersistKeysToStackExchangeRedis(
                        StackExchange.Redis.ConnectionMultiplexer.Connect(_configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache)),
                        "datarecipient-cache-dp-keys");
            }
            else
            {
                // Use in memory cache.
                services.AddDistributedMemoryCache();
            }

            string specificOrigin = _configuration.GetValue<string>(Constants.ConfigurationKeys.AllowSpecificOrigins);
            services.AddCors(options =>
            {
                options.AddPolicy(AllowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins(specificOrigin)
                            .SetPreflightMaxAge(TimeSpan.FromSeconds(600))
                            .WithMethods("GET")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                });
            });

            string connStr = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Logging);
            services.AddHealthChecks()
                    .AddCheck("sql-connection", () =>
                    {
                        using (var db = new SqlConnection(connStr))
                        {
                            try
                            {
                                db.Open();
                            }
                            catch (SqlException)
                            {
                                return HealthCheckResult.Unhealthy();
                            }
                        }
                        return HealthCheckResult.Healthy();
                    });

            var issuer = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.Issuer);
            if (!string.IsNullOrEmpty(issuer))
            {
                var clientId = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ClientId);
                var clientSecret = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ClientSecret);
                var callbackPath = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.CallbackPath);
                var responseType = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ResponseType);
                var responseMode = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ResponseMode, "form_post");
                var scopes = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.Scope);

                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(o =>
                {
                    o.Authority = issuer;
                    o.ClientId = clientId;
                    o.ClientSecret = clientSecret;
                    o.ResponseType = responseType;
                    o.ResponseMode = responseMode;
                    o.CallbackPath = callbackPath;
                    o.GetClaimsFromUserInfoEndpoint = false;
                    o.Events.OnRemoteFailure = async context =>
                    {
                        string errMessage = context.Failure == null ? "" : context.Failure.Message;
                        string innerErrorMessage = string.Empty;
                        string redirectError = string.Format("?error_message={0}", errMessage);
                        if (context.Failure.InnerException != null)
                        {
                            innerErrorMessage = context.Failure.InnerException.Message;
                            redirectError = string.Format("{0}&inner_error={1}", redirectError, innerErrorMessage);
                        }
                        redirectError = redirectError.Replace("\r\n", "|");

                        string rtnMessage = "oidc/remoteerror";
                        rtnMessage += redirectError;
                        context.Response.Redirect(rtnMessage);
                        context.HandleResponse();
                    };
                    o.Events.OnAuthenticationFailed = async context =>
                    {
                        string errMessage = context.Exception.Message;
                        string innerErrorMessage = string.Empty;
                        string redirectError = string.Format("?error_message={0}", errMessage);
                        if (context.Exception.InnerException != null)
                        {
                            innerErrorMessage = context.Exception.InnerException.Message;
                            redirectError = string.Format("{0}&inner_error={1}", redirectError, innerErrorMessage);
                        }
                        redirectError = redirectError.Replace("\r\n", "|");

                        string rtnMessage = "oidc/autherror";
                        rtnMessage += redirectError;
                        context.Response.Redirect(rtnMessage);
                        context.HandleResponse();
                    };
                    o.Events.OnAccessDenied = async context =>
                    {
                        string errMessage = context.Result == null ? "" : context.Result.Failure.Message;
                        string redirectError = string.Format("?error_message={0}", errMessage);
                        redirectError = redirectError.Replace("\r\n", "|");

                        string rtnMessage = "oidc/accesserror";
                        rtnMessage += redirectError;
                        context.Response.Redirect(rtnMessage);
                        context.HandleResponse();
                    };

                    foreach (var scope in scopes.Split(' '))
                    {
                        o.Scope.Add(scope);
                    }
                });
            }

            services.AddSession(o =>
            {
                o.Cookie.Name = "mdr";
                o.Cookie.SameSite = SameSiteMode.None;
                o.Cookie.HttpOnly = true;
                o.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            services.AddFeatureManagement();

            if (_configuration.GetSection("SerilogRequestResponseLogger") != null)
            {
                Log.Logger.Information("Adding request response logging middleware");
                services.AddRequestResponseLogging();
            }
        }

        private bool UseDistributedCache()
        {
            var cacheConnectionString = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache);
            return !string.IsNullOrEmpty(cacheConnectionString);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // If an external IdP is not configured, then create a dummy mdr user.
            if (!_configuration.IsOidcConfigured())
            {
                app.Use(async (ctx, next) =>
                {
                    var claims = new List<Claim>() {
                        new Claim(Constants.Claims.UserId, Constants.LocalAuthentication.UserId),
                        new Claim(ClaimTypes.GivenName, Constants.LocalAuthentication.GivenName),
                        new Claim(ClaimTypes.Surname, Constants.LocalAuthentication.Surname),
                        new Claim(Constants.Claims.Name, string.Concat(Constants.LocalAuthentication.GivenName, " ", Constants.LocalAuthentication.Surname))
                    };
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, Constants.LocalAuthentication.AuthenticationType));
                    await next();
                });
            }

            // Add HTTP response headers.
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-Security-Policy", _configuration.GetValue(Constants.ConfigurationKeys.ContentSecurityPolicy, "default-src 'self', 'https://cdn.jsdelivr.net/';"));
                await next();
            });

            // Set the request host name.
            var hostname = _configuration.GetValue<string>(Constants.ConfigurationKeys.MockDataRecipient.Hostname);
            if (!string.IsNullOrEmpty(hostname))
            {
                app.Use((context, next) =>
                {
                    context.Request.Host = new HostString(hostname);
                    return next(context);
                });
            }

            app.UseSerilogRequestLogging();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(AllowSpecificOrigins);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            // Custom Client autorise middleware           
            app.UseMiddleware<ClientAuthorizationMiddleware>();

            // Common swagger.
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "cds-common";
                c.SwaggerEndpoint("/data-sharing-common/swagger", "CDS Common Swagger");
                c.InjectStylesheet("/css/swagger.css");
                c.InjectJavascript("/js/swagger.js");
                c.UseRequestInterceptor("(request) => { return AppendCdrArrangementIdToRequest(request); }");
            });

            // Banking swagger.
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "cds-banking";
                c.SwaggerEndpoint("/data-sharing-banking/swagger", "CDS Banking Swagger");
                c.InjectStylesheet("/css/swagger.css");
                c.InjectJavascript("/js/swagger.js");
                c.UseRequestInterceptor("(request) => { return AppendCdrArrangementIdToRequest(request); }");
            });

            // Energy swagger.
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "cds-energy";
                c.SwaggerEndpoint("/data-sharing-energy/swagger", "CDS Energy Swagger");
                c.InjectStylesheet("/css/swagger.css");
                c.InjectJavascript("/js/swagger.js");
                c.UseRequestInterceptor("(request) => { return AppendCdrArrangementIdToRequest(request); }");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            var options = new HealthCheckOptions();
            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = CustomResponseWriter
            });

            // Migrate the database to the latest version during application startup.
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                const string HEALTHCHECK_READY_FILENAME = "_healthcheck_ready"; // MJS - Should be using ASPNet health check, not a file
                File.Delete(HEALTHCHECK_READY_FILENAME);

                if (RunMigrations())
                {
                    var optionsBuilder = new DbContextOptionsBuilder<RecipientDatabaseContext>();

                    // Use DBO connection string since it has DBO rights needed to update db schema
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString(DbConstants.ConnectionStringNames.Migrations)
                        ?? throw new Exception($"Connection string '{DbConstants.ConnectionStringNames.Migrations}' not found"));

                    using var dbContext = new RecipientDatabaseContext(optionsBuilder.Options);
                    dbContext.Database.Migrate();
                }

                File.WriteAllText(HEALTHCHECK_READY_FILENAME, "");  // Create file to indicate MDR is ready, this can be used by Docker/Dockercompose health checks // MJS - Should be using ASPNet health check, not a file
            }
            

        }

        /// <summary>
        /// Determine if EF Migrations should run.
        /// </summary>
        private bool RunMigrations()
        {
            // Run migrations if the DBO connection string is set.
            var dbo = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Migrations);
            return !string.IsNullOrEmpty(dbo);
        }


        private static Task CustomResponseWriter(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json";
            var result = JsonConvert.SerializeObject(new
            {
                status = healthReport.Status.ToString(),
                errors = healthReport.Entries.Select(e => new
                {
                    key = e.Key,
                    value = e.Value.Status.ToString()
                })
            });
            return context.Response.WriteAsync(result);
        }
    }
}