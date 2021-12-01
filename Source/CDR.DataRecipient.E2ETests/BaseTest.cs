// #define TEST_DEBUG_MODE

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
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
    public class BaseTest
    {
        public bool CreateMedia { get; set; } = true;

        // URL of the web UI
        protected const string WEB_URL = "https://localhost:9001";

        // Connection strings
        static public string DATAHOLDER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolder"]
            ?? throw new Exception($"{nameof(DATAHOLDER_CONNECTIONSTRING)} - configuration setting not found");
        static public string DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolderIdentityServer"]
            ?? throw new Exception($"{nameof(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING)} - configuration setting not found");
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

#if !TEST_DEBUG_MODE
                ViewportSize = new ViewportSize
                {
                    Width = 1200,
                    Height = 1600
                }
#endif              
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
    }
}