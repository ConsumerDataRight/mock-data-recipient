using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class US23863_MDR_E2ETests : BaseTest, IClassFixture<TestFixture>
    {
        [Fact]
        public static async Task AC01_HomePage()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC01_HomePage)}", async (page) =>
            {
                // Arrange

                // Act - Goto home page
                await page.GotoAsync(WEB_URL);

                // Assert - Check banner
                await Assert_TextContentAsync(page, "h1 >> text=Mock Data Recipient");
                await Assert_TextContentAsync(page, "text=Welcome to the Mock Data Recipient");
                await Assert_TextContentAsync(page, "a >> text=Settings");
                await Assert_TextContentAsync(page, "a >> text=About");

                // Assert - Check menu items exists
                await Assert_TextContentAsync(page, "a >> text=Home");
                await Assert_TextContentAsync(page, "a >> text=Discover Data Holders");
                await Assert_TextContentAsync(page, "a >> text=Get SSA");
                await Assert_TextContentAsync(page, "a >> text=Dynamic Client Registration");
                await Assert_TextContentAsync(page, "a >> text=Consent and Authorisation");
                await Assert_TextContentAsync(page, "a >> text=Consents");
                await Assert_TextContentAsync(page, "a >> text=Consumer Data Sharing");
                await Assert_TextContentAsync(page, "a >> text=PAR");
                await Assert_TextContentAsync(page, "span >> text=Utilities");
                await Assert_TextContentAsync(page, "a >> text=ID Token Helper");
                await Assert_TextContentAsync(page, "a >> text=Private Key JWT Generator");

                // TODO - Finish this test
            });
        }

        [Fact]
        public static async Task AC02_DiscoverDataHolders()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_DiscoverDataHolders)}", async (page) =>
            {
                // Arrange - Goto home page
                await page.GotoAsync(WEB_URL);
                // Arrange - Click menu button
                await Assert_ClickAsync(page, "a >> text=Discover Data Holders");
                // Arrange - Check page loaded
                await Assert_TextContentAsync(page, "h2 >> text=Discover Data Holders");

                // Act - Click Refresh button
                await Assert_ClickAsync(page, @"h5:has-text(""Refresh Data Holders"") ~ div.card-body >> input:has-text(""Refresh"")");

                // Assert - Check refresh was successful, card-footer should be showing OK - 30 data holder brands loaded
                await Assert_TextContentAsync(page, @"h5:has-text(""Refresh Data Holders"") ~ div.card-footer >> text=OK - 30 data holder brands loaded");

                // TODO - Finish this test
            });
        }

        [Fact]
        public static async Task AC03_GetSSA()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC03_GetSSA)}", async (page) =>
            {
                // Arrange - Goto home page
                await page.GotoAsync(WEB_URL);
                // Arrange - Click menu button
                await Assert_ClickAsync(page, "a >> text=Get SSA");
                // Arrange - Check page loaded
                await Assert_TextContentAsync(page, "h2 >> text=Get Software Statement Assertion");

                // Act - Click Refresh button
                await Assert_ClickAsync(page, @"h5:has-text(""Get SSA"") ~ div.card-body >> input:has-text(""Get SSA"")");

                // Assert - Check refresh was successful, card-footer should be showing OK - SSA Generated
                await Assert_TextContentAsync(page, @"h5:has-text(""Get SSA"") ~ div.card-footer >> text=OK - SSA Generated");

                // TODO - Finish this test
            });
        }

        [Fact]
        public static async Task AC04_DynamicClientRegistration()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistration)}", async (page) =>
            {               
                // Arrange - Goto home page
                await page.GotoAsync(WEB_URL);
                // Arrange - Click menu button
                await Assert_ClickAsync(page, "a >> text=Dynamic Client Registration");
                // Arrange - Check page loaded
                await Assert_TextContentAsync(page, "h2 >> text=Dynamic Client Registration");

                // Act - Click Refresh button
                await Assert_ClickAsync(page, @"h5:has-text(""Create Client Registration"") ~ div.card-body >> input:has-text(""Register"")");

                // Assert - Check refresh was successful, card-footer should be showing OK etc
                await Assert_TextContentAsync(page, @"h5:has-text(""Create Client Registration"") ~ div.card-footer >> text=OK");

                // TODO - Finish this test
            });
        }

        [Fact]
        public static async Task AC05_ConsentAndAuthorisation()
        {
            static string GetClientId()
            {
                using var mdrConnection = new SqliteConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
                mdrConnection.Open();

                using var selectCommand = new SqliteCommand($"select clientid from registration", mdrConnection);
                string? clientId = Convert.ToString(selectCommand.ExecuteScalar());

                if (String.IsNullOrEmpty(clientId))
                    throw new Exception("No registrations found");

                return clientId;
            }

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC05_ConsentAndAuthorisation)}", async (page) =>
            {
                // Arrange - Goto home page
                await page.GotoAsync(WEB_URL);

                // await page.ClickAsync("text=Consent and Authorisation");
                await page.Locator("text=Consent and Authorisation").ClickAsync();
                await page.Locator("h2 >> text=Consent and Authorisation").TextContentAsync();

                // await page.SelectOptionAsync("select[name=\"ClientId\"]", new[] { GetClientId() });
                await page.Locator("select[name=\"ClientId\"]").SelectOptionAsync(new[] { GetClientId() });

                // await page.FillAsync("input[name=\"SharingDuration\"]", "10000");
                await page.Locator("input[name=\"SharingDuration\"]").FillAsync("10000");

                // await page.ClickAsync("text=Construct Authorisation Uri");
                await page.Locator("text=Construct Authorisation Uri").ClickAsync();

                // var locator = page.Locator("body > main > div.main.col-10.p-3 > div > div.card-body > p > a");
                // await locator.ClickAsync();
                await page.Locator("p.results > a").ClickAsync();

                await page.FillAsync("[placeholder=\"Your Customer ID\"]", "jwilson");
                await page.ClickAsync("text=Continue");

                // FIXME - should wait until text becomes visible, until then just wait 5 seconds
                // await page.TextContentAsync("text=Your One Time Password is 000789 >> visible=true");
                await Task.Delay(5000);                

                await page.FillAsync("[placeholder=\"Enter 6 digit One Time Password\"]", "000789");
                await page.ClickAsync("text=Continue");

                await page.ClickAsync("text=Personal Loan xxx-xxx xxxxx987");
                await page.ClickAsync("text=Continue");

                await page.ClickAsync("text=I Confirm");

                await page.TextContentAsync("text=Consent and Authorisation - Callback");

                // TODO - Finish this test
            });
        }

        // TODO - The following tests need to be finished
        /*
        [Fact]
        public static void AC06_Consents()
        {
            // throw new NotImplementedException();
        }

        [Fact]
        public static void AC07_ConsumerDataSharing()
        {
            // throw new NotImplementedException();
        }

        [Fact]
        public static void AC08_PAR()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public static void AC09_IDTokenHelper()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public static void AC10_PrivateKeyJWTHelper()
        {
            throw new NotImplementedException();
        }
        */

        [Fact]
        public static async Task AC11_Settings()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC11_Settings)}", async (page) =>
            {
                // Arrange - Goto home page
                await page.GotoAsync(WEB_URL);

                // Act - Click settings button
                await Assert_ClickAsync(page, "a >> text=Settings");

                // Assert - Check settings page loaded
                await Assert_TextContentAsync(page, "h2 >> text=Settings");

                // TODO - Finish this test
            });
        }

        [Fact]
        public static async Task AC12_About()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC12_About)}", async (page) =>
            {
                // Arrange - Goto home page
                await page.GotoAsync(WEB_URL);

                // Act - Click settings button
                await Assert_ClickAsync(page, "a >> text=About");

                // Assert - Check settings page loaded
                await Assert_TextContentAsync(page, "h2 >> text=About");

                // TODO - Finish this test
            });
        }
    }
}