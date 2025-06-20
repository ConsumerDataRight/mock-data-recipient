using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataRecipient.API.Logger;
using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.Repository.SQL;
using CDR.DataRecipient.SDK.Extensions;
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

namespace CDR.DataRecipient.Web
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly string _allowSpecificOrigins = "_allowSpecificOrigins";

        private bool healthCheckMigration = false;
        private string healthCheckMigrationMessage = null;

        public Startup(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IOidcSettingsProvider, OidcSettingsProvider>();

            services.AddDbContext<RecipientDatabaseContext>(options => options.UseSqlServer(this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Default)));

            services.AddControllersWithViews().AddRazorRuntimeCompilation()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddSingleton<IServiceConfiguration>(x => new ServiceConfiguration()
            {
                AcceptAnyServerCertificate = this._configuration.IsAcceptingAnyServerCertificate(),
                EnforceHttpsEndpoints = this._configuration.IsEnforcingHttpsEndpoints(),
            });
            services.AddTransient<SDK.Services.Register.IInfosecService, SDK.Services.Register.InfosecService>();
            services.AddTransient<IAccessTokenService, AccessTokenService>();
            services.AddTransient<IMetadataService, MetadataService>();
            services.AddTransient<ISsaService, SsaService>();
            services.AddTransient<IDynamicClientRegistrationService, DynamicClientRegistrationService>();
            services.AddScoped<ISqlDataAccess, SqlDataAccess>();
            services.AddScoped<IDataHoldersRepository, SqlDataHoldersRepository>();
            services.AddScoped<IConsentsRepository, SqlConsentsRepository>();
            services.AddScoped<IRegistrationsRepository, SqlRegistrationsRepository>();
            services.AddTransient<SDK.Services.DataHolder.IInfosecService, SDK.Services.DataHolder.InfosecService>();
            services.AddSingleton<ICacheManager, CacheManager>();
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IDataHolderDiscoveryCache, DataHolderDiscoveryCache>();
            services.AddScoped<LogActionEntryAttribute>();

            // if the distributed cache connection string has been set then use it, otherwise fall back to in-memory caching.
            if (this.UseDistributedCache())
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache);
                    options.InstanceName = "datarecipient-cache-";
                });

                services.AddDataProtection()
                    .SetApplicationName("mdh-idsvr")
                    .PersistKeysToStackExchangeRedis(
                        StackExchange.Redis.ConnectionMultiplexer.Connect(this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache)),
                        "datarecipient-cache-dp-keys");
            }
            else
            {
                // Use in memory cache.
                services.AddDistributedMemoryCache();
            }

            string specificOrigin = this._configuration.GetValue<string>(Constants.ConfigurationKeys.AllowSpecificOrigins);
            services.AddCors(options =>
            {
                options.AddPolicy(
                    this._allowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins(specificOrigin)
                                .SetPreflightMaxAge(TimeSpan.FromSeconds(600))
                                .WithMethods("GET")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                    });
            });

            string connStr = this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Logging);
            services.AddHealthChecks()
                    .AddCheck("migration", () => this.healthCheckMigration ? HealthCheckResult.Healthy(this.healthCheckMigrationMessage) : HealthCheckResult.Unhealthy(this.healthCheckMigrationMessage))
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

            var issuer = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.Issuer);
            if (!string.IsNullOrEmpty(issuer))
            {
                var clientId = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ClientId);
                var clientSecret = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ClientSecret);
                var callbackPath = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.CallbackPath);
                var responseType = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ResponseType);
                var responseMode = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ResponseMode, "form_post");
                var scopes = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.Scope);

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
                    o.Events.OnRedirectToIdentityProvider = context =>
                    {
                        context.Response.Headers.CacheControl = "no-cache,no-store";

                        // Set the client secret dynamically here
                        var clientSecretProvider = context.HttpContext.RequestServices.GetRequiredService<IOidcSettingsProvider>();
                        context.Options.ClientSecret = clientSecretProvider.GetSecret();
                        return Task.CompletedTask;
                    };
                    o.Events.OnRemoteFailure = context =>
                    {
                        string errMessage = context.Failure == null ? string.Empty : context.Failure.Message;
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
                        return Task.CompletedTask;
                    };
                    o.Events.OnAuthenticationFailed = context =>
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
                        return Task.CompletedTask;
                    };
                    o.Events.OnAccessDenied = context =>
                    {
                        string errMessage = context.Result == null ? string.Empty : context.Result.Failure.Message;
                        string redirectError = string.Format("?error_message={0}", errMessage);
                        redirectError = redirectError.Replace("\r\n", "|");

                        string rtnMessage = "oidc/accesserror";
                        rtnMessage += redirectError;
                        context.Response.Redirect(rtnMessage);
                        context.HandleResponse();
                        return Task.CompletedTask;
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

            if (this._configuration.GetSection("SerilogRequestResponseLogger") != null)
            {
                Log.Logger.Information("Adding request response logging middleware");
                services.AddRequestResponseLogging();
            }

            services.AddHttpClient();

            // Different HttpClient is used based on private or public endpoint
            services.AddHttpClient("PublicDataHolderClient", client => { })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = this._configuration.IsAcceptingAnyServerCertificate()
                        ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        : null,
                });

            // Different HttpClient is used based on private or public endpoint
            services.AddHttpClient("PrivateDataHolderClient", client => { })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler();
                    if (this._configuration.IsAcceptingAnyServerCertificate())
                    {
                        handler.SetServerCertificateValidation(this._configuration.IsAcceptingAnyServerCertificate());
                    }

                    // Provide the data recipient's client certificate for a non-public endpoint.
                    handler.ClientCertificates.Add(this._configuration.GetSoftwareProductConfig().ClientCertificate.X509Certificate);
                    return handler;
                });
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
            if (!this._configuration.IsOidcConfigured())
            {
                app.Use(async (ctx, next) =>
                {
                    var claims = new List<Claim>()
                    {
                        new Claim(Constants.Claims.UserId, Constants.LocalAuthentication.UserId),
                        new Claim(ClaimTypes.GivenName, Constants.LocalAuthentication.GivenName),
                        new Claim(ClaimTypes.Surname, Constants.LocalAuthentication.Surname),
                        new Claim(Constants.Claims.Name, string.Concat(Constants.LocalAuthentication.GivenName, " ", Constants.LocalAuthentication.Surname)),
                    };
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, Constants.LocalAuthentication.AuthenticationType));
                    await next();
                });
            }

            // Add HTTP response headers.
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append(
                    "Content-Security-Policy",
                    this._configuration.GetValue(Constants.ConfigurationKeys.ContentSecurityPolicy, "default-src 'self', 'https://cdn.jsdelivr.net/';"));
                await next();
            });

            // Set the request host name.
            var hostname = this._configuration.GetValue<string>(Constants.ConfigurationKeys.MockDataRecipient.Hostname);
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
            app.UseCors(this._allowSpecificOrigins);
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

            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = CustomResponseWriter,
            });

            // Migrate the database to the latest version during application startup.
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                if (this.RunMigrations())
                {
                    this.healthCheckMigrationMessage = "Migration in progress";

                    var optionsBuilder = new DbContextOptionsBuilder<RecipientDatabaseContext>();

                    // Use DBO connection string since it has DBO rights needed to update db schema
                    optionsBuilder.UseSqlServer(this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Migrations)
                        ?? throw new InvalidOperationException($"Connection string '{DbConstants.ConnectionStringNames.Migrations}' not found"));

                    using var dbContext = new RecipientDatabaseContext(optionsBuilder.Options);
                    dbContext.Database.Migrate();

                    this.healthCheckMigrationMessage = "Migration completed";
                }

                this.healthCheckMigration = true;

                // Reconfigure Serilog with DB
                Program.ConfigureSerilog(this._configuration, true);
            }
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
                    value = e.Value.Status.ToString(),
                }),
            });
            return context.Response.WriteAsync(result);
        }

        private bool UseDistributedCache()
        {
            var cacheConnectionString = this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache);
            return !string.IsNullOrEmpty(cacheConnectionString);
        }

        /// <summary>
        /// Determine if EF Migrations should run.
        /// </summary>
        private bool RunMigrations()
        {
            // Run migrations if the DBO connection string is set.
            var dbo = this._configuration.GetConnectionString(DbConstants.ConnectionStringNames.Migrations);
            return !string.IsNullOrEmpty(dbo);
        }
    }
}
