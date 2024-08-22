using CDR.DataRecipient.E2ETests.Pages;
using CDR.DataRecipient.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class US46352_MDR_E2ETests_AuthCodeFlowJarm : BaseTest, IClassFixture<TestFixture>
    {

        private const string WRONG_CERTIFICATE_FILENAME = "Certificates/client.pfx";
        private const string WRONG_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";
        private const string DEFAULT_KID = "7C5716553E9B132EF325C49CA2079737196C03DB";
        private const string DEFAULT_AUD = "cbe67fb4-999d-43bc-833d-8dee6d1a89a4";
        private const string DEFAULT_JWT_EXP_IN_SECONDS= "300";

        [Theory]
        [InlineData("Banking_PS256_NoJarmEnc", DH_BRANDID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "PS256", null, null)]
        [InlineData("Banking_ES256_NoJarmEnc", DH_BRANDID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "ES256", null, null)]
        [InlineData("Banking_PS256_Jarm_Enc_RSA-OAEP_A256GCM", DH_BRANDID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "PS256", "RSA-OAEP", "A256GCM")]
        [InlineData("Banking_ES256_Jarm_Enc_RSA-OAEP_A128CBC-HS256", DH_BRANDID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "ES256", "RSA-OAEP", "A128CBC-HS256")]
        [InlineData("Banking_PS256_Jarm_Enc_RSA-OAEP-256_A256GCM", DH_BRANDID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "PS256", "RSA-OAEP-256", "A256GCM")]
        [InlineData("Banking_ES256_Jarm_Enc_RSA-OAEP-256_A128CBC-HS256", DH_BRANDID, CUSTOMERID_BANKING, CUSTOMERACCOUNTS_BANKING, "ES256", "RSA-OAEP-256", "A128CBC-HS256")]
        //ENERGY
        [InlineData("Energy_PS256_NoJarmEnc", DH_BRANDID_ENERGY, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY, "PS256", null, null)]
        [InlineData("Energy_ES256_NoJarmEnc", DH_BRANDID_ENERGY, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY, "ES256", null, null)]
        [InlineData("Energy_PS256_Jarm_Enc_RSA-OAEP_A256GCM", DH_BRANDID_ENERGY, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY, "PS256", "RSA-OAEP", "A256GCM")]
        [InlineData("Energy_ES256_Jarm_Enc_RSA-OAEP-256_A128CBC-HS256", DH_BRANDID_ENERGY, CUSTOMERID_ENERGY, CUSTOMERACCOUNTS_ENERGY, "ES256", "RSA-OAEP-256", "A128CBC-HS256")]

        public async Task AC01_AC03_AC04_AC05_AC19_Valid_Authorisation_Code_Par(string testSuffix, string dhBrandId, string customerId, string customerAccounts, string jarmSigningAlgo, string? jarmEncryptAlg, string? jarmEncryptEnc)
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC01_AC03_AC04_AC05_AC19_Valid_Authorisation_Code_Par)} - {testSuffix}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, jarmSigningAlgo: jarmSigningAlgo, jarmEncrypAlg: jarmEncryptAlg, jarmEncryptEnc: jarmEncryptEnc, responseTypes: "code")
                        ?? throw new NullReferenceException(nameof(dhClientId));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, dhBrandId, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
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
        //BANKING without JARM Enc
        [InlineData("Banking_PS256_NoJarmEnc", DH_BRANDID, CUSTOMERID_BANKING, "PS256", null, null)]
        //ENERGY with JARM Enc
        [InlineData("Energy_PS256_Jarm_Enc_RSA-OAEP_A256GCM", DH_BRANDID_ENERGY, CUSTOMERACCOUNTS_ENERGY, "PS256", "RSA-OAEP", "A256GCM")]
        public async Task AC20_Cancel_Par(string testSuffix, string dhBrandId, string customerId, string jarmSigningAlgo, string? jarmEncryptAlg, string? jarmEncryptEnc)
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC20_Cancel_Par)} - {testSuffix}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await ClientRegistration_Create(page, dhBrandId, jarmSigningAlgo: jarmSigningAlgo, jarmEncrypAlg: jarmEncryptAlg, jarmEncryptEnc: jarmEncryptEnc, responseTypes: "code")
                        ?? throw new NullReferenceException(nameof(dhClientId));
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, dhBrandId, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
                    await parPage.ClickInitiatePar();
                    await parPage.ClickAuthorizeUrl();

                    ConsentAndAuthorisationPages consentAndAuthorisationPages = new ConsentAndAuthorisationPages(page);

                    await consentAndAuthorisationPages.EnterCustomerId(customerId);
                    await consentAndAuthorisationPages.ClickContinue();

                    await consentAndAuthorisationPages.ClickCancel();

                    // Act/Assert - verify error message
                    string expectedError = "access_denied (): ERR-AUTH-009: User cancelled the authorisation flow";

                    bool errorMessageExists = await parPage.ErrorExists(expectedError);
                    errorMessageExists.Should().Be(true, $"{expectedError} should be use has cancelled the authorisation flow.");
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


        //NOTE: This test needs a review
        [Fact]
        public async Task AC09_Missing_Auhtorisation_Code()
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC09_Missing_Auhtorisation_Code)}";
                string? dhClientId = null;

                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await CreateBankingRegistration(page, "code");
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, DH_BRANDID, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
                    await parPage.ClickInitiatePar();

                    string requestUri = await parPage.GetRequestUri();
                    string dataHolderAuthState = GetDataholderAuthenticationState(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING, requestUri);

                    string callbackUrl = $"{WEB_URL}/consent/callback?response={GetAuthenticationCodeJwt(state: dataHolderAuthState, authCode: "")}";
                    await page.GotoAsync(callbackUrl);

                    // Act/Assert - verify error message
                    string expectedError = "Token Validation Failed (IDX10214)";
                    bool errorMessageExists = await parPage.ErrorExists(expectedError);
                    errorMessageExists.Should().Be(true, $"'{expectedError} should be displayed when 'code' is missing.");
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
        public async Task AC09_Malformed_Authorisation_Code_Jwt()
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC09_Malformed_Authorisation_Code_Jwt)}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await CreateBankingRegistration(page, "code");
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, DH_BRANDID, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
                    await parPage.ClickInitiatePar();

                    string requestUri = await parPage.GetRequestUri();

                    string dataHolderAuthState = GetDataholderAuthenticationState(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING, requestUri);

                    //add foo to JWT to make it malformed
                    string callbackUrl = $"{WEB_URL}/consent/callback?response=foo{GetAuthenticationCodeJwt(state: dataHolderAuthState)}";
                    await page.GotoAsync(callbackUrl);

                    // Act/Assert - verify error message
                    string expectedError = "Token Validation Error (IDX12729)";
                    bool errorMessageExists = await parPage.ErrorExists(expectedError);
                    errorMessageExists.Should().Be(true, $"{expectedError} should be displayed when JWT is malformed.");
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
        [InlineData("Incorrect_State", "foo", "Missing Required Field (): authState is missing", "'AuthState' is missing because the wrong state has been used in simulated callback.")]
        [InlineData("Missing_State", "", "Missing Required Field (): state is missing", "'state' is blank in simulated callbak.")]
        public async Task AC11_Invalid_State_In_Authorisation_Code_Jwt(string testSuffix, string jwtState, string expectedError, string becauseText)
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC11_Invalid_State_In_Authorisation_Code_Jwt)} - {testSuffix}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId =await CreateBankingRegistration(page);
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, DH_BRANDID, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
                    await parPage.ClickInitiatePar();

                    string callbackUrl = $"{WEB_URL}/consent/callback?response={GetAuthenticationCodeJwt(state: jwtState)}";
                    await page.GotoAsync(callbackUrl);

                    // Act/Assert - verify error message
                    bool errorMessageExists = await parPage.ErrorExists(expectedError);
                    errorMessageExists.Should().Be(true, $"{expectedError} should be displayed when {becauseText}");

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
        [InlineData("Iss_Missing",      null,               "USE_VALID_AUD",    DEFAULT_JWT_EXP_IN_SECONDS, "Token Validation Failed (IDX10211):  Unable to validate issuer. The 'issuer' parameter is null or whitespace", "'iss' is blank in simulated callback.")]
        [InlineData("Iss_Mismatch",     "foo",              "USE_VALID_AUD",    DEFAULT_JWT_EXP_IN_SECONDS, "Token Validation Failed (IDX10205):  Issuer validation failed. Issuer: 'foo'.", "'iss' is different to the expected client Id in simulated callback.")]
        [InlineData("Aud_Missing",      "USE_VALID_ISS",    null,               DEFAULT_JWT_EXP_IN_SECONDS, "Token Validation Failed (IDX10206):  Unable to validate audience. The 'audiences' parameter is empty.", "'aud' is blank in simulated callback.")]
        [InlineData("Aud_Mismatch",     "USE_VALID_ISS",    "foo",              DEFAULT_JWT_EXP_IN_SECONDS, "Token Validation Failed (IDX10214):  Audience validation failed. Audiences: 'foo'.", "'aud' is different to the expected uri in simulated callback..")]
        [InlineData("missing_Exp",      "USE_VALID_ISS",    "USE_VALID_AUD",    null,                       "Token Validation Failed (IDX10225):  Lifetime validation failed. The token is missing an Expiration Time.", "'exp' is blank in simulated callback.")]
        [InlineData("Expired_Token",    "USE_VALID_ISS",    "USE_VALID_AUD",    "-500",                     "Token Validation Failed (IDX10223):  Lifetime validation failed. The token is expired.", "JWT has expired. (now - 500 seconds)")]
        public async Task AC12_AC13_AC14_AC15_Jwt_Missing_And_Mismatch_Metadata(string testSuffix, string? issuer, string? audience, string? jwtExpiry, string expectedError, string becauseText)
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC12_AC13_AC14_AC15_Jwt_Missing_And_Mismatch_Metadata)} - {testSuffix}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await CreateBankingRegistration(page, "code");
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, DH_BRANDID, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
                    await parPage.ClickInitiatePar();

                    string requestUri = await parPage.GetRequestUri();
                    string dataHolderAuthState = GetDataholderAuthenticationState(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING, requestUri);

                    if (audience != null)
                    {
                        audience = audience.Replace("USE_VALID_AUD", GetDataholderClientId(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING, requestUri));
                    }
                    if(issuer != null)
                    {
                        issuer = issuer.Replace("USE_VALID_ISS", GetInfosecBaseUriFromRegister(DH_BRANDID));
                    }                  

                    string callbackUrl = $"{WEB_URL}/consent/callback?response={GetAuthenticationCodeJwt(state: dataHolderAuthState,issuer:issuer, aud:audience, expiryTimeInSeconds: jwtExpiry )}";
                    await page.GotoAsync(callbackUrl);

                    // Act/Assert - verify error message
                    bool errorMessageExists = await parPage.ErrorExists(expectedError);
                    errorMessageExists.Should().Be(true, $"{expectedError} should be displayed when {becauseText}: Callback URL Used: {callbackUrl}");
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
        public async Task AC18_Response_JWT_Signature_Fail()
        {
            try
            {
                string testName = $"{nameof(US46352_MDR_E2ETests_AuthCodeFlowJarm)} - {nameof(AC18_Response_JWT_Signature_Fail)}";
                string? dhClientId = null;
               
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await CreateBankingRegistration(page, "code");
                });

                await TestAsync(testName, async (page) =>
                {
                    // Arrange - Goto home page, click menu button, check page loaded
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, DH_BRANDID, sharingDuration: SHARING_DURATION, responseType: "code", responseMode: "jwt");
                    await parPage.ClickInitiatePar();

                    string requestUri = await parPage.GetRequestUri();
                    string dataHolderAuthState = GetDataholderAuthenticationState(DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING, requestUri);

                    string callbackUrl = $"{WEB_URL}/consent/callback?response={GetAuthenticationCodeJwt(state: dataHolderAuthState, certificateFileName: WRONG_CERTIFICATE_FILENAME, certificatePassword: WRONG_CERTIFICATE_PASSWORD)}";
                    await page.GotoAsync(callbackUrl);

                    // Act/Assert - verify error message
                    string expectedError = "Token Validation Failed (IDX10511)";
                    bool errorMessageExists = await parPage.ErrorExists(expectedError);
                    errorMessageExists.Should().Be(true, $"'{expectedError} should be displayed when JWT was signed with a different certificate.");
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

        private static async Task<string> CreateBankingRegistration(IPage page, string responseType = "code id_token")
        {
            return await ClientRegistration_Create(page, DH_BRANDID, responseTypes: responseType);
        }

        private static string GetAuthenticationCodeJwt(string state = "",
            string certificateFileName = IntegrationTests.BaseTest.DATAHOLDER_CERTIFICATE_FILENAME,
            string certificatePassword = IntegrationTests.BaseTest.DATAHOLDER_CERTIFICATE_PASSWORD,
            string? issuer = IntegrationTests.BaseTest.DATAHOLDER_BRAND,
            string? aud = DEFAULT_AUD,
            string? authCode = null,
            string? expiryTimeInSeconds = DEFAULT_JWT_EXP_IN_SECONDS)
        {
            authCode = authCode ?? Guid.NewGuid().ToString();

            return new AuthenticationCodeJwt
            {
                CertificateFilename = certificateFileName,
                CertificatePassword = certificatePassword,
                ExpiryTimeInSeconds = expiryTimeInSeconds,
                Iss = issuer,
                Aud = aud,
                Kid = DEFAULT_KID,  
                State = state,
                Code = authCode,
            }.Get();
        }

        private static string GetDataholderClientId(string connectionString, string urnToLookUp)
        {
            JObject? grantJson = GetDataholderGrantFromDb(connectionString, urnToLookUp);

            string clientId = (string?)grantJson["client_id"] ?? throw new ArgumentNullException($"client_id not found in request JSON {grantJson}.");

            return clientId;
        }

        private static string GetDataholderAuthenticationState(string connectionString, string urnToLookUp)
        {
            JObject? grantJson = GetDataholderGrantFromDb(connectionString, urnToLookUp);

            string state = (string?)grantJson["state"] ?? throw new ArgumentNullException($"State not found in request JSON {grantJson}.");

            return state;
        }

        private static JObject GetDataholderGrantFromDb(string connectionString, string urnToLookUp)
        {
            using var dhAuthServerConnection = new SqlConnection(connectionString);
            dhAuthServerConnection.Open();

            using var selectCommand = new SqlCommand($"SELECT Data FROM GRANTS WHERE \"Key\" = '{urnToLookUp}'", dhAuthServerConnection);
            string? data = Convert.ToString(selectCommand.ExecuteScalar());

            if (String.IsNullOrEmpty(data))
                throw new Exception($"Could not find urn '{urnToLookUp}' in Authentication Server database Grants table.");

            JObject dataJson = JObject.Parse(data);

            // CT: needs a different type of exception
            string? request = (string?)dataJson["request"] ?? throw new ArgumentNullException($"Request Json not found in 'data' from 'Grants' table.");

            return JObject.Parse(request);
        }

        private static string GetInfosecBaseUriFromRegister(string brandId)
        {

            using var registerDbServerConnection = new SqlConnection(IntegrationTests.BaseTest.REGISTER_CONNECTIONSTRING);
            registerDbServerConnection.Open();

            using var selectCommand = new SqlCommand($"SELECT InfosecBaseUri FROM Endpoint WHERE BrandId = '{brandId}'", registerDbServerConnection);
            string ? infosecBaseUri = Convert.ToString(selectCommand.ExecuteScalar());

            if (String.IsNullOrEmpty(infosecBaseUri))
                throw new Exception($"Could not find InfosecBaseUri for '{brandId}' in Register Endpoint Table.");

            return infosecBaseUri;
        }
    }
}
