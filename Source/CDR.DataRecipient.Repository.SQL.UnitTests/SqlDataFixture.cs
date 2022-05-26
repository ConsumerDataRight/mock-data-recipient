using CDR.DataRecipient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;

namespace CDR.DataRecipient.Repository.SQL.UnitTests
{
    public class SqlDataFixture
    {
        public IServiceProvider ServiceProvider { get; set; }

        public SqlDataFixture()
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

            var connectionStr = configuration.GetConnectionString("DataRecipient_DB");

            var recipientDatabaseContext = CreateDbContext(connectionStr);

            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            services.AddSingleton<ISqlDataAccess>(x => new SqlDataAccess(configuration, recipientDatabaseContext));
            services.AddSingleton<IDataHoldersRepository>(x => new SqlDataHoldersRepository(configuration, recipientDatabaseContext));
            services.AddSingleton<IConsentsRepository>(x => new SqlConsentsRepository(configuration, recipientDatabaseContext));
            services.AddSingleton<IRegistrationsRepository>(x => new SqlRegistrationsRepository(configuration, recipientDatabaseContext));

            this.ServiceProvider = services.BuildServiceProvider();

            var loggerFactory = this.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("UnitTests");

            loggerFactory.AddSerilog();
        }

        public static RecipientDatabaseContext CreateDbContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<RecipientDatabaseContext>()
                .UseSqlServer(connectionString) // connection string is only needed if using "dotnet ef database update ..." to actually run migrations from commandline
                .Options;

            return new RecipientDatabaseContext(options);
        }
    }
}
