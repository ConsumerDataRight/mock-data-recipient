using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class US23863_MDR_E2ETests : BaseTest, IClassFixture<TestFixture>
    {
        private const string SHARING_DURATION = "100000";

        // Pre-generated ID Token used in IDTokenhelper test
        const string IDTOKEN = @"eyJhbGciOiJSU0EtT0FFUCIsImVuYyI6IkEyNTZHQ00iLCJraWQiOiJCNTQ4QzkxNEEwMjc4N0EzQjVGMTU1ODNDOEVCMDMwRDk0QkMyNDI1In0.biUv7OpkYyUYecHQ8x-XxyR77ZwCfCORY-MRLtoOYuoM5XrWm" +
            "mqmuVD_1mFa88dvmkEeDPnK89cdzCFevV1u5xThXECMUHAMyBrxLgcp86DFqBtiB9g1UyvN1-1JgTX3719yl0tk5jqXvKW4iike-rtI-vZyeFtcZAp2ZrEXMo033cAEzW7gOKiZtoTY2GTmxgdtH5DqhCld0kNfII" +
            "idpQ0vaKNVbV28K0sotUjSOGeN63vOtPb3JrcUorQ1kMqI4mTZDDSs8O97fqEqcnKsbvw65WOQk-T5uqPPycz2Q8BW_V1V0t8F19VMGB2rmHnFxVlBaWlm3NBdUnqFxWmYyw.EpLS7enX1zoeCYn5.cQ9dhezjgVE" +
            "AzReklnAdUgcsCSMDhJ3GmriGD5gBqFveI4anjsupBMqd39OVhSdsJKh_w1Xz8rtY3OdeQPGLG_ZFlGdFLn3KMRRYrPx9i8lifXMYR2dxSDBDDfFblmmpGogrSqH5_O7e3kr4J9uRIVffGRHMZzajsg1ZqeSTiStI" +
            "teGxxTVwnMSIUvsUpLLFN3wUZA7mlidtD1WhylRSbLHpw6h7OPKgELxUrl28FQd44vBNUnEBo73jpm1v0XvPwZI6JDZjoG4lFXg88SzOwnHX6B2xdWTDqrSkTboWw-tAJyPDYCzmufOx8c9UKp8FA7gLUNYqWBwTo" +
            "jkOuj-xiqm9VkBFJ-MIFZcZiLAS5W2e2sE_td78CxroYK1iKpGr4CoHvvDrS-RhqiOWR7_lGmMOgGFxYZvFugr_iNYW69UPhMfKOBx_aGS6g_UBnNQfW10zhGNPFG3aacJ9JkDEC8HEBVddmbxCK2f_PBpzG65GXI" +
            "kcFOHQlkoKgfHSFqcSDQvBte-hHgKuL-iFOoIaCTvytQ5SwAoqEmR_6O6_5GI_TyMV3h0ZtJOdAR0CBL6E5brmTYMgbpSJiQDJTaDTJBkLFrJxkgE1rDByXyO4hYGyq6ec8FHdr49c8Pe5w7hDZLMIzE2HGkXwK6B" +
            "cxuu9pTXotJKzpCOlsjTRQRzHfvW-Jc7x6RAZO_9HdslTr_fY5addJscGuy7TOqXDvTBY-fsk8F3RbKb_ZekmfNVo1-v9MbtvszynF6bx3eXOVbdWmt-mxGijzf0_iKOJicvuddOjNZizTMgkcsceoxSnnm0rX1Ob" +
            "SMAmyLr0uQYC8EZAiUuqZUyTsydCH4P9C3GgxYX_NkLacV92iEZTTqt4NajFJq5u3JwBTp2gA8p-cim6ncV82cQfNBW4Cuct2uQuRODiMsgwaMVAxTtUKV3tWz66BEh2fRWuwmJrgDKTo2KlVMH3GhBVUQg4zCo26" +
            "ewTMWy8zJl7ehplYPDbtOJJB-yzz0lOfkE9r3s1izbzmcw7A3_ITw7U3dHmFApZyxX3lrR4hPg9bJNG6Vvr5CGL5snit3spe1nzujp1aY91lVIUTziBiyshnWhLjjcl341nPJPnQ9aw81nIx_3PAjoMT0O4Y338fD" +
            "H-u34_gkCGyXUx4IWuf3okhRF4GIE4Hz-Dls4fgJSkhHYXGJcHiDauv8RdzbmzjBG_-grLvAlnHVb5pXHQNZz0aNKqu6YHFuEtlwXqvbOGejv_2DLB0csxiBLDHUfoQGZb0_OOaq3CPE6_q87K8gJlLfkqoVsrgbL" +
            "o0hnzYCaP2rI6oB1PMMPNxeW4OuuSZYcs_GX0jiLwzt88gsoLoavbFsWl7arskUWN9XoIRieTJfFi1seY3zH-hVGVQC5ixPgJv45Q96LfpZZKV5lJPNOQHhqaoHOfHSb2jw-X-_drxzFB0bfGqnFQ32VC1xi_qnBG" +
            "TB9U3T-pxeWnjaefeLxXDlQx7ZRPIni7Yf7sB3u4IoxfUNuORHL9OP1d_fljD_pd65xZEY-weNGJ8NhlFeMzf_e288dg44PD2xzkk5oAsh_qdn8HKV8PeBTaiUywYvQiQM3HGGJ6kd3TeB2UYmLFlCwsjONt_qJ7w" +
            "dJ9KgyjWN6ypuCrbCZ9TGq6i0o3HjH6tNup6ltnYfquW1FPmyIeB_TEQ3GLsxJSAauomwJ9PljuEJlhZsh7Cllc8ack7R47UHbYXAYTR2VVt11PsAWrdeCoAQpjqQ.S6AhwkvWKyJ7Jz3nmp_PJg";

        private bool inArrange = false;
        private delegate Task ArrangeDelegate();
        private async Task Arrange(ArrangeDelegate arrange)
        {
            if (inArrange)
                return;

            inArrange = true;
            try
            {
                CreateMedia = false;
                try
                {
                    TestFixture.PatchRegister();
                    TestFixture.PurgeMDR();
                    TestFixture.PurgeMDHIdentityServer();
                    await arrange();
                }
                finally
                {
                    CreateMedia = true;
                }
            }
            finally
            {
                inArrange = true;
            }
        }

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

        public class ConsentAndAuthorisationResponse
        {
            public string? IDToken { get; init; }
            public string? AccessToken { get; init; }
            public string? RefreshToken { get; init; }
            public string? ExpiresIn { get; init; }
            public string? Scope { get; init; }
            public string? TokenType { get; init; }
            public string? CDRArrangementID { get; init; }
        }

        // static async Task<(string? idToken, string? accessToken, string? refreshToken, string? scope, string? cdrArrangementId)> ConsentAndAuthorisation(IPage page)
        static async Task<ConsentAndAuthorisationResponse> ConsentAndAuthorisation(IPage page)
        {
            // Act - Enter Customer ID
            await page.Locator("[placeholder=\"Your Customer ID\"]").FillAsync("jwilson");
            await page.Locator("text=Continue").ClickAsync();

            // Act - Wait for OTP, then enter it, and click Continue
            await Task.Delay(5000);
            await page.Locator("[placeholder=\"Enter 6 digit One Time Password\"]").FillAsync("000789");
            await page.Locator("text=Continue").ClickAsync();

            // Act - select Personal Loan account, click Continue and I confirm
            await page.Locator("text=Personal Loan xxx-xxx xxxxx987").ClickAsync();
            await page.Locator("text=Continue").ClickAsync();
            await page.Locator("text=I Confirm").ClickAsync();

            // Assert - Check callback is shown and get arrangement ID
            await page.Locator("text=Consent and Authorisation - Callback").TextContentAsync();

            return new ConsentAndAuthorisationResponse
            {
                IDToken = await page.Locator(@"dt:has-text(""Id Token"") + dd ").TextContentAsync(),
                AccessToken = await page.Locator(@"dt:has-text(""Access Token"") + dd ").TextContentAsync(),
                RefreshToken = await page.Locator(@"dt:has-text(""Refresh Token"") + dd ").TextContentAsync(),
                ExpiresIn = await page.Locator(@"dt:has-text(""Expires In"") + dd ").TextContentAsync(),
                Scope = await page.Locator(@"dt:has-text(""Scope"") + dd ").TextContentAsync(),
                TokenType = await page.Locator(@"dt:has-text("" Token Type"") + dd ").TextContentAsync(),
                CDRArrangementID = await page.Locator(@"dt:has-text(""CDR Arrangement Id"") + dd ").TextContentAsync()
            };
        }

        public static async Task TestToken(IPage page, string menuText, string? expectedToken)
        {
            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=Consents").ClickAsync();
            await page.Locator("h2 >> text=Consents").TextContentAsync();

            // Act - Click actions button and submenu item
            await page.Locator("button:has-text(\"Actions\")").ClickAsync();
            await page.Locator($"a >> text={menuText}").ClickAsync();

            // Act - Check modal opens
            await page.Locator("div#modal-token >> h5.modal-title >> text=Token Contents").TextContentAsync();

            // Assert - Check actual token matches expected token
            var token = await page.Locator(@"div#modal-token >> div.modal-body").TextContentAsync();
            token.Should().NotBeNullOrEmpty();
            token.Should().Be(expectedToken);
        }

        public static async Task TestInfo(IPage page, string menuText, string modalTitle, string expectedStatusCode, (string name, string? value)[]? expectedPayload = null)
        {
            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=Consents").ClickAsync();
            await page.Locator("h2 >> text=Consents").TextContentAsync();

            // Act - Click actions button and submenu item
            await page.Locator("button:has-text(\"Actions\")").ClickAsync();
            await page.Locator($"a >> text={menuText}").ClickAsync();

            // Act - Check modal opens
            await page.Locator($"div#modal-info >> h5.modal-title >> text={modalTitle}").TextContentAsync();

            // Assert - Check statuscode
            var statusCode = await page.Locator(@$"div#modal-info >> div.modal-statusCode >> text={expectedStatusCode}").TextContentAsync();
            statusCode.Should().Be(expectedStatusCode);

            // Assert - Check payload is what's expected
            if (expectedPayload != null)
            {
                var payload = await page.Locator(@"div#modal-info >> pre.modal-payload").TextContentAsync();
                Assert_Json2(payload, expectedPayload);
            }
        }

        static async Task TestResults(IPage page, string label, string? value = null)
        {
            // Check for label and value
            if (value != null)
                await page.Locator($"div.results >> dl >> dt:has-text(\"{label}\") + dd:has-text(\"{value}\")").TextContentAsync();
            // Just check for label
            else
                await page.Locator($"div.results >> dl >> dt:has-text(\"{label}\")").TextContentAsync();
        }

        [Fact]
        public async Task AC01_HomePage()
        {
            await Arrange(async () => { });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC01_HomePage)}", async (page) =>
            {
                // Act - Goto home page
                await page.GotoAsync(WEB_URL);

                // Assert - Check banner
                await page.Locator("h1 >> text=Mock Data Recipient").TextContentAsync();
                await page.Locator("text=Welcome to the Mock Data Recipient").TextContentAsync();
                await page.Locator("a >> text=Settings").TextContentAsync();
                await page.Locator("a >> text=About").TextContentAsync();

                // Assert - Check menu items exists
                await page.Locator("a >> text=Home").TextContentAsync();
                await page.Locator("a >> text=Discover Data Holders").TextContentAsync();
                await page.Locator("a >> text=Get SSA").TextContentAsync();
                await page.Locator("a >> text=Dynamic Client Registration").TextContentAsync();
                await page.Locator("a >> text=Consent and Authorisation").TextContentAsync();
                await page.Locator("a >> text=Consents").TextContentAsync();
                await page.Locator("a >> text=Consumer Data Sharing").TextContentAsync();
                await page.Locator("a >> text=PAR").TextContentAsync();
                await page.Locator("span >> text=Utilities").TextContentAsync();
                await page.Locator("a >> text=ID Token Helper").TextContentAsync();
                await page.Locator("a >> text=Private Key JWT Generator").TextContentAsync();
            });
        }

        [Fact]
        public async Task AC02_DiscoverDataHolders()
        {
            await Arrange(async () => { });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_DiscoverDataHolders)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=Discover Data Holders").ClickAsync();
                await page.Locator("h2 >> text=Discover Data Holders").TextContentAsync();

                // Act - Click Refresh button
                await page.Locator(@"h5:has-text(""Refresh Data Holders"") ~ div.card-body >> input:has-text(""Refresh"")").ClickAsync(); ;

                // Assert - Check refresh was successful, card-footer should be showing OK - 30 data holder brands loaded
                await page.Locator(@"h5:has-text(""Refresh Data Holders"") ~ div.card-footer >> text=OK - 30 data holder brands loaded").TextContentAsync();
            });
        }

        [Fact]
        public async Task AC03_GetSSA()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC03_GetSSA)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=Get SSA").ClickAsync();
                await page.Locator("h2 >> text=Get Software Statement Assertion").TextContentAsync();

                // Act - Click Refresh button
                await page.Locator(@"h5:has-text(""Get SSA"") ~ div.card-body >> input:has-text(""Get SSA"")").ClickAsync();

                // Assert - Check refresh was successful, card-footer should be showing OK - SSA Generated
                await page.Locator(@"h5:has-text(""Get SSA"") ~ div.card-footer >> text=OK - SSA Generated").TextContentAsync();
            });
        }

        [Fact]
        public async Task AC04_DynamicClientRegistration()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistration)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=Dynamic Client Registration").ClickAsync();
                await page.Locator("h2 >> text=Dynamic Client Registration").TextContentAsync();

                // Act - Click Refresh button
                await page.Locator(@"h5:has-text(""Create Client Registration"") ~ div.card-body >> input:has-text(""Register"")").ClickAsync(); ;

                // Assert - Check refresh was successful, card-footer should be showing OK etc
                await page.Locator(@"h5:has-text(""Create Client Registration"") ~ div.card-footer >> text=OK").TextContentAsync();
            });
        }

        [Fact]
        public async Task<ConsentAndAuthorisationResponse> AC05_ConsentAndAuthorisation()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
            });

            ConsentAndAuthorisationResponse? res = null;
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC05_ConsentAndAuthorisation)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("text=Consent and Authorisation").ClickAsync();
                await page.Locator("h2 >> text=Consent and Authorisation").TextContentAsync();

                // Act - Select Client ID, enter Sharing Duration, click Construct Authoriation URI button
                await page.Locator("select[name=\"ClientId\"]").SelectOptionAsync(new[] { GetClientId() });
                await page.Locator("input[name=\"SharingDuration\"]").FillAsync(SHARING_DURATION);
                await page.Locator("text=Construct Authorisation Uri").ClickAsync();

                // Act - Click Authorisation URI link
                await page.Locator("p.results > a").ClickAsync();

                // Act/Assert - Perform consent and authorisation
                res = await ConsentAndAuthorisation(page);
            });

            return res ?? throw new ArgumentNullException($"Expected {nameof(ConsentAndAuthorisationResponse)}");
        }

        [Fact]
        public async Task AC06_Consents_ViewIDToken()
        {
            ConsentAndAuthorisationResponse? response = null;

            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                response = await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewIDToken)}", async (page) =>
            {
                await TestToken(page, "View ID Token", response?.IDToken ?? throw new ArgumentNullException());
            });
        }

        [Fact]
        public async Task AC06_Consents_ViewAccessToken()
        {
            ConsentAndAuthorisationResponse? response = null;

            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                response = await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewAccessToken)}", async (page) =>
            {
                await TestToken(page, "View Access Token", response?.AccessToken ?? throw new ArgumentNullException());
            });
        }

        [Fact]
        public async Task AC06_Consents_ViewRefreshToken()
        {
            ConsentAndAuthorisationResponse? response = null;

            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                response = await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewRefreshToken)}", async (page) =>
            {
                await TestToken(page, "View Refresh Token", response?.RefreshToken ?? throw new ArgumentNullException());
            });
        }

        [Fact]
        public async Task AC06_Consents_ViewUserInfo()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewUserInfo)}", async (page) =>
            {
                var expected = new (string, string?)[]
                {
                    ("given_name", "Jane"),
                    ("family_name", "Wilson"),
                    ("name", "Jane Wilson"),
                    ("aud", ASSERT_JSON2_ANYVALUE),
                    ("iss", ASSERT_JSON2_ANYVALUE),
                    ("sub", ASSERT_JSON2_ANYVALUE),
                };

                await TestInfo(page, "UserInfo", "User Info", "200", expected);
            });
        }

        [Fact]
        public async Task AC06_Consents_Introspect()
        {
            ConsentAndAuthorisationResponse? response = null;

            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                response = await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Introspect)}", async (page) =>
            {
                var expected = new (string, string?)[]
                {
                    ("cdr_arrangement_id", response?.CDRArrangementID ?? throw new ArgumentNullException()),
                    ("scope", ASSERT_JSON2_ANYVALUE),
                    ("exp", ASSERT_JSON2_ANYVALUE),
                    ("active", "True"),
                };

                await TestInfo(page, "Introspect", "Introspection", "200", expected);
            });
        }

        [Fact]
        public async Task AC06_Consents_Refresh_Access_Token()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Refresh_Access_Token)}", async (page) =>
            {
                var expected = new (string, string?)[]
                {
                    ("id_token", ASSERT_JSON2_ANYVALUE),
                    ("access_token", ASSERT_JSON2_ANYVALUE),
                    ("refresh_token", ASSERT_JSON2_ANYVALUE),
                    ("expires_in", ACCESSTOKENLIFETIMESECONDS),
                    ("token_type", "Bearer"),
                    ("scope", "openid profile bank:accounts.basic:read bank:transactions:read common:customer.basic:read bank:accounts.detail:read"),
                    ("cdr_arrangement_id", ASSERT_JSON2_ANYVALUE),
                };

                await TestInfo(page, "Refresh Access Token", "Refresh Access Token", "200", expected);
            });
        }

        [Fact]
        public async Task AC06_Consents_Revoke_Arrangement()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Revoke_Arrangement)}", async (page) =>
            {
                await TestInfo(page, "Revoke Arrangement", "Revoke Arrangement", "204");
                await ScreenshotAsync(page, "-Modal");

                await page.Locator("div#modal-info >> div.modal-footer >> a >> text=Refresh Page").ClickAsync();
            });
        }

        [Fact]
        public async Task AC06_Consents_Revoke_AccessToken()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Revoke_AccessToken)}", async (page) =>
            {
                await TestInfo(page, "Revoke Access Token", "Revoke Access Token", "200");
            });
        }

        [Fact]
        public async Task AC06_Consents_Revoke_RefreshToken()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Revoke_RefreshToken)}", async (page) =>
            {
                await TestInfo(page, "Revoke Refresh Token", "Revoke Refresh Token", "200");
            });
        }

        [Fact]
        public async Task AC06_Consents_Delete_Local()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Delete_Local)}", async (page) =>
            {
                await TestInfo(page, "Delete (local)", "Delete Arrangement", "204");
            });
        }

        [Fact]
        public async Task AC07_ConsumerDataSharing()
        {
            await Arrange(async () => { });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC07_ConsumerDataSharing)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=Consumer Data Sharing").ClickAsync();
                await page.Locator("h2 >> text=Data Sharing").TextContentAsync();
            });
        }

        // [Fact]
        public async Task AC08_PAR()
        {
            await Arrange(async () =>
            {
                await AC02_DiscoverDataHolders();
                await AC03_GetSSA();
                await AC04_DynamicClientRegistration();
                await AC05_ConsentAndAuthorisation();
            });

            static string GetCDRArrangementId()
            {
                using var mdrConnection = new SqliteConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
                mdrConnection.Open();

                using var selectCommand = new SqliteCommand($"select cdrarrangementid from cdrarrangement", mdrConnection);
                string? cdrArrangementId = Convert.ToString(selectCommand.ExecuteScalar());

                if (String.IsNullOrEmpty(cdrArrangementId))
                    throw new Exception("No arrangements found");

                return cdrArrangementId;
            }

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC08_PAR)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=PAR").ClickAsync();
                await page.Locator("h2 >> text=Pushed Authorisation Request (PAR)").TextContentAsync();

                // Arrange - Select Client ID, CdrArrangementId, enter Sharing Duration, click Initate PAR button
                await page.Locator("select[name=\"ClientId\"]").SelectOptionAsync(new[] { GetClientId() });
                await page.Locator("select[name=\"CdrArrangementId\"]").SelectOptionAsync(new[] { GetCDRArrangementId() });
                await page.Locator("input[name=\"SharingDuration\"]").FillAsync(SHARING_DURATION);

                // Act - Click Initiate PAR button
                await page.Locator("div.form >> text=Initiate PAR").ClickAsync();

                // Act - Click request uri
                await page.Locator("p.results > a").ClickAsync();

                // Act/Assert - Perform consent and authorisation
                await ConsentAndAuthorisation(page);
            });
        }

        [Theory]
        [InlineData(IDTOKEN)]
        public async Task AC09_IDTokenHelper(string encryptedToken)
        {
            await Arrange(async () => { });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC09_IDTokenHelper)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=ID Token Helper").ClickAsync();
                await page.Locator("h2 >> text=ID Token Helper").TextContentAsync();

                // Arrange - Enter id token to decrypt
                await page.Locator("textarea[name=\"IdTokenEncrypted\"]").FillAsync(encryptedToken);

                // Act
                await page.Locator("text=Decrypt ID Token").ClickAsync();

                // Assert - Check results
                await TestResults(page, "nbf", "1635114387");
                await TestResults(page, "exp", "1635114687");
                await TestResults(page, "iss", "https://localhost:8001");
                await TestResults(page, "aud", "ffd1f415-a576-4e3e-9eab-1f732bbf55c6");
                await TestResults(page, "nonce", "38ff9cc4-57c4-404a-98ec-5e519336d419");
                await TestResults(page, "iat", "1635114387");
                await TestResults(page, "at_hash", "HPpIrSN5_JyYHYUfwp9LAA");
                await TestResults(page, "s_hash", "D0zlxPkG8fQvsfSB9hw36w");
                await TestResults(page, "auth_time", "1635114375");
                await TestResults(page, "idp", "local");
                await TestResults(page, "sharing_expires_at", "1635214377");
                await TestResults(page, "cdr_arrangement_id", "457e3e07-0fae-4441-ad44-0523f4bd13eb");
                await TestResults(page, "sub", "pJkPifkovXGjesboAo9+40W1e7nf2QHtXV0KZLjQTJsA8Oy03Q3CVJvGuvW/uY8p");
                await TestResults(page, "name", "Jane Wilson");
                await TestResults(page, "family_name", "Wilson");
                await TestResults(page, "given_name", "Jane");
                await TestResults(page, "updated_at", "1624884401");
                await TestResults(page, "acr", "urn:cds.au:cdr:2");
                await TestResults(page, "refresh_token_expires_at", "1635214377");
                await TestResults(page, "amr", "pwd");
            });
        }

        [Fact]
        public async Task AC10_PrivateKeyJWTGenerator()
        {
            await Arrange(async () => { });

            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC10_PrivateKeyJWTGenerator)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=Private Key JWT Generator").ClickAsync();
                await page.Locator("h2 >> text=Private Key JWT Generator").TextContentAsync();
                await page.Locator("input[name=\"Jti\"]").FillAsync("foo");

                // Act - Click generate button
                await page.Locator("form >> text=Generate").ClickAsync();

                // Assert - Client assertion was generated, and check for View Decoded button
                var clientAssertion = await page.Locator("div.results >> div.code").TextContentAsync();
                clientAssertion.Should().NotBeNullOrWhiteSpace();
                await page.Locator("a >> text=View Decoded").TextContentAsync();

                // Assert - Check client assertions claims
                await TestResults(page, "sub", "c6327f87-687a-4369-99a4-eaacd3bb8210");
                await TestResults(page, "jti", "foo");
                await TestResults(page, "iat"); // just check claim exists
                await TestResults(page, "exp"); // just check claim exists
                await TestResults(page, "iss", "c6327f87-687a-4369-99a4-eaacd3bb8210");
                await TestResults(page, "aud", "https://localhost:7001/idp/connect/token");
            });
        }

        [Fact]
        public async Task AC11_Settings()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC11_Settings)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=Settings").ClickAsync();
                await page.Locator("h2 >> text=Settings").TextContentAsync();
            });
        }

        [Fact]
        public async Task AC12_About()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC12_About)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded                
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=About").ClickAsync();
                await page.Locator("h2 >> text=About").TextContentAsync();
            });
        }
    }
}