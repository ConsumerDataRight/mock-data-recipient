using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Services.Tokens;
using CDR.DataRecipient.Web.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Repository.SQLite;
using CDR.DataRecipient.Web.Middleware;

namespace CDR.DataRecipient.Web
{
    public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;

            // Force database to be created early since it's needed by integration tests for arrangement
            _ = new SqliteDataAccess(Configuration);
		}

		public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddTransient<SDK.Services.Register.IInfosecService, SDK.Services.Register.InfosecService>();
            services.AddTransient<IAccessTokenService, AccessTokenService>();
            services.AddTransient<IMetadataService, MetadataService>();
            services.AddTransient<ISsaService, SsaService>();
            services.AddTransient<IDynamicClientRegistrationService, DynamicClientRegistrationService>();
			services.AddSingleton<ISqliteDataAccess>(x => new SqliteDataAccess(Configuration));
			services.AddSingleton<IDataHoldersRepository>(x => new SqliteDataHoldersRepository(Configuration));
			services.AddSingleton<IConsentsRepository>(x => new SqliteConsentsRepository(Configuration));
			services.AddSingleton<IRegistrationsRepository>(x => new SqliteRegistrationsRepository(Configuration));
			services.AddTransient<SDK.Services.DataHolder.IInfosecService, SDK.Services.DataHolder.InfosecService>();
            services.AddMemoryCache();
			services.AddSingleton<IDataHolderDiscoveryCache, DataHolderDiscoveryCache>();			
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
			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			// Custom Client autorise middleware           
			app.UseMiddleware<ClientAuthorizationMiddleware>(); 

			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/data-sharing/swagger", "CDS Swagger");
				c.InjectStylesheet("/css/swagger.css");
				c.InjectJavascript("/js/swagger.js");
				c.UseRequestInterceptor("(request) => { return AppendCdrArrangementIdToRequest(request); }");
				c.RoutePrefix = "cds";
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
