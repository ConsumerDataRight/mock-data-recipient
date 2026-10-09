using System;
using System.IO;
using CDR.DataRecipient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
            recipientDatabaseContext.Database.Migrate();
            RecreateDatabaseWithForTests(recipientDatabaseContext.Database);

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

        //For Unit Testing only - recreated from https://dev.azure.com/CDR-AU/Participant%20Tooling/_git/sb-mock-data-recipient?path=/Source/CDR.DataRecipient.Repository.SQL/SqlDataAccess.cs&version=GC6249e9a579bbad383425d950b5f3336de3763448&line=1153&lineEnd=1233&lineStartColumn=9&lineEndColumn=14&lineStyle=plain&_a=contents
        public static void RecreateDatabaseWithForTests(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
        {
            // Not clear why drop and recreate scripts were used originally, but assuming it was for a good reason so maintaining this approach over truncation
            //Purging all existing data
            FormattableString sqlQuery = 
                $"""
                IF EXISTS (SELECT * FROM sysobjects WHERE name='CdrArrangement' AND xtype='U') DROP TABLE IF EXISTS CdrArrangement;
                   IF EXISTS (SELECT * FROM sysobjects WHERE name='DataHolderBrand' AND xtype='U') DROP TABLE IF EXISTS DataHolderBrand;
                     IF EXISTS (SELECT * FROM sysobjects WHERE name='Registration' AND xtype='U') DROP TABLE IF EXISTS Registration;
                """;

            database.ExecuteSql(sqlQuery);

            //Create fresh db. 
            sqlQuery = 
                $"""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CdrArrangement' AND xtype='U') CREATE TABLE CdrArrangement ([CdrArrangementId] [uniqueidentifier] NOT NULL, [ClientId] [uniqueidentifier] NOT NULL, JsonDocument VARCHAR(MAX), [UserId] nvarchar(100) NULL);
                     IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DataHolderBrand' AND xtype='U') CREATE TABLE DataHolderBrand ([DataHolderBrandId] [uniqueidentifier] NOT NULL, JsonDocument VARCHAR(MAX));
                       IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Registration' AND xtype='U') CREATE TABLE Registration ([ClientId] [uniqueidentifier] NOT NULL, JsonDocument VARCHAR(MAX), [DataHolderBrandId] [uniqueidentifier] NULL);
                """;

            database.ExecuteSql(sqlQuery);

            //Insert test data
            InsertTestData(database);
        }
        public static void InsertTestData(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
        {
            //Insert data holders data for testing
            var brandId = "cf217aba-e00d-48d5-9c3d-03af0b91cb80";
            var jsonDocument = @"{""_id"":{""$oid"":""613ef59c1a0ee5d9fd426a80""},""DataHolderBrandId"":""cf217aba-e00d-48d5-9c3d-03af0b91cb80"",""BrandName"":""Hall Bank"",""LegalEntity"":{""LegalEntityId"":""924ca498-0f19-402d-ae07-2cb61088f8aa"",""LegalEntityName"":""Hall Bank""},""Status"":""ACTIVE"",""EndpointDetail"":{""Version"":""1"",""PublicBaseUri"":""https://publicapi.hallbank"",""ResourceBaseUri"":""https://api.hallbank"",""InfoSecBaseUri"":""https://idp.hallbank"",""ExtensionBaseUri"":"""",""WebsiteUri"":""https://hallbank/""},""AuthDetails"":[{""RegisterUType"":""SIGNED-JWT"",""JwksEndpoint"":""https://hallbank/idp/jwks""}]}";
            FormattableString sqlQuery = $"INSERT INTO DataHolderBrand (DataHolderBrandId, JsonDocument) VALUES ({brandId}, {jsonDocument})";
            database.ExecuteSql(sqlQuery);
            
            brandId = "cf217aba-e00d-48d5-9c3d-03af0b91cb81";
            jsonDocument = @"{""_id"":{""$oid"":""613ef59c1a0ee5d9fd426a81""},""DataHolderBrandId"":""cf217aba-e00d-48d5-9c3d-03af0b91cb81"",""BrandName"":""Hall Bank"",""LegalEntity"":{""LegalEntityId"":""924ca498-0f19-402d-ae07-2cb61088f8aa"",""LegalEntityName"":""Hall Bank""},""Status"":""ACTIVE"",""EndpointDetail"":{""Version"":""1"",""PublicBaseUri"":""https://publicapi.hallbank"",""ResourceBaseUri"":""https://api.hallbank"",""InfoSecBaseUri"":""https://idp.hallbank"",""ExtensionBaseUri"":"""",""WebsiteUri"":""https://hallbank/""},""AuthDetails"":[{""RegisterUType"":""SIGNED-JWT"",""JwksEndpoint"":""https://hallbank/idp/jwks""}]}";
            sqlQuery = $"INSERT INTO DataHolderBrand (DataHolderBrandId, JsonDocument) VALUES ({brandId}, {jsonDocument})";
            database.ExecuteSql(sqlQuery);

            //Insert cdr arrangements data for testing
            var clientId = "bad06794-39e2-400c-9e1b-f15a0bb67f46";
            var cdrArrangementId = "92d260c1-a625-41e2-a777-c0af1912a74a";
            jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74a"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}";
            sqlQuery = $"INSERT INTO CdrArrangement (CdrArrangementId, ClientId, JsonDocument) VALUES ({cdrArrangementId}, {clientId}, {jsonDocument})";
            database.ExecuteSql(sqlQuery);

            cdrArrangementId = "92d260c1-a625-41e2-a777-c0af1912a74b";
            jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74b"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}";
            sqlQuery = $"INSERT INTO CdrArrangement (CdrArrangementId, ClientId, JsonDocument) VALUES ({cdrArrangementId}, {clientId}, {jsonDocument})";
            database.ExecuteSql(sqlQuery);

            //Insert cdr-registrations data for testing 
            jsonDocument = @"{""_id"":{""$oid"":""6143d52c4433e41a861ea58d""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""ClientIdIssuedAt"":1631835434,""ClientDescription"":""A product to help you manage your budget"",""ClientUri"":""https://mocksoftware/mybudgetapp"",""OrgId"":""ffb1c8ba-279e-44d8-96f0-1bc34a6b436f"",""OrgName"":""Mock Finance Tools"",""RedirectUris"":[""https://localhost:9001/consent/callback""],""LogoUri"":""https://mocksoftware/mybudgetapp/img/logo.png"",""TosUri"":""https://mocksoftware/mybudgetapp/terms"",""PolicyUri"":""https://mocksoftware/mybudgetapp/policy"",""JwksUri"":""https://localhost:9001/jwks"",""RevocationUri"":""https://localhost:9001/revocation"",""RecipientBaseUri"":""https://localhost:9001"",""TokenEndpointAuthSigningAlg"":""PS256"",""TokenEndpointAuthMethod"":""private_key_jwt"",""GrantTypes"":[""client_credentials"",""authorization_code"",""refresh_token""],""ResponseTypes"":[""code id_token""],""ApplicationType"":""web"",""IdTokenSignedResponseAlg"":""PS256"",""IdTokenEncryptedResponseAlg"":""RSA-OAEP"",""IdTokenEncryptedResponseEnc"":""A256GCM"",""RequestObjectSigningAlg"":""PS256"",""SoftwareStatement"":""eyJhbGciOiJQUzI1NiIsImtpZCI6IjU0MkE5QjkxNjAwNDg4MDg4Q0Q0RDgxNjkxNkE5RjQ0ODhERDI2NTEiLCJ0eXAiOiJKV1QifQ.ew0KICAicmVjaXBpZW50X2Jhc2VfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEiLA0KICAibGVnYWxfZW50aXR5X2lkIjogIjE4Yjc1YTc2LTU4MjEtNGM5ZS1iNDY1LTQ3MDkyOTFjZjBmNCIsDQogICJsZWdhbF9lbnRpdHlfbmFtZSI6ICJNb2NrIFNvZnR3YXJlIENvbXBhbnkiLA0KICAiaXNzIjogImNkci1yZWdpc3RlciIsDQogICJpYXQiOiAxNjMxODM1NDE3LA0KICAiZXhwIjogMTYzMTgzNjAxNywNCiAgImp0aSI6ICJjNzYzYjU4NzJkNGY0MzIwOWE3NmUzOTU3YTAzMDgwNCIsDQogICJvcmdfaWQiOiAiZmZiMWM4YmEtMjc5ZS00NGQ4LTk2ZjAtMWJjMzRhNmI0MzZmIiwNCiAgIm9yZ19uYW1lIjogIk1vY2sgRmluYW5jZSBUb29scyIsDQogICJjbGllbnRfbmFtZSI6ICJNeUJ1ZGdldEhlbHBlciIsDQogICJjbGllbnRfZGVzY3JpcHRpb24iOiAiQSBwcm9kdWN0IHRvIGhlbHAgeW91IG1hbmFnZSB5b3VyIGJ1ZGdldCIsDQogICJjbGllbnRfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwIiwNCiAgInJlZGlyZWN0X3VyaXMiOiBbDQogICAgImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvY29uc2VudC9jYWxsYmFjayINCiAgXSwNCiAgImxvZ29fdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL2ltZy9sb2dvLnBuZyIsDQogICJ0b3NfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL3Rlcm1zIiwNCiAgInBvbGljeV91cmkiOiAiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvcG9saWN5IiwNCiAgImp3a3NfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvandrcyIsDQogICJyZXZvY2F0aW9uX3VyaSI6ICJodHRwczovL2xvY2FsaG9zdDo5MDAxL3Jldm9jYXRpb24iLA0KICAic29mdHdhcmVfaWQiOiAiYzYzMjdmODctNjg3YS00MzY5LTk5YTQtZWFhY2QzYmI4MjEwIiwNCiAgInNvZnR3YXJlX3JvbGVzIjogImRhdGEtcmVjaXBpZW50LXNvZnR3YXJlLXByb2R1Y3QiLA0KICAic2NvcGUiOiAib3BlbmlkIHByb2ZpbGUgYmFuazphY2NvdW50cy5iYXNpYzpyZWFkIGJhbms6YWNjb3VudHMuZGV0YWlsOnJlYWQgYmFuazp0cmFuc2FjdGlvbnM6cmVhZCBiYW5rOnBheWVlczpyZWFkIGJhbms6cmVndWxhcl9wYXltZW50czpyZWFkIGNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIGNvbW1vbjpjdXN0b21lci5kZXRhaWw6cmVhZCBjZHI6cmVnaXN0cmF0aW9uIg0KfQ.j_UwVV2g28047YN12KdsGxE3pQwXVkF_ZSCwq7_HLdrlnQKZHsReQCprtxk-MV9vH0EGwpMw46WFQV5pTB-mxwZZfhkQx0-U30ufJfmPwvpxxAI90gFl3MFtQbwgC5a8IkkVfjSUoK1-m-pgG3X79rf0zUB9aRZoSigXgVemKfnQeiB-Gx_TI3zi0QkF1Uw052dAATQvUvaZ040oyqWuTFKETG7AzTV6M1ZcxVJYX5gGhemFIoWA0bVqrP3-dEMUOLFhhFwe3otMMB7iaBfOjBmQ9xtlnnmxFGIGvHErBiHouwfGzG0jCqI5dwtKkicjNKoa4uq-ul3EGup8FWY4Vw"",""SoftwareId"":""c6327f87-687a-4369-99a4-eaacd3bb8210"",""Scope"":""openid profile bank:accounts.basic:read bank:transactions:read common:customer.basic:read cdr:registration""}";
            sqlQuery = $"INSERT INTO Registration (ClientId, JsonDocument) VALUES ({clientId}, {jsonDocument})";
            database.ExecuteSql(sqlQuery);
        }
    }
}
