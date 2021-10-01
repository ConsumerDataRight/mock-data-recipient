using System;
using System.IO;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.Repository.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace CDR.DataRecipient.Repository.SQLite.UnitTests
{
    public class SqliteDataFixture
    {
        public IServiceProvider ServiceProvider { get; set; }

        public SqliteDataFixture()
        {            
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .Build();
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var services = new ServiceCollection();

            var connectionStr = configuration.GetConnectionString("DefaultConnection");

            Log.Logger.Information($"Sqlite Db ConnectionString: {connectionStr}");

            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            services.AddSingleton<ISqliteDataAccess>(x => new SqliteDataAccess(configuration));
            services.AddSingleton<IDataHoldersRepository>(x => new SqliteDataHoldersRepository(configuration));
            services.AddSingleton<IConsentsRepository>(x => new SqliteConsentsRepository(configuration));
            services.AddSingleton<IRegistrationsRepository>(x => new SqliteRegistrationsRepository(configuration));

            this.ServiceProvider = services.BuildServiceProvider();

            var loggerFactory = this.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("UnitTests");

            loggerFactory.AddSerilog();
        }
    }
}
