using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("E2ETests")]
    [TestCaseOrderer("CDR.DataRecipient.E2ETests.XUnit.Orderers.AlphabeticalOrderer", "CDR.DataRecipient.E2ETests")]
    public class BaseTest
    {
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
        protected static async Task TestAsync(string testName, TestDelegate testDelegate)
        {
            static void DeleteFile(string filename)
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }

            // Remove video/screens if they exist
            DeleteFile($"{MEDIAFOLDER}/{testName}.webm");
            DeleteFile($"{MEDIAFOLDER}/{testName}.png");
            DeleteFile($"{MEDIAFOLDER}/{testName}-exception.png");

            // Setup Playwright
            using var playwright = await Playwright.CreateAsync();

            // Setup browser
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                // Headless = false,
                // SlowMo = 250,
            });

            // Setup browser context
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                RecordVideoDir = $"{MEDIAFOLDER}",
                // RecordVideoSize = new RecordVideoSize() { Width = 640, Height = 480 }

                // ScreenSize = new ScreenSize
                // {
                //     Width = 1024,
                //     Height = 1600,
                // },

                ViewportSize = new ViewportSize
                {
                    Width = 1200,
                    Height = 1600
                }
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
                        if (page.Video != null)
                        {
                            videoPath = await page.Video.PathAsync();
                        }
                    };

                    using (new AssertionScope())
                    {
                        await testDelegate(page);
                    }
                }
                // catch
                // {
                //     // Save a screenshot if exception is thrown
                //     await ScreenshotAsync(page, $"{testName}-exception");
                //     throw;
                // }
                finally
                {
                    // Save a screenshot
                    await ScreenshotAsync(page, $"{testName}");
                }
            }
            finally
            {
                // Wait 1 second so that video captures final state of page
                await Task.Delay(1000);

                await context.CloseAsync();

                // Rename video file
                if (videoPath != null)
                {
                    File.Move(videoPath, $"{MEDIAFOLDER}/{testName}.webm");
                }

                await browser.CloseAsync();
            }
        }

        protected static async Task ScreenshotAsync(IPage page, string name)
        {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{MEDIAFOLDER}/{name}.png" });
        }

        // Assert text content exists
        protected static async Task Assert_TextContentAsync(IPage page, string selector, string because = "")
        {
            string? found = null;
            try
            {
                found = await page.TextContentAsync(selector, new PageTextContentOptions
                {
                    Timeout = 1000
                });
            }
            catch (System.TimeoutException)
            {
                found.Should().Be(selector, because);
                throw;
            }
        }

        // Assert click
        protected static async Task Assert_ClickAsync(IPage page, string selector, string because = "")
        {
            try
            {
                await page.ClickAsync(selector, new PageClickOptions
                {
                    // Timeout = 1000
                });
            }
            catch (System.TimeoutException)
            {
                // found.Should().Be(selector, because);
                throw;
            }
        }
    }
}