using CDR.DataRecipient.E2ETests.Pages;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class US23863_MDR_E2ETests : BaseTest, IClassFixture<TestFixture>
    {
        private const string SWAGGER_BANKING_IFRAME = "cds-banking/index.html";
        private const string SWAGGER_ENERGY_IFRAME = "cds-energy/index.html";
        private const string SWAGGER_COMMON_IFRAME = "cds-common/index.html";

        static string GetClientId()
        {
            using var mdrConnection = new SqlConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
            mdrConnection.Open();

            using var selectCommand = new SqlCommand($"select clientid from registration", mdrConnection);
            string? clientId = Convert.ToString(selectCommand.ExecuteScalar());

            if (String.IsNullOrEmpty(clientId))
                throw new Exception("No registrations found");

            return clientId;
        }

        static async Task TestToken(IPage page, string menuText, string? expectedToken)
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
                await page.Locator("a >> text=Consents").TextContentAsync();
                await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Common\")").TextContentAsync();
                await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Banking\")").TextContentAsync();
                await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Energy\")").TextContentAsync();
                await page.Locator("a >> text=PAR").TextContentAsync();
                await page.Locator("span >> text=Utilities").TextContentAsync();
                await page.Locator("a >> text=Private Key JWT Generator").TextContentAsync();
                Assert.True(true);
            });
        }

        [Fact]
        public async Task AC02_01_DiscoverDataHolders_Banking()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_01_DiscoverDataHolders_Banking)}", async (page) =>
            {
                await DataHolders_Discover(page, "BANKING", "2", 30);
            });
        }

        [Fact]
        public async Task AC02_02_DiscoverDataHolders_Energy()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_02_DiscoverDataHolders_Energy)}", async (page) =>
            {
                await DataHolders_Discover(page, "ENERGY", "2", 2);
            });
        }

        [Fact]
        public async Task AC02_03_DiscoverDataHolders_Telco()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_03_DiscoverDataHolders_Telco)}", async (page) =>
            {
                await DataHolders_Discover(page, "TELCO", "2", 0);  // Currently no Telco dataholders, so 0 = no additional dataholders loaded
            });
        }

        [Fact]
        public async Task AC02_04_DiscoverDataHolders_AnyIndustry()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_04_DiscoverDataHolders_AnyIndustry)}", async (page) =>
            {
                await DataHolders_Discover(page, "ALL", "2", 0);  // Should have loaded all dataholders by now, so 0 = no additional dataholders loaded
            });
        }

        [Theory]
        [InlineData("ALL", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("ALL", "foo", null, "BadRequest - Bad Request")]
        [InlineData("BANKING", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("BANKING", "foo", null, "BadRequest - Bad Request")]
        [InlineData("ENERGY", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("ENERGY", "foo", null, "BadRequest - Bad Request")]
        [InlineData("TELCO", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("TELCO", "foo", null, "BadRequest - Bad Request")]
        public async Task AC02_99_DiscoverDataHolders(string industry = "ALL", string version = "2", int? expectedRecords = 32, string? expectedError = null)
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC02_99_DiscoverDataHolders)} - Industry={industry} - Version={version}", async (page) =>
            {
                await DataHolders_Discover(page, industry, version, expectedRecords, expectedError);
            });
        }

        [Theory]
        [InlineData("BANKING", "1", DR_BRANDID, DR_SOFTWAREPRODUCTID, "NotAcceptable")]
        [InlineData("BANKING", "2", DR_BRANDID, DR_SOFTWAREPRODUCTID, "NotAcceptable")]
        [InlineData("BANKING", "3", DR_BRANDID, DR_SOFTWAREPRODUCTID, "OK - SSA Generated")]
        [InlineData("BANKING", "4", DR_BRANDID, DR_SOFTWAREPRODUCTID, "NotAcceptable")]
        public async Task AC03_GetSSA(string industry, string version, string drBrandId, string drSoftwareProductId, string expectedMessage)
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC03_GetSSA)} - Version={version} - DR_BrandId={drBrandId} - DR_SoftwareProductId={drSoftwareProductId}", async (page) =>
            {
                await SSA_Get(page, industry, version, drBrandId, drSoftwareProductId, expectedMessage);
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID)] // Also test for Energy DH 
        public async Task AC04_DynamicClientRegistration(string dhBrandId = DH_BRANDID, string drBrandId = DR_BRANDID, string drSoftwareProductId = DR_SOFTWAREPRODUCTID)
        {
            string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistration)} - DH_BrandId={dhBrandId} - DR_BrandId={drBrandId} - DR_SoftwareProductId={drSoftwareProductId}";
            try
            {
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                });

                await TestAsync(testName, async (page) =>
                {

                    // Create a default Banking Dataholder Registration using defaults
                    DynamicClientRegistrationPage dcrPage = new DynamicClientRegistrationPage(page, WEB_URL);
                    await dcrPage.GotoDynamicClientRegistrationPage();
                    await dcrPage.SelectDataHolderBrandId(DH_BRANDID);
                    await dcrPage.ClickRegister();

                    // Assert Software Product Registered
                    var registrationResponse = await dcrPage.GetRegistrationResponse(includeHeading: true);
                    registrationResponse.Should().Contain("Created - Registered");

                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID)] // Also test for Energy DH 
        public async Task AC04_DynamicClientRegistration_Defaults(string dhBrandId = DH_BRANDID, string drBrandId = DR_BRANDID, string drSoftwareProductId = DR_SOFTWAREPRODUCTID)
        {
            string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistration_Defaults)} - DH_BrandId={dhBrandId} - DR_BrandId={drBrandId} - DR_SoftwareProductId={drSoftwareProductId}";

            await ArrangeAsync(testName, async (page) =>
            {
                await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
            });

            await TestAsync(testName, async (page) =>
            {

                // Select Dataholder
                DynamicClientRegistrationPage dcrPage = new DynamicClientRegistrationPage(page, WEB_URL);
                await dcrPage.GotoDynamicClientRegistrationPage();
                await dcrPage.SelectDataHolderBrandId(dhBrandId);

                // Assert all default values
                using (new AssertionScope())
                {
                    dcrPage.GetClientId().Result.Should().Be(String.Empty);
                    dcrPage.GetSsaVersion().Result.Should().Be("3");
                    dcrPage.GetIndustry().Result.Should().Be("ALL");
                    dcrPage.GetSoftwareProductId().Result.Should().Be(drSoftwareProductId);
                    dcrPage.GetRedirectUris().Result.Should().Be($"https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/consent/callback");
                    dcrPage.GetScope().Result.Should().Be(DR_DEFAULT_SCOPES);
                    dcrPage.GetTokenSigningAlgo().Result.Should().Be("PS256");
                    dcrPage.GetGrantTypes().Result.Should().Be("client_credentials,authorization_code,refresh_token");
                    dcrPage.GetResponseTypes().Result.Should().Be("code");
                    dcrPage.GetApplicationType().Result.Should().Be("web");
                    dcrPage.GetIdTokenSignedResponseAlgo().Result.Should().Be("PS256");
                    dcrPage.GetRequestSigningAlgo().Result.Should().Be("PS256");
                    dcrPage.GetAuthorisedSignedResponsegAlgo().Result.Should().Be("PS256");
                    dcrPage.GetAuthorisedEncryptedResponseAlgo().Result.Should().Be(String.Empty);
                    dcrPage.GetAuthorisedEncryptedResponseEnc().Result.Should().Be(String.Empty);
                }

            });

        }

        [Fact]
        public async Task AC04_DynamicClientRegistrationViewRegistration()
        {
            string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistrationViewRegistration)}";
            try
            {
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    dhClientId = await ClientRegistration_Create(page, DH_BRANDID) ?? throw new NullReferenceException(nameof(dhClientId));
                });

                await TestAsync(testName, async (page) =>
                {
                    // View newly created registration
                    DynamicClientRegistrationPage dcrPage = new DynamicClientRegistrationPage(page, WEB_URL);
                    await dcrPage.GotoDynamicClientRegistrationPage();
                    await dcrPage.ClickViewRegistration(dhClientId);

                    // Assert
                    using (new AssertionScope())
                    {
                        string viewRegistrationResponse = await dcrPage.GetViewRegistrationResponse();
                        viewRegistrationResponse.Should().Contain("Registration retrieved successfully.");
                        viewRegistrationResponse.Should().Contain($"\"client_id\": \"{dhClientId}\"");
                    }

                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Fact]
        public async Task AC04_DynamicClientRegistrationViewDiscoveryDocument()
        {
            string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistrationViewDiscoveryDocument)}";

            await ArrangeAsync(testName, async (page) =>
            {
                await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands                   
            });

            await TestAsync(testName, async (page) =>
            {
                // View discovery document
                DynamicClientRegistrationPage dcrPage = new DynamicClientRegistrationPage(page, WEB_URL);
                await dcrPage.GotoDynamicClientRegistrationPage();
                await dcrPage.SelectDataHolderBrandId(DH_BRANDID);

                // Assert
                using (new AssertionScope())
                {
                    string discoveryDocumentDetails = await dcrPage.GetDiscoveryDocumentDetails("Discovery Document details loaded");
                    discoveryDocumentDetails.Should().Contain($"Discovery Document details loaded from https://{HOSTNAME_DATAHOLDER}:8001/.well-known/openid-configuration");
                    discoveryDocumentDetails.Should().Contain($"\"issuer\": \"https://{HOSTNAME_DATAHOLDER}:8001\"");
                }

            });

        }

        [Fact]
        public async Task AC04_DynamicClientRegistrationViewDiscoveryDocument_Invalid()
        {
            string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistrationViewDiscoveryDocument_Invalid)}";

            await ArrangeAsync(testName, async (page) =>
            {
                await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands                   
            });

            await TestAsync(testName, async (page) =>
            {
                // View discovery document
                DynamicClientRegistrationPage dcrPage = new DynamicClientRegistrationPage(page, WEB_URL);
                await dcrPage.GotoDynamicClientRegistrationPage();
                await dcrPage.SelectDataHolderBrandId(DH_BRANDID_DUMMY_DH);

                // Assert
                using (new AssertionScope())
                {
                    string discoveryDocumentDetails = await dcrPage.GetDiscoveryDocumentDetails("Discovery Document");
                    discoveryDocumentDetails.Should().Contain("Unable to load Discovery Document from https://idp.bank2/.well-known/openid-configuration");
                }

            });
        }

        [Theory]
        [InlineData(DH_BRANDID)]
        [InlineData(DH_BRANDID_ENERGY)] // Also test for Energy DH 
        public async Task AC04_DynamicClientRegistrationUpdate(string dhBrandId)
        {
            string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC04_DynamicClientRegistrationUpdate)}";
            try
            {
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                });

                await TestAsync(testName, async (page) =>
                {
                    DynamicClientRegistrationPage dcrPage = new DynamicClientRegistrationPage(page, WEB_URL);

                    // Create a default Banking Dataholder Registration using defaults
                    await dcrPage.GotoDynamicClientRegistrationPage();
                    await dcrPage.SelectDataHolderBrandId(dhBrandId);
                    await dcrPage.ClickRegister();

                    var registrationResponseJson = await dcrPage.GetRegistrationResponse();

                    // Deserialise response and return DH client id
                    string clientId = GetClientIdFromRegistrationResponse(registrationResponseJson);

                    // Navigate to fresh DCR page
                    await dcrPage.GotoDynamicClientRegistrationPage();
                    await dcrPage.ClickEditRegistration(clientId);

                    // Assert Client Correctly Loaded
                    dcrPage.GetClientId().Result.Should().Be(clientId);

                    // Modify authorization encrypted response enc and alg values
                    await dcrPage.EnterAuthorisedEncryptedResponseEnc("A128CBC-HS256");
                    await dcrPage.EnterAuthorisedEncryptedResponseAlgo("RSA-OAEP-256");

                    await dcrPage.ClickUpdate();

                    // Assert Software Product Registration Updated
                    var registrationResponse = await dcrPage.GetRegistrationResponse(includeHeading: true);
                    registrationResponse.Should().Contain("Registration update successful.");
                    registrationResponse.Should().Contain("\"authorization_encrypted_response_alg\": \"RSA-OAEP-256\"");
                    registrationResponse.Should().Contain("\"authorization_encrypted_response_enc\": \"A128CBC-HS256\"");

                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        public delegate Task ConsentsDelegate(IPage page, ConsentAndAuthorisationResponse response);
        async Task Test_Consents(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts, string testName, ConsentsDelegate test)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync($"{testName} - DH_BrandId={dhBrandId}", async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                       ?? throw new NullReferenceException(nameof(cdrArrangement));

                });

                await TestAsync($"{testName} - DH_BrandId={dhBrandId}", async (page) =>
                {
                    await test(page, cdrArrangement ?? throw new NullReferenceException(nameof(cdrArrangement)));
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewIDToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewIDToken)}", async (page, response) =>
            {
                // CT: This needs to be a different exception type
                await TestToken(page, "View ID Token", response?.IDToken ?? throw new ArgumentNullException(nameof(response.IDToken)));
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewAccessToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewAccessToken)}", async (page, response) =>
            {
                // CT: This needs to be a different exception type
                await TestToken(page, "View Access Token", response?.AccessToken ?? throw new ArgumentNullException(nameof(response.AccessToken)));
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewRefreshToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewRefreshToken)}", async (page, response) =>
            {
                // CT: This needs to be a different exception type
                await TestToken(page, "View Refresh Token", response?.RefreshToken ?? throw new ArgumentNullException(nameof(response.RefreshToken)));
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "Jane", "Wilson")]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY, "Mary", "Moss")] // Also test for Energy DH
        public async Task AC06_Consents_ViewUserInfo(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts, string expectedGivenName, string expectedFamilyName)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewUserInfo)}", async (page, response) =>
            {
                await TestAsync($"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_ViewUserInfo)}", async (page) =>
                {
                    var expected = new (string, string?)[]
                    {
                    ("given_name", expectedGivenName),
                    ("family_name", expectedFamilyName),
                    ("name", $"{expectedGivenName} {expectedFamilyName}"),
                    ("aud", ASSERT_JSON2_ANYVALUE),
                    ("iss", ASSERT_JSON2_ANYVALUE),
                    ("sub", ASSERT_JSON2_ANYVALUE),
                    };

                    await TestInfo(page, "UserInfo", "User Info", "200", expected);
                });
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Introspect(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Introspect)}", async (page, response) =>
            {
                // CT: This needs to be a different exception type
                var expected = new (string, string?)[]
                {
                    ("cdr_arrangement_id", response?.CDRArrangementID ?? throw new ArgumentNullException(nameof(response.CDRArrangementID))),
                    ("scope", ASSERT_JSON2_ANYVALUE),
                    ("exp", ASSERT_JSON2_ANYVALUE),
                    ("active", "True"),
                };

                await TestInfo(page, "Introspect", "Introspection", "200", expected);
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING,
            "openid profile common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:regular_payments:read bank:payees:read")]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY,
           "openid profile common:customer.basic:read common:customer.detail:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.der:read energy:electricity.usage:read")] // Also test for Energy DH
        public async Task AC06_Consents_Refresh_Access_Token(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts, string expectedScope)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Refresh_Access_Token)}", async (page, response) =>
            {
                var expected = new (string, string?)[]
                {
                    ("id_token", ASSERT_JSON2_ANYVALUE),
                    ("access_token", ASSERT_JSON2_ANYVALUE),
                    ("refresh_token", ASSERT_JSON2_ANYVALUE),
                    ("expires_in", ACCESSTOKENLIFETIMESECONDS),
                    ("token_type", "Bearer"),
                    ("scope", expectedScope),
                    ("cdr_arrangement_id", ASSERT_JSON2_ANYVALUE),
                };

                await TestInfo(page, "Refresh Access Token", "Refresh Access Token", "200", expected);
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Revoke_Arrangement(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Revoke_Arrangement)}", async (page, response) =>
            {
                await TestInfo(page, "Revoke Arrangement", "Revoke Arrangement", "204");
                await ScreenshotAsync(page, "-Modal");

                await page.Locator("div#modal-info >> div.modal-footer >> a >> text=Refresh Page").ClickAsync();
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Revoke_AccessToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Revoke_AccessToken)}", async (page, response) =>
            {
                await TestInfo(page, "Revoke Access Token", "Revoke Access Token", "200");
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Revoke_RefreshToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Revoke_RefreshToken)}", async (page, response) =>
            {
                await TestInfo(page, "Revoke Refresh Token", "Revoke Refresh Token", "200");
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Delete_Local(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC06_Consents_Delete_Local)}", async (page, response) =>
            {
                await Consents_DeleteLocal(page);
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC07_PAR(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC07_PAR)}";
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                       ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, dhBrandId, cdrArrangement: cdrArrangement!.CDRArrangementID, sharingDuration: SHARING_DURATION);
                    await parPage.ClickInitiatePar();
                    await parPage.ClickAuthorizeUrl();

                    // Act/Assert - Perform consent and authorisation
                    await ConsentAndAuthorisation2(page, customerId, customerAccounts);
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        public async Task AC08_ConsumerDataSharing_Banking(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC08_ConsumerDataSharing_Banking)}";
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                       ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Banking\")").ClickAsync();
                    await page.Locator("h2 >> text=Data Sharing - Banking").TextContentAsync();
                    await Task.Delay(2000);  // give screen time to refresh
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        public async Task AC08_ConsumerDataSharing_Banking_AccountsGet(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC08_ConsumerDataSharing_Banking_AccountsGet)}";
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                       ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Banking\")").ClickAsync();
                    await page.Locator("h2 >> text=Data Sharing - Banking").TextContentAsync();

                    // Arrange - Get Swagger iframe
                    var iFrame = page.FrameByUrl($"{WEB_URL}/{SWAGGER_BANKING_IFRAME}") ?? throw new Exception($"IFrame not found - {SWAGGER_BANKING_IFRAME}");

                    // Wait for the CDR arrangement <select> to be added and populated.
                    System.Threading.Thread.Sleep(10000);

                    // Arrange - Select CDR arrangemment
                    await iFrame.SelectOptionAsync(
                        "select",
                        new string[] {
                            cdrArrangement!.CDRArrangementID ?? throw new NullReferenceException(nameof(cdrArrangement.CDRArrangementID))
                        },
                        new FrameSelectOptionOptions() { Timeout = 90000 }
                    );

                    // Arrange - Click GET​/banking​/accountsGet Accounts
                    await iFrame.ClickAsync("//div[@id='operations-Banking_Accounts-listBankingAccounts']//button[1]");

                    // Arrange - Click Try it out
                    await iFrame.ClickAsync("text=Try it out");

                    // Arrange - Set x-v
                    await iFrame.FillAsync("[placeholder=\"x-v\"]", "2");

                    // Act - Click Execute
                    await iFrame.ClickAsync("text=Execute");

                    // Assert - Status code should be 200 
                    var statusCode = await iFrame.Locator("div.responses-inner > div > div > table > tbody > tr > td.response-col_status").TextContentAsync();
                    statusCode.Should().Be("200");
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)]
        public async Task AC08_ConsumerDataSharing_Energy(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC08_ConsumerDataSharing_Energy)}";
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                        ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Energy\")").ClickAsync();
                    await page.Locator("h2 >> text=Data Sharing - Energy").TextContentAsync();
                    await Task.Delay(2000);  // give screen time to refresh
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)]
        public async Task AC08_ConsumerDataSharing_Energy_AccountsGet(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC08_ConsumerDataSharing_Energy_AccountsGet)}";
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                        ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Energy\")").ClickAsync();
                    await page.Locator("h2 >> text=Data Sharing - Energy").TextContentAsync();

                    // Arrange - Get Swagger iframe
                    var iFrame = page.FrameByUrl($"{WEB_URL}/{SWAGGER_ENERGY_IFRAME}") ?? throw new Exception($"IFrame not found - {SWAGGER_ENERGY_IFRAME}");

                    // Wait for the CDR arrangement <select> to be added and populated.
                    System.Threading.Thread.Sleep(10000);

                    // Arrange - Select CDR arrangemment
                    await iFrame.SelectOptionAsync(
                        "select",
                        new string[] {
                            cdrArrangement!.CDRArrangementID ?? throw new NullReferenceException(nameof(cdrArrangement.CDRArrangementID))
                        },
                        new FrameSelectOptionOptions() { Timeout = 90000 }
                    );

                    // Arrange - Click GET​/energy​/accountsGet Energy Accounts
                    await iFrame.ClickAsync("//div[@id='operations-Energy_Accounts-listEnergyAccounts']//button[1]");

                    // Arrange - Click Try it out
                    await iFrame.ClickAsync("text=Try it out");

                    // Arrange - Set x-v
                    await iFrame.FillAsync("[placeholder=\"x-v\"]", "1");

                    // Act - Click Execute
                    await iFrame.ClickAsync("text=Execute");

                    // Assert - Status code should be 200 
                    var statusCode = await iFrame.Locator("div.responses-inner > div > div > table > tbody > tr > td.response-col_status").TextContentAsync();
                    statusCode.Should().Be("200");
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        public async Task AC08_ConsumerDataSharing_Common_StatusGet(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string testName = $"{nameof(US23863_MDR_E2ETests)} - {nameof(AC08_ConsumerDataSharing_Common_StatusGet)}";
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                       ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Common\")").ClickAsync();
                    await page.Locator("h2 >> text=Data Sharing - Common").TextContentAsync();

                    // Arrange - Get Swagger iframe
                    var iFrame = page.FrameByUrl($"{WEB_URL}/{SWAGGER_COMMON_IFRAME}") ?? throw new Exception($"IFrame not found - {SWAGGER_BANKING_IFRAME}");

                    // Wait for the CDR arrangement <select> to be added and populated.
                    System.Threading.Thread.Sleep(10000);

                    // Arrange - Select CDR arrangement
                    await iFrame.SelectOptionAsync(
                        "select",
                        new string[] {
                            cdrArrangement!.CDRArrangementID ?? throw new NullReferenceException(nameof(cdrArrangement.CDRArrangementID))
                        },
                        new FrameSelectOptionOptions() { Timeout = 90000 }
                    );

                    // Arrange - Click GET/discovery/statu
                    await iFrame.ClickAsync("//div[@id='operations-Data_Holder_Operations-getOutages']//button[1]");

                    // Arrange - Click Try it out
                    await iFrame.ClickAsync("text=Try it out");

                    // Arrange - Set x-v
                    await iFrame.FillAsync("[placeholder=\"x-v\"]", "1");

                    // Act - Click Execute
                    await iFrame.ClickAsync("text=Execute");

                    // Assert - Status code should be 200 
                    var statusCode = await iFrame.Locator("div.responses-inner > div > div > table > tbody > tr > td.response-col_status").TextContentAsync();
                    statusCode.Should().Be("200");
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Fact]
        public async Task AC10_PrivateKeyJWTGenerator()
        {

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
                await TestResults(page, "aud", $"https://{BaseTest.HOSTNAME_REGISTER}:7001/idp/connect/token");
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
                Assert.True(true);
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
                Assert.True(true);
            });
        }

    }
}
