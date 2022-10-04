using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class US23863_MDR_E2ETests_v2 : BaseTest_v2, IClassFixture<TestFixture>
    {
        private const string SWAGGER_BANKING_IFRAME = "cds-banking/index.html";
        private const string SWAGGER_ENERGY_IFRAME = "cds-energy/index.html";
        private const string SWAGGER_COMMON_IFRAME = "cds-common/index.html";

        private const string SHARING_DURATION = "100000";

        // Pre-generated ID Token used in IDTokenhelper test
        const string IDTOKEN = @"eyJhbGciOiJSU0EtT0FFUCIsImVuYyI6IkEyNTZHQ00iLCJraWQiOiJlMmM2ZWFmZjdmYWQwODFjNzhiMjJkNjNiYmI1N" +
            "jdlODYwNzBlNTFlYjE0NzZhYmNhNmEyMzNiY2U4Y2ExNGJhIn0.fp9-2--a7mM70FsVL1c5MacViVkPcI-CZCGXyjV4F1T-RZuUsMaw9sCkjNrnE3" +
            "LR9nnBOYploFXljRo6_ZkH5PJHtSfnoF8GRnJJHJ74dvJC3MqRsvGUl8uS5P2sEW2TexXQlhdKwFA3REM1khpVNAA1FZyFK3iv7IucipnL0UcO1H-" +
            "23iBb7puOdEm6_3Sndu2ycACt5in-gO2UIWHFeaUDwLdr7iCBH4fzn5jupSJdyZ-9iU2ahOhf6iJFAwYM4R7CEPRTr8h8JqXBW3cqOcAktK0jScmv" +
            "U3k2zOqtiwTxZ3yTwNEinSSvDxyxofKmiXsQDkgOzbJt6YPAq2c9gw.o-hgXFF8FzFUd1gY.NX-J0RWfE53KksdvhNK9pcXjXvLicaFO3N1N-ivAV" +
            "G3HL_7k7f4L2d7B3Elh4UXiiV84xuS2wBltNT2XcOAEMZuJd6-e1Octuhzzc9o8pFQEtP-72vzpo6K6wbdSgFwqPNQ4xGJTArS2BCG1v_rfiUNqM7" +
            "Y-igdsGImk3DoCBtsq-PWaWZgwj0d4ci5De4EkMpb3UZrk9UlZGGpgOH0rqGEPQEhO41M-7XeBw-cVqyGwaS6c3iwSUZHHCLgluZ-AfJ3ebfq9ya3" +
            "u02de95C1YMhN04BA9Oj_tkTUA4XvyfXnxd_cXtjPUUjAo2Tvmc1YxU-tfNgwLgW3s2jFdkeJnHj7vwXzAbiAthIleD9D2LrYUN_vNWI0Lc570R1g" +
            "GVXVFl_IfUMhZBSRYTl4z15NGdTwTopusNDVnOKQrKu5FsRWa0g1h80Fnmiw4sNRByFzxY_qziz67aUbJK1qz57FdBhHy85GkdK90Zd7kTuITYkPc" +
            "c7_Srtu6ZHMWNRbUxvEHF_YHnTVWiH3DCiGYwrPCYEcXLutoY2A0nJGBAPsgCYnZ8_FvsQCsUno-PJyyWhjZ98mXcmtNIBOw5qArH7E520p586FtO" +
            "JjS0zbRL7UjNxK2IcSB01-9WkWVxTxYrQnD8fzobnNPCf6esGGyPjU-PD-bstCQ1PUhA5OSg7MmBiTyRZYfvj59_rkhqdLovYCqyZLu9NbySLoFiq" +
            "hnCXmmCROv6U_-KDW0-y30Uf9s_ke00l5kBSs9F4h8shDF5rXySL8HeAwNt_0JY4Q1pg6zu2PISuXVmcnizCZoCFaDTQ1ReQKHBfsDss7g680F6OY" +
            "_UFxzYwxowIctfZzKQtT0q0J2xghgjPP-U47ng8VTQX2b_8TmMMWXiEvumxEb9gHp_i6246uHOeuiBu2LVGniAJkbTB2DHgICXgRYSd_pQTi7GBsu" +
            "m4ZzQeVIVC7UKxc1OOHkLLypzPRK2bwOIg0SMZjnugCLbmQUyJxyB7ht5iA2psX-EavKeR-KjTqOWFSr5byOIbMUSLY44K0Al503u7NTvH5JVUH4y" +
            "y6zI8sgiaQTcfAXo1YKhmHqJPf4VbQZRMTs3_2tdu5rNl-BSytbLnKrmFvL85RoKAq6wqE3Clk4BLJQ0CNoBvm-nObhzhQ_mhbXyRXn7A1vKEkf_p" +
            "KHv3-dKBlX4-jdyFJAwd_HPqb6gtBFsKROlpQwlCxGbEV5GF2iN0az03r6RPveqxvkP4RpgfG8VaXvKyW9xi4lENVelt_Ep11BXHsrJGKupIaIdW" +
            "IhUZu90VJYIezKKztLycPlxs7oOkEDTIfVrO1HeuEOHk1CCgxBiWtqZiKO2ELtE5mj2fee35fWFQ31muNiXBVD2DgHabnb_S0ZXHzY6MR368Rj0h" +
            "xqxiDVc6ppQcVt02XYL4HLz4mBExyvy0F13OfuEkP5tWlaHkK8k90eISkn7aKcoMVV5JSFjqPasA_dY91Gbpq0itHDtaEIzgcOYWP-u8LUGJGLF4N" +
            "qWkBfZUEGxgPIQ1ynRGwwUwnrh8xQSuvG_Phid0DGHm0Kxz_j0Vyvr60TpFoYNwGPH6vQ3bsiSs2JlxLuOdC-Kw6YVahxsGkE6b9ZKn9HiW6tmxj-" +
            "CCf1u4tYiYxW48ceOcivydgT-mIKAdQeClTIbAJdJq4wY_936f5GWIJxM-wJ78qwFTN4R7zG4vINi2gd5000h4JjCaYTyX9v989Fzuq5SIkNHRb8E" +
            "hSLcMA_3De7epFZCnImEhkKUQPHxwaW9kDBExy-sVJ2ku9EN6k4AZAM60lCMAdOGWwvOFN-pW9k9DBDmZTfg1kjfnbn4guIfw7lJKp2iy-0TGVhDD" +
            "3MN3BTrucmURy6_kpXWRLGuLd3fEj-bQAwD4sMgLVe_GWh6f1TFo7hUfrNmwuXLjqtFMjMgWXbo8zM4VJDL6fCO_U0HZzXx3CGzxuSETWQxF9EeDG" +
            "kSpwIdweEmFl1finL1g.gTClNE4BztTzJIpe8BcNRw";

        static string GetClientId()
        {
            using var mdrConnection = new SqlConnection(BaseTest_v2.DATARECIPIENT_CONNECTIONSTRING);
            mdrConnection.Open();

            using var selectCommand = new SqlCommand($"select clientid from registration", mdrConnection);
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

        static async Task<ConsentAndAuthorisationResponse> ConsentAndAuthorisation2(IPage page, string customerId = CUSTOMERID_BANKING, string customerAccounts = CUSTOMERACCOUNTS_BANKING)
        {
            // Act - Enter Customer ID
            await page.Locator("[placeholder=\"Your Customer ID\"]").FillAsync(customerId);
            await page.Locator("text=Continue").ClickAsync();

            // Act - Wait for OTP, then enter it, and click Continue
            await Task.Delay(5000);
            await page.Locator("[placeholder=\"Enter 6 digit One Time Password\"]").FillAsync("000789");
            await page.Locator("text=Continue").ClickAsync();

            // Act - Select accounts
            foreach (var customerAccount in customerAccounts.Split(','))
            {
                await page.Locator($"text={customerAccount}").ClickAsync();
            }

            // Act - Click Continue and I confirm
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

        static async Task<ConsentAndAuthorisationResponse> NewConsentAndAuthorisationWithPAR(
            IPage page, 
            string dhClientId, 
            string customerId = CUSTOMERID_BANKING, 
            string customerAccounts = CUSTOMERACCOUNTS_BANKING,
            string dhBrandId = DH_BRANDID)
        {
            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=PAR").ClickAsync();
            await page.Locator("h2 >> text=Pushed Authorisation Request (PAR)").TextContentAsync();

            // Arrange - Set Client ID
            await page.Locator("select[name=\"RegistrationId\"]").SelectOptionAsync(new[] { $"{dhClientId!}|||{dhBrandId}"  });
            await page.Locator("select[name=\"RegistrationId\"]").ClickAsync();  // there is JS that runs on the click event, so simulate click here
            await Task.Delay(2000);
            // Arrange - Set CdrArrangementId
            //await page.Locator("select[name=\"CdrArrangementId\"]").SelectOptionAsync(new[] { cdrArrangement!.CDRArrangementID ?? throw new NullReferenceException(nameof(cdrArrangement.CDRArrangementID)) });
            // Arrange - Set Sharing Duration
            await page.Locator("input[name=\"SharingDuration\"]").FillAsync(SHARING_DURATION);

            // Act - Click Initiate PAR button
            await page.Locator("div.form >> text=Initiate PAR").ClickAsync();

            // Act - Click request uri
            await page.Locator("p.results > a").ClickAsync();

            return await ConsentAndAuthorisation2(page, customerId, customerAccounts);
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
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC01_HomePage)}", async (page) =>
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
                await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Common\")").TextContentAsync();
                await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Banking\")").TextContentAsync();
                await page.Locator("span:text(\"Consumer Data Sharing\") + ul >> a:text(\"Energy\")").TextContentAsync();
                await page.Locator("a >> text=PAR").TextContentAsync();
                await page.Locator("span >> text=Utilities").TextContentAsync();
                await page.Locator("a >> text=ID Token Helper").TextContentAsync();
                await page.Locator("a >> text=Private Key JWT Generator").TextContentAsync();
            });
        }

        static private async Task DataHolders_Discover(IPage page, string industry = "ALL", string version = "2", int? expectedRecords = 32, string? expectedError = null)
        {
            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=Discover Data Holders").ClickAsync();
            await page.Locator("h2 >> text=Discover Data Holders").TextContentAsync();

            // Arrange - Set industry
            if (String.IsNullOrEmpty(industry)) // Clear industry
            {
                await page.Locator("select[name=\"Industry\"]").SelectOptionAsync(new SelectOptionValue[] { });
            }
            else
            {
                await page.Locator("select[name=\"Industry\"]").SelectOptionAsync(new[] { industry switch
                    {
                        // "" => "", // Doesn't work for clearing, use SelectOptionAsync(new SelectOptionValue[] { }) instead (see above)
                        "ALL" => "0",
                        "BANKING" => "1",
                        "ENERGY" => "2",
                        "TELCO" => "3",
                        _ => throw new ArgumentOutOfRangeException($"{nameof(industry)}")
                    }});
            }

            // Arrange - Set version
            await page.Locator("input[name=\"Version\"]").FillAsync(version);

            // Act - Click Refresh button
            await page.Locator(@"h5:has-text(""Refresh Data Holders"") ~ div.card-body >> input:has-text(""Refresh"")").ClickAsync();

            // Assert - Check refresh was successful
            var footer = page.Locator(@"h5:has-text(""Refresh Data Holders"") ~ div.card-footer");
            var text = await footer.InnerTextAsync();
            if (expectedError != null)
            {
                text.Should().Be(expectedError);
            }
            else
            {
                if (expectedRecords != -1) // -1 = don't bother checking
                {
                    text.Should().Be($"OK: {expectedRecords} data holder brands added. 0 data holder brands updated.");
                }
            }
        }

        [Fact]
        public async Task AC02_01_DiscoverDataHolders_Banking()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC02_01_DiscoverDataHolders_Banking)}", async (page) =>
            {
                await DataHolders_Discover(page, "BANKING", "1", 30);
            });
        }

        [Fact]
        public async Task AC02_02_DiscoverDataHolders_Energy()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC02_02_DiscoverDataHolders_Energy)}", async (page) =>
            {
                await DataHolders_Discover(page, "ENERGY", "2", 2);
            });
        }

        [Fact]
        public async Task AC02_03_DiscoverDataHolders_Telco()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC02_03_DiscoverDataHolders_Telco)}", async (page) =>
            {
                await DataHolders_Discover(page, "TELCO", "2", 0);  // Currently no Telco dataholders, so 0 = no additional dataholders loaded
            });
        }

        [Fact]
        public async Task AC02_04_DiscoverDataHolders_AnyIndustry()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC02_04_DiscoverDataHolders_AnyIndustry)}", async (page) =>
            {
                await DataHolders_Discover(page, "ALL", "2", 0);  // Should have loaded all dataholders by now, so 0 = no additional dataholders loaded
            });
        }

        [Theory]
        //[InlineData("ALL", "", null, "BadRequest - Bad Request")] // DF: won't fail anymore
        [InlineData("ALL", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("ALL", "foo", null, "BadRequest - Bad Request")]
        //[InlineData("BANKING", "", null, "BadRequest - Bad Request")] // DF: won't fail anymore
        [InlineData("BANKING", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("BANKING", "foo", null, "BadRequest - Bad Request")]
        //[InlineData("ENERGY", "", null, "BadRequest - Bad Request")] // DF: won't fail anymore
        [InlineData("ENERGY", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("ENERGY", "foo", null, "BadRequest - Bad Request")]
        //[InlineData("TELCO", "", null, "BadRequest - Bad Request")] // DF: won't fail anymore
        [InlineData("TELCO", "3", null, "NotAcceptable - Not Acceptable")]
        [InlineData("TELCO", "foo", null, "BadRequest - Bad Request")]
        public async Task AC02_99_DiscoverDataHolders(string industry = "ALL", string version = "2", int? expectedRecords = 32, string? expectedError = null)
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC02_99_DiscoverDataHolders)} - Industry={industry} - Version={version}", async (page) =>
            {
                await DataHolders_Discover(page, industry, version, expectedRecords, expectedError);
            });
        }

        static private async Task SSA_Get(IPage page, string industry, string version, string drBrandId, string drSoftwareProductId, string expectedMessage)
        {
            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=Get SSA").ClickAsync();
            await page.Locator("h2 >> text=Get Software Statement Assertion").TextContentAsync();

            // Set version
            await page.Locator("input[name=\"Version\"]").FillAsync(version);
            // Set brandId
            await page.Locator("input[name=\"BrandId\"]").FillAsync(drBrandId);
            // Set softwareProductId
            await page.Locator("input[name=\"SoftwareProductId\"]").FillAsync(drSoftwareProductId);
            // Set industry
            await page.Locator("select[name=\"Industry\"]").SelectOptionAsync(new[] { industry switch
                    {
                        // "" => "", // Doesn't work for clearing, use SelectOptionAsync(new SelectOptionValue[] { }) instead (see above)
                        "ALL" => "0",
                        "BANKING" => "1",
                        "ENERGY" => "2",
                        "TELCO" => "3",
                        _ => throw new ArgumentOutOfRangeException($"{nameof(industry)}")
                    }});

            // Act - Click Refresh button
            await page.Locator(@"h5:has-text(""Get SSA"") ~ div.card-body >> input:has-text(""Get SSA"")").ClickAsync();

            // Assert - Check refresh was successful, card-footer should be showing OK - SSA Generated
            var footer = page.Locator(@"h5:has-text(""Get SSA"") ~ div.card-footer");
            var text = await footer.InnerTextAsync();
            text.Should().StartWith(expectedMessage);
        }

        [Theory]
        //[InlineData("", DR_BRANDID, DR_SOFTWAREPRODUCTID, "BadRequest")] // DF: no longer an error
        [InlineData("BANKING", "1", DR_BRANDID, DR_SOFTWAREPRODUCTID, "OK - SSA Generated")]
        [InlineData("BANKING", "2", DR_BRANDID, DR_SOFTWAREPRODUCTID, "OK - SSA Generated")]
        [InlineData("BANKING", "3", DR_BRANDID, DR_SOFTWAREPRODUCTID, "OK - SSA Generated")]
        [InlineData("BANKING", "4", DR_BRANDID, DR_SOFTWAREPRODUCTID, "NotAcceptable")]
        public async Task AC03_GetSSA(string industry, string version, string drBrandId, string drSoftwareProductId, string expectedMessage)
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC03_GetSSA)} - Version={version} - DR_BrandId={drBrandId} - DR_SoftwareProductId={drSoftwareProductId}", async (page) =>
            {
                await SSA_Get(page, industry, version, drBrandId, drSoftwareProductId, expectedMessage);
            });
        }

        private class DCRResponse
        {
            [JsonProperty("client_id")]
            public string? ClientId { get; set; }
        }

        // Create Client Registration returning DH client ID of client that was registered
        static private async Task<string> ClientRegistration_Create(IPage page, string dhBrandId, string drBrandId, string drSoftwareProductId)
        {
            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=Dynamic Client Registration").ClickAsync();
            await page.Locator("h2 >> text=Dynamic Client Registration").TextContentAsync();

            // Set data holder brand id
            await page.Locator("select[name=\"DataHolderBrandId\"]").SelectOptionAsync(new[] { dhBrandId });

            // Assert - Check software product id
            (await page.Locator("input[name=\"SoftwareProductId\"]").InputValueAsync()).Should().Be(drSoftwareProductId);

            // Act - Click create button
            await page.Locator(@"h5:has-text(""Create Client Registration"") ~ div.card-body >> input:has-text(""Register"")").ClickAsync();

            // Assert - Check client was registered
            await page.Locator(@"h5:has-text(""Create Client Registration"") ~ div.card-footer:has-text(""Created - Registered"")").TextContentAsync();

            // Assert - Get json result
            var json = await page.Locator(@"h5:has-text(""Create Client Registration"") ~ div.card-footer >> pre").InnerTextAsync();

            // Deserialise response and return DH client id
            DCRResponse dcrResponse = JsonConvert.DeserializeObject<DCRResponse>(json) ?? throw new NullReferenceException(nameof(json));
            return dcrResponse.ClientId ?? throw new NullReferenceException(nameof(dcrResponse.ClientId));
        }

        // Delete Client Registration
        static private async Task ClientRegistration_Delete(IPage page)
        {
            await page.GotoAsync(WEB_URL);
            await page.Locator("a >> text=Dynamic Client Registration").ClickAsync();
            await page.Locator("h2 >> text=Dynamic Client Registration").TextContentAsync();
            await page.Locator("text=Delete").ClickAsync();
            await page.Locator("text=No existing registrations found.").TextContentAsync();
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID)]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID)] // Also test for Energy DH 
        public async Task AC04_DynamicClientRegistration(string dhBrandId = DH_BRANDID, string drBrandId = DR_BRANDID, string drSoftwareProductId = DR_SOFTWAREPRODUCTID)
        {
            try
            {
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                });

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC04_DynamicClientRegistration)} - DH_BrandId={dhBrandId} - DR_BrandId={drBrandId} - DR_SoftwareProductId={drSoftwareProductId}", async (page) =>
                {
                    await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId);
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

        static private async Task<ConsentAndAuthorisationResponse> ConsentAndAuthorisation(
            IPage page,
            string dhClientId,
            string dhBrandId,
            string drBrandId,
            string drSoftwareProductId,
            string customerId,
            string customerAccounts)
        {
            ConsentAndAuthorisationResponse? res = null;

            // Arrange - Goto home page, click menu button, check page loaded
            await page.GotoAsync(WEB_URL);
            await page.Locator("a:text(\"Consent and Authorisation\")").ClickAsync();
            await page.Locator("h2 >> text=Consent and Authorisation").TextContentAsync();

            // Arrange - Set Client ID
            await page.Locator("select[name=\"RegistrationId\"]").SelectOptionAsync($"{dhClientId}|||{dhBrandId}");
            await page.Locator("select[name=\"RegistrationId\"]").ClickAsync();  // there is JS that runs on the click event, so simulate click here
            await Task.Delay(2000);

            // Arrange - Set Sharing Duration
            await page.Locator("input[name=\"SharingDuration\"]").FillAsync(SHARING_DURATION);
            // Arrange - Click Construct Authoriation URI button
            await page.Locator("text=Construct Authorisation Uri").ClickAsync();

            // Act - Click Authorisation URI link
            await page.Locator("p.results > a").ClickAsync();

            // Act/Assert - Perform consent and authorisation
            res = await ConsentAndAuthorisation2(page, customerId, customerAccounts);

            return res ?? throw new ArgumentNullException($"Expected {nameof(ConsentAndAuthorisationResponse)}");
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Don't test for Energy as error will be raised due to FAPI 1.0 conformance
        public async Task AC05_ConsentAndAuthorisation(
            string dhBrandId = DH_BRANDID,
            string drBrandId = DR_BRANDID,
            string drSoftwareProductId = DR_SOFTWAREPRODUCTID,
            string customerId = CUSTOMERID_BANKING,
            string customerAccounts = CUSTOMERACCOUNTS_BANKING)
        {
            try
            {
                string? dhClientId = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId) ?? throw new NullReferenceException(nameof(dhClientId));
                });

                ConsentAndAuthorisationResponse? res = null;

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC05_ConsentAndAuthorisation)} -  DH_BrandId={dhBrandId}", async (page) =>
                {
                    res = await ConsentAndAuthorisation(page, dhClientId!, dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts);
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        public delegate Task ConsentsDelegate(IPage page, ConsentAndAuthorisationResponse response);
        public async Task Test_Consents(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts, string testName, ConsentsDelegate test)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await ConsentAndAuthorisation(page, dhClientId, dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts);
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
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewIDToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_ViewIDToken)}", async (page, response) =>
            {
                await TestToken(page, "View ID Token", response?.IDToken ?? throw new ArgumentNullException(nameof(response.IDToken)));
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewAccessToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_ViewAccessToken)}", async (page, response) =>
            {
                await TestToken(page, "View Access Token", response?.AccessToken ?? throw new ArgumentNullException(nameof(response.AccessToken)));
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewRefreshToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_ViewRefreshToken)}", async (page, response) =>
            {
                await TestToken(page, "View Refresh Token", response?.RefreshToken ?? throw new ArgumentNullException(nameof(response.RefreshToken)));
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        // [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_ViewUserInfo(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_ViewUserInfo)}", async (page, response) =>
            {
                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_ViewUserInfo)}", async (page) =>
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
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Introspect(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_Introspect)}", async (page, response) =>
            {
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
            "openid profile cdr:registration bank:accounts.basic:read bank:transactions:read common:customer.basic:read")]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY,
        //    "openid profile cdr:registration energy:accounts.basic:read energy:accounts.concessions:read common:customer.basic:read")] // Also test for Energy DH
        public async Task AC06_Consents_Refresh_Access_Token(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts, string expectedScope)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_Refresh_Access_Token)}", async (page, response) =>
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
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Revoke_Arrangement(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_Revoke_Arrangement)}", async (page, response) =>
            {
                await TestInfo(page, "Revoke Arrangement", "Revoke Arrangement", "204");
                await ScreenshotAsync(page, "-Modal");

                await page.Locator("div#modal-info >> div.modal-footer >> a >> text=Refresh Page").ClickAsync();
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Revoke_AccessToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_Revoke_AccessToken)}", async (page, response) =>
            {
                await TestInfo(page, "Revoke Access Token", "Revoke Access Token", "200");
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Revoke_RefreshToken(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_Revoke_RefreshToken)}", async (page, response) =>
            {
                await TestInfo(page, "Revoke Refresh Token", "Revoke Refresh Token", "200");
            });
        }

        private static async Task Consents_DeleteLocal(IPage page)
        {
            await TestInfo(page, "Delete (local)", "Delete Arrangement", "204");
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC06_Consents_Delete_Local(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            await Test_Consents(dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts, $"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC06_Consents_Delete_Local)}", async (page, response) =>
            {
                await Consents_DeleteLocal(page);
            });
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        //[InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)] // Also test for Energy DH
        public async Task AC07_PAR(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await ConsentAndAuthorisation(page, dhClientId, dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts)
                        ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC07_PAR)}", async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    await page.Locator("a >> text=PAR").ClickAsync();
                    await page.Locator("h2 >> text=Pushed Authorisation Request (PAR)").TextContentAsync();

                    // Arrange - Set Client ID
                    await page.Locator("select[name=\"RegistrationId\"]").SelectOptionAsync(new[] { $"{dhClientId}|||{dhBrandId}" });
                    await page.Locator("select[name=\"RegistrationId\"]").ClickAsync();  // there is JS that runs on the click event, so simulate click here
                    await Task.Delay(2000);
                    // Arrange - Set CdrArrangementId
                    await page.Locator("select[name=\"CdrArrangementId\"]").SelectOptionAsync(new[] { cdrArrangement!.CDRArrangementID ?? throw new NullReferenceException(nameof(cdrArrangement.CDRArrangementID)) });
                    // Arrange - Set Sharing Duration
                    await page.Locator("input[name=\"SharingDuration\"]").FillAsync(SHARING_DURATION);

                    // Act - Click Initiate PAR button
                    await page.Locator("div.form >> text=Initiate PAR").ClickAsync();

                    // Act - Click request uri
                    await page.Locator("p.results > a").ClickAsync();

                    // Act/Assert - Perform consent and authorisation
                    await ConsentAndAuthorisation2(page, customerId, customerAccounts);
                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        public async Task AC08_ConsumerDataSharing_Banking(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await ConsentAndAuthorisation(page, dhClientId, dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts);
                });

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC08_ConsumerDataSharing_Banking)}", async (page) =>
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
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING)]
        public async Task AC08_ConsumerDataSharing_Banking_AccountsGet(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await ConsentAndAuthorisation(page, dhClientId, dhBrandId, drBrandId, drSoftwareProductId, customerId, customerAccounts)
                        ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC08_ConsumerDataSharing_Banking_AccountsGet)}", async (page) =>
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
                    await iFrame.ClickAsync("text=GET​/banking​/accountsGet Accounts");

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
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)]
        public async Task AC08_ConsumerDataSharing_Energy(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                        ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC08_ConsumerDataSharing_Energy)}", async (page) =>
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
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(DH_BRANDID_ENERGY, DR_BRANDID, DR_SOFTWAREPRODUCTID, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY)]
        public async Task AC08_ConsumerDataSharing_Energy_AccountsGet(string dhBrandId, string drBrandId, string drSoftwareProductId, string customerId, string customerAccounts)
        {
            try
            {
                string? dhClientId = null;
                ConsentAndAuthorisationResponse? cdrArrangement = null;
                await ArrangeAsync(async (page) =>
                {
                    await DataHolders_Discover(page, "ALL", "2", -1); // get all dh brands
                    await SSA_Get(page, "ALL", "3", drBrandId, drSoftwareProductId, "OK - SSA Generated");
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                    cdrArrangement = await NewConsentAndAuthorisationWithPAR(page, dhClientId, customerId, customerAccounts, dhBrandId)
                        ?? throw new NullReferenceException(nameof(cdrArrangement));
                });

                await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC08_ConsumerDataSharing_Energy_AccountsGet)}", async (page) =>
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
                    await iFrame.ClickAsync("text=GET​/energy​/accountsGet Energy Accounts");

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
                    try { await Consents_DeleteLocal(page); } catch { };
                });
            }
        }

        [Theory]
        [InlineData(IDTOKEN)]
        public async Task AC09_IDTokenHelper(string encryptedToken)
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC09_IDTokenHelper)}", async (page) =>
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
                await TestResults(page, "nbf", "1653444726");
                await TestResults(page, "exp", "1653445026");
                // await TestResults(page, "iss", $"https://{BaseTest.HOSTNAME_DATAHOLDER}:8001");
                await TestResults(page, "iss", $"https://localhost:8001"); // token is const, it was created on localhost, we are just checking the decryption of token works and not where it was issued
                await TestResults(page, "aud", "17d9e269-96d7-4b2a-bd0d-6767ecff965a");
                await TestResults(page, "nonce", "78b2faa4-954c-45de-af0e-33d1424732c7");
                await TestResults(page, "iat", "1653444725");
                await TestResults(page, "at_hash", "jybXQh-TiklHgnSjITgtoA");
                await TestResults(page, "s_hash", "DlHToSN2VJtaskuOQYV2sw");
                await TestResults(page, "auth_time", "1653444711");
                await TestResults(page, "idp", "local");
                await TestResults(page, "cdr_arrangement_id", "d6505b4b-001c-4b98-ae36-1b4d29ea0ef4");
                await TestResults(page, "sub", "mYnxnN7keNe/eiR2j9OZ+axM5WUUa5IdTUEBxyqQWUToZbS9MlaxYYPEhWOhJUQl");
                await TestResults(page, "name", "Kamilla Smith");
                await TestResults(page, "family_name", "Smith");
                await TestResults(page, "given_name", "Kamilla");
                await TestResults(page, "updated_at", "1614623400");
                await TestResults(page, "acr", "urn:cds.au:cdr:2");
                await TestResults(page, "amr", "pwd");
            });
        }

        [Fact]
        public async Task AC10_PrivateKeyJWTGenerator()
        {
            // await ArrangeAsync(async () => { });

            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC10_PrivateKeyJWTGenerator)}", async (page) =>
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
                await TestResults(page, "aud", $"https://{BaseTest_v2.HOSTNAME_REGISTER}:7001/idp/connect/token");
            });
        }

        [Fact]
        public async Task AC11_Settings()
        {
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC11_Settings)}", async (page) =>
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
            await TestAsync($"{nameof(US23863_MDR_E2ETests_v2)} - {nameof(AC12_About)}", async (page) =>
            {
                // Arrange - Goto home page, click menu button, check page loaded                
                await page.GotoAsync(WEB_URL);
                await page.Locator("a >> text=About").ClickAsync();
                await page.Locator("h2 >> text=About").TextContentAsync();
            });
        }
    }
}