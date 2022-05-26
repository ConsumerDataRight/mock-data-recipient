#undef DEPRECATED  // instead see BaseTest_v2
#if DEPRECATED

// #define TEST_DEBUG_MODE // Run Playwright in non-headless mode for debugging purposes (ie show a browser)

// In docker (Ubuntu container) Playwright will fail if running in non-headless mode, so we ensure TEST_DEBUG_MODE is undef'ed
#if !DEBUG
#undef TEST_DEBUG_MODE
#endif

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("E2ETests")]
    [TestCaseOrderer("CDR.DataRecipient.E2ETests.XUnit.Orderers.AlphabeticalOrderer", "CDR.DataRecipient.E2ETests")]
    [DisplayTestMethodName]
    public class BaseTest
    {
        // Customers
        public const string CUSTOMERID_BANKING = "jwilson";
        public const string CUSTOMERACCOUNTS_BANKING = "Personal Loan xxx-xxx xxxxx987,Transactions and Savings Account xxx-xxx xxxxx988";
        public const string CUSTOMERID_ENERGY = "mmoss";
        public const string CUSTOMERACCOUNTS_ENERGY = "'ELECTRICITY ACCOUNT','ELECTRICITY ACCOUNT 2',ELECTRICITY ACCOUNT 3,ELECTRICITY ACCOUNT 4,ELECTRICITY ACCOUNT 5,ELECTRICITY ACCOUNT 6,ELECTRICITY ACCOUNT 7,ELECTRICITY ACCOUNT 8,ELECTRICITY ACCOUNT 9,ELECTRICITY ACCOUNT 10,ELECTRICITY ACCOUNT 11,ELECTRICITY ACCOUNT 12,ELECTRICITY ACCOUNT 13,ELECTRICITY ACCOUNT 14,ELECTRICITY ACCOUNT 15,ELECTRICITY ACCOUNT 16,ELECTRICITY ACCOUNT 17,ELECTRICITY ACCOUNT 18,ELECTRICITY ACCOUNT 19,ELECTRICITY ACCOUNT 20,ELECTRICITY ACCOUNT 21";

        // Data Holder
        public const string DH_BRANDID = "804fc2fb-18a7-4235-9a49-2af393d18bc7";
        public const string DH_BRANDID_ENERGY = "cfcaf0df-401b-47f2-98af-94787289eca8"; // Mock Data Holder (Energy)

        // Data Recipient
        public const string DR_BRANDID = "ffb1c8ba-279e-44d8-96f0-1bc34a6b436f";
        public const string DR_SOFTWAREPRODUCTID = "c6327f87-687a-4369-99a4-eaacd3bb8210";

        // URLs        
        static public string REGISTER_MTLS_BaseURL => Configuration["MTLS_BaseURL"]
            ?? throw new Exception($"{nameof(REGISTER_MTLS_BaseURL)} - configuration setting not found");

        static public string REGISTER_IDENTITYSERVER_URL = REGISTER_MTLS_BaseURL + "/idp/connect/token";

        // Client certificates
        protected const string CERTIFICATE_FILENAME = "Certificates/client.pfx";
        protected const string CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        public const string JWT_CERTIFICATE_FILENAME = "Certificates/jwks.pfx";
        public const string JWT_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";


        public bool CreateMedia { get; set; } = true;

        // Test settings.
        static public bool CREATE_MEDIA => Configuration.GetValue<bool>("CreateMedia", true);
        static public int TEST_TIMEOUT => Configuration.GetValue<int>("TestTimeout", 30000);

        // URL of the web UI
        static public string WEB_URL => Configuration["Web_URL"]
            ?? throw new Exception($"{nameof(WEB_URL)} - configuration setting not found");

        // Hostnames
        static public string HOSTNAME_REGISTER => Configuration["Hostnames:Register"]
            ?? throw new Exception($"{nameof(HOSTNAME_REGISTER)} - configuration setting not found");
        static public string HOSTNAME_DATAHOLDER => Configuration["Hostnames:DataHolder"]
            ?? throw new Exception($"{nameof(HOSTNAME_DATAHOLDER)} - configuration setting not found");
        static public string HOSTNAME_DATAHOLDER_ENERGY => Configuration["Hostnames:DataHolderEnergy"]
            ?? throw new Exception($"{nameof(HOSTNAME_DATAHOLDER_ENERGY)} - configuration setting not found");
        static public string HOSTNAME_DATARECIPIENT => Configuration["Hostnames:DataRecipient"]
            ?? throw new Exception($"{nameof(HOSTNAME_DATARECIPIENT)} - configuration setting not found");

        // Connection strings
        static public string DATAHOLDER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolder"]
            ?? throw new Exception($"{nameof(DATAHOLDER_CONNECTIONSTRING)} - configuration setting not found");
        static public string DATAHOLDER_ENERGY_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolderEnergy"]
            ?? throw new Exception($"{nameof(DATAHOLDER_ENERGY_CONNECTIONSTRING)} - configuration setting not found");
        static public string DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolderIdentityServer"]
            ?? throw new Exception($"{nameof(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING)} - configuration setting not found");
        static public string DATAHOLDER_ENERGY_IDENTITYSERVER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolderEnergyIdentityServer"]
            ?? throw new Exception($"{nameof(DATAHOLDER_ENERGY_IDENTITYSERVER_CONNECTIONSTRING)} - configuration setting not found");
        static public string REGISTER_CONNECTIONSTRING => Configuration["ConnectionStrings:Register"]
            ?? throw new Exception($"{nameof(REGISTER_CONNECTIONSTRING)} - configuration setting not found");
        static public string DATARECIPIENT_CONNECTIONSTRING => Configuration["ConnectionStrings:DataRecipient"]
            ?? throw new Exception($"{nameof(DATARECIPIENT_CONNECTIONSTRING)} - configuration setting not found");

        // Media folder (for videos and screenshots)
        static public string MEDIAFOLDER => Configuration["MediaFolder"]
            ?? throw new Exception($"{nameof(MEDIAFOLDER)} - configuration setting not found");

        // Dataholder - Access token lifetime seconds
        static public string ACCESSTOKENLIFETIMESECONDS => Configuration["DataHolder:AccessTokenLifetimeSeconds"]
            ?? throw new Exception($"{nameof(ACCESSTOKENLIFETIMESECONDS)} - configuration setting not found");

        static private IConfigurationRoot? configuration;
        static protected IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .Build();
                }

                return configuration;
            }
        }

        protected BaseTest()
        {
            // Default from config.
            this.CreateMedia = CREATE_MEDIA;
        }

        private bool inArrange = false;
        protected delegate Task ArrangeDelegate();
        protected async Task Arrange(ArrangeDelegate arrange)
        {
            if (inArrange)
                return;

            inArrange = true;
            try
            {
                CreateMedia = false;
                try
                {
                    // PatchRegister(); // No longer needed - seed data is correct
                    PurgeMDR();
                    PurgeMDHIdentityServer();
                    PurgeMDHEnergyIdentityServer();
                    PurgeMDH();
                    PurgeMDHE();
                    await arrange();
                }
                finally
                {
                    CreateMedia = CREATE_MEDIA;
                }
            }
            finally
            {
                inArrange = false;
            }
        }

        private bool inCleanup = false;
        protected delegate Task CleanupDelegate(IPage page);
        protected async Task CleanupAsync(CleanupDelegate cleanup)
        {
            if (inArrange) // shouldn't cleanup if arranging
                return;

            if (inCleanup)
                return;

            inCleanup = true;
            try
            {
                // Setup Playwright
                using var playwright = await Playwright.CreateAsync();

                // Setup browser
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    SlowMo = 250,
#if TEST_DEBUG_MODE
                    Headless = false,
                    Timeout = 5000 // DEBUG - 5 seconds
#endif
                });

                // Setup browser context
                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    IgnoreHTTPSErrors = true,
                    RecordVideoDir = null,
                    ViewportSize = new ViewportSize
                    {
                        Width = 1200,
                        Height = 1600
                    }
                });

                try
                {
                    var page = await context.NewPageAsync();
                    page.Close += async (_, page) =>
                    {
                    };

                    using (new AssertionScope())
                    {
                        page.SetDefaultTimeout(TEST_TIMEOUT);
                        await cleanup(page);
                    }
                }
                finally
                {
                    await context.CloseAsync();
                    await browser.CloseAsync();
                }
            }
            finally
            {
                inCleanup = false;
            }
        }

        protected delegate Task TestDelegate(IPage page);
        protected async Task TestAsync(string testName, TestDelegate testDelegate) //, bool? CreateMedia = true)
        {
            _testName = testName;

            static void DeleteFile(string filename)
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }

            if (CreateMedia == true)
            {
                // Remove video/screens if they exist
                DeleteFile($"{MEDIAFOLDER}/{testName}.webm");
                DeleteFile($"{MEDIAFOLDER}/{testName}.png");
                DeleteFile($"{MEDIAFOLDER}/{testName}-exception.png");
            }

            // Setup Playwright
            using var playwright = await Playwright.CreateAsync();

            // Setup browser
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                SlowMo = 250,
#if TEST_DEBUG_MODE                
                Headless = false,
                Timeout = 5000 // DEBUG - 5 seconds
#endif                
            });

            // Setup browser context
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                RecordVideoDir = CreateMedia == true ? $"{MEDIAFOLDER}" : null,

                //#if !TEST_DEBUG_MODE
                ViewportSize = new ViewportSize
                {
                    Width = 1200,
                    Height = 1600
                }
                //#endif              
            });

            string? videoPath = null;
            try
            {
                var page = await context.NewPageAsync();
                try
                {
                    page.Close += async (_, page) =>
                    {
                        // Page is closed, so save videoPath
                        if (CreateMedia == true)
                        {
                            if (page.Video != null)
                            {
                                videoPath = await page.Video.PathAsync();
                            }
                        }
                    };

                    using (new AssertionScope())
                    {
                        page.SetDefaultTimeout(TEST_TIMEOUT);
                        await testDelegate(page);
                    }
                }
                finally
                {
                    // Save a screenshot
                    if (CreateMedia == true)
                    {
                        await ScreenshotAsync(page, "");
                    }
                }
            }
            finally
            {
                // Wait 1 second so that video captures final state of page
                await Task.Delay(1000);

                await context.CloseAsync();

                // Rename video file
                if (CreateMedia == true)
                {
                    if (videoPath != null)
                    {
                        File.Move(videoPath, $"{MEDIAFOLDER}/{testName}.webm");
                    }
                }

                await browser.CloseAsync();
            }
        }

        private string? _testName;
        public async Task ScreenshotAsync(IPage page, string name)
        {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{MEDIAFOLDER}/{_testName}{name}.png" });
        }

        protected static string? StripJsonProperty(string? json, string propertyName)
        {
            if (String.IsNullOrEmpty(json))
                return json;

            return Regex.Replace(json, @$"""{propertyName}"".*", "");
        }

        protected static void Assert_Json(string? expectedJson, string? actualJson)
        {
            static object? Deserialize(string json)
            {
                try { return JsonConvert.DeserializeObject<object>(json); }
                catch { return null; }
            }

            static bool JsonCompare(string json, string jsonToCompare)
            {
                var jsonToken = JToken.Parse(json);
                var jsonToCompareToken = JToken.Parse(jsonToCompare);
                return JToken.DeepEquals(jsonToken, jsonToCompareToken);
            }

            expectedJson.Should().NotBeNullOrEmpty();
            actualJson.Should().NotBeNullOrEmpty(expectedJson == null ? "" : $"expected {expectedJson}");

            if (string.IsNullOrEmpty(expectedJson) || string.IsNullOrEmpty(actualJson))
                return;

            object? expectedObject = Deserialize(expectedJson);
            expectedObject.Should().NotBeNull($"Error deserializing expected json - '{expectedJson}'");

            object? actualObject = Deserialize(actualJson);
            actualObject.Should().NotBeNull($"Error deserializing actual json - '{actualJson}'");

            var expectedJsonNormalised = JsonConvert.SerializeObject(expectedObject);
            var actualJsonNormalised = JsonConvert.SerializeObject(actualObject);

            JsonCompare(actualJson, expectedJson).Should().BeTrue(
                $"\r\nExpected json:\r\n{expectedJsonNormalised}\r\nActual Json:\r\n{actualJsonNormalised}\r\n"
            );
        }

        protected const string ASSERT_JSON2_ANYVALUE = "***ANYVALUE***";
        protected static void Assert_Json2(string? actualJson, (string name, string? value)[] expected)
        {
            actualJson.Should().NotBeNullOrEmpty();
            if (actualJson == null)
                return;

            var root = JToken.Parse(actualJson);

            foreach ((var name, var value) in expected)
            {
                // Assert that json property exists
                root[name].Should().NotBeNull($"Missing property '{name}'");

                // Assert that value matches
                if (value != ASSERT_JSON2_ANYVALUE)
                {
                    root.Value<string>(name).Should().Be(value, $"Property '{name}' should be '{value}'");
                }
            }
        }

        public static void PatchRegister()
        {
            using var connection = new SqlConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            // mock-data-recipient
            using var updateCommand = new SqlCommand($@"
                    update 
                        softwareproduct
                    set 
                        recipientbaseuri = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001',
                        revocationuri = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/revocation',
                        redirecturis = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/consent/callback',
                        jwksuri = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/jwks'
                    where 
                        softwareproductid = '{BaseTest.DR_SOFTWAREPRODUCTID}'",
                connection);
            updateCommand.ExecuteNonQuery();

            // mock-data-holder
            using var updateCommand2 = new SqlCommand($@"
                    update
                        endpoint
                    set 
                        publicbaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER}:8000',
                        resourcebaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER}:8002',
                        infosecbaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER}:8001'
                    where 
                        brandid = '{BaseTest.DH_BRANDID}'",
                connection);
            updateCommand2.ExecuteNonQuery();

            // mock-data-holder-energy
            using var updateCommand3 = new SqlCommand($@"
                    update
                        endpoint
                    set 
                        publicbaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER_ENERGY}:8100',
                        resourcebaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER_ENERGY}:8102',
                        infosecbaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER_ENERGY}:8101'
                    where 
                        brandid = '{BaseTest.DH_BRANDID_ENERGY}'",
                connection);
            updateCommand3.ExecuteNonQuery();
        }

        static void Purge(SqlConnection connection, string table)
        {
            // Delete all rows from table
            using var deleteCommand = new SqlCommand($"delete from {table}", connection);
            deleteCommand.ExecuteNonQuery();

            // Check all rows deleted
            using var selectCommand = new SqlCommand($"select count(*) from {table}", connection);
            var count = Convert.ToInt32(selectCommand.ExecuteScalar());
            if (count != 0)
            {
                throw new Exception($"Error purging {table}");
            }
        }

        public static void PurgeMDR()
        {
            using var mdrConnection = new SqlConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
            mdrConnection.Open();

            Purge(mdrConnection, "CdrArrangement");
            Purge(mdrConnection, "DataHolderBrand");
            Purge(mdrConnection, "Registration");
        }

        public static void PurgeMDH()
        {
            // using var mdhConnection = new SqlConnection(BaseTest.DATAHOLDER_CONNECTIONSTRING);
            // mdhConnection.Open();
            // Purge(mdhConnection, "Brand");
            // Purge(mdhConnection, "SoftwareProduct");
        }

        public static void PurgeMDHE()
        {
            // using var mdheConnection = new SqlConnection(BaseTest.DATAHOLDER_ENERGY_CONNECTIONSTRING);
            // mdheConnection.Open();
            // Purge(mdheConnection, "Brand");
            // Purge(mdheConnection, "SoftwareProduct");
        }

        // public static void PurgeMDR_CDRArrangements()
        // {
        //     using var mdrConnection = new SqlConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
        //     mdrConnection.Open();
        //     Purge(mdrConnection, "CdrArrangement");
        // }

        private static void PurgeIdentityServer(string connectionString)
        {
            using var identityServerConnection = new SqlConnection(connectionString);
            identityServerConnection.Open();

            // Purge(identityServerConnection, "ApiResourceClaims");
            // Purge(identityServerConnection, "ApiResourceProperties");
            // Purge(identityServerConnection, "ApiResources");
            // Purge(identityServerConnection, "ApiResourceScopes");
            // Purge(identityServerConnection, "ApiResourceSecrets");
            // Purge(identityServerConnection, "ApiScopeClaims");
            // Purge(identityServerConnection, "ApiScopeProperties");
            // Purge(identityServerConnection, "ApiScopes");
            Purge(identityServerConnection, "ClientClaims");
            Purge(identityServerConnection, "ClientCorsOrigins");
            Purge(identityServerConnection, "ClientGrantTypes");
            Purge(identityServerConnection, "ClientIdPRestrictions");
            Purge(identityServerConnection, "ClientPostLogoutRedirectUris");
            Purge(identityServerConnection, "ClientProperties");
            Purge(identityServerConnection, "ClientRedirectUris");
            Purge(identityServerConnection, "Clients");
            Purge(identityServerConnection, "ClientScopes");
            Purge(identityServerConnection, "ClientSecrets");
            // Purge(identityServerConnection, "DeviceCodes");
            // Purge(identityServerConnection, "IdentityResourceClaims");
            // Purge(identityServerConnection, "IdentityResourceProperties");
            // Purge(identityServerConnection, "IdentityResources");
            Purge(identityServerConnection, "PersistedGrants");
        }

        public static void PurgeMDHIdentityServer()
        {
            PurgeIdentityServer(BaseTest.DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING);
        }

        public static void PurgeMDHEnergyIdentityServer()
        {
            PurgeIdentityServer(BaseTest.DATAHOLDER_ENERGY_IDENTITYSERVER_CONNECTIONSTRING);
        }
    }
}

#endif