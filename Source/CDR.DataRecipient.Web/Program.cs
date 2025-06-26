using System;
using System.IO;
using System.Security.Authentication;
using CDR.DataRecipient.Web.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Settings.Configuration;

namespace CDR.DataRecipient.Web
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var configuration = BuildConfiguration(args);

            try
            {
                ConfigureSerilog(configuration);
                Serilog.Debugging.SelfLog.Enable(msg => Log.Logger.Debug(msg));
            }
            catch (Exception)
            {
                // Catch and handle exception here if required.
            }

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args, configuration).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                string type = ex.GetType().Name;
                if (type.Equals("StopTheHostException", StringComparison.Ordinal))
                {
                    throw;
                }

                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Configure Serilog logging.
        /// </summary>
        /// <param name="configuration">App configuration.</param>
        /// <param name="isDatabaseReady">Set to True if the database is ready and the MSSqlServer sink will be configured.</param>
        public static void ConfigureSerilog(IConfiguration configuration, bool isDatabaseReady = false)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

            // If the database is ready, configure the SQL Server sink
            if (isDatabaseReady)
            {
                loggerConfiguration.ReadFrom.Configuration(configuration, new ConfigurationReaderOptions() { SectionName = "SerilogMSSqlServerWriteTo" });
            }

            Log.Logger = loggerConfiguration.CreateLogger();
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    _ = BuildConfiguration(args, builder);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel((context, serverOptions) =>
                    {
                        serverOptions.Configure(context.Configuration.GetSection("Kestrel"))
                                        .Endpoint("HTTPS", listenOptions =>
                                        {
                                            listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;
                                        });

                        serverOptions.ConfigureHttpsDefaults(options =>
                        {
                            options.SslProtocols = SslProtocols.Tls12;
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                });

        private static IConfigurationRoot BuildConfiguration(string[] args, IConfigurationBuilder builder = null)
        {
            var configurationCommandLine = new ConfigurationBuilder()
                .AddCommandLine(args).Build();

            builder ??= new ConfigurationBuilder();

            builder.AddCommandLine(args)
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? configurationCommandLine.GetValue<string>("environment")}.json", true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            var secretVolume = configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.SecretVolumePath);

            // if the volume mount configured add this as well to the configuration to look for secrets.
            if (Directory.Exists(secretVolume))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), secretVolume);
                builder.AddKeyPerFile(path, optional: true, true);
                configuration = builder.Build();
            }

            return configuration;
        }
    }
}
