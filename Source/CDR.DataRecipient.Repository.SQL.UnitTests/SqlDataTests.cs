using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CDR.DataRecipient.Repository.SQL.UnitTests
{
    //[Collection("UnitTests")]
    public class SqlDataTests
    {
        IServiceProvider _serviceProvider;

        public SqlDataTests()
        {
            var sqlLiteFixture = new SqlDataFixture();
            _serviceProvider = sqlLiteFixture.ServiceProvider;            
        }

        #region Data holder Repository Unit Tests
        [Fact]
        public async Task GetDataHolderBrand_Success()
        {
            //Arrange
            var dataHoldersRepository = _serviceProvider.GetRequiredService<IDataHoldersRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var data = await dataHoldersRepository.GetDataHolderBrand("cf217aba-e00d-48d5-9c3d-03af0b91cb80");

            //Assert            
            Assert.NotNull(data);
            Assert.Equal("cf217aba-e00d-48d5-9c3d-03af0b91cb80", data.DataHolderBrandId);
        }

        [Fact]
        public async Task GetDataHolderBrands_Success()
        {
            //Arrange
            var dataHoldersRepository = _serviceProvider.GetRequiredService<IDataHoldersRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var dataHolders = await dataHoldersRepository.GetDataHolderBrands();

            //Assert            
            Assert.NotNull(dataHolders);
            Assert.Equal(2, dataHolders.Count());
        }

        [Fact]
        public async Task PersistDataHolderBrands_Success()
        {
            //Arrange
            var dataHoldersRepository = _serviceProvider.GetRequiredService<IDataHoldersRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var dataHolders = await dataHoldersRepository.GetDataHolderBrands();
            await dataHoldersRepository.PersistDataHolderBrands(dataHolders);

            //Assert
            Assert.True(true);
        }
        #endregion

        #region Consent Agrrangement UnitTests

        [Fact]
        public async Task GetConsentArrangement_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var data = await consentRepository.GetConsentByArrangement("92d260c1-a625-41e2-a777-c0af1912a74a");

            //Assert            
            Assert.NotNull(data);            
            Assert.Equal("92d260c1-a625-41e2-a777-c0af1912a74a", data.CdrArrangementId);
            Assert.Equal("804fc2fb-18a7-4235-9a49-2af393d18bc7", data.DataHolderBrandId);            
        }

        [Fact]
        public async Task GetConsentArrangements_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var data = await consentRepository.GetConsents("","","mdr-user");

            //Assert            
            Assert.NotNull(data);
            Assert.Equal(2, data.Count());
        }

        [Fact]
        public async Task PersistConsentArrangementsInsert_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            await sqliteDataAccess.DeleteCdrArrangementData();

            //string cdrArranamgentId = "92d260c1-a625-41e2-a777-c0af1912a74a";
            string jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74a"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}";
            var data = JsonConvert.DeserializeObject<ConsentArrangement>(jsonDocument, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            
            //Act                                    
            await consentRepository.PersistConsent(data);
            var afterPersistdata = await consentRepository.GetConsentByArrangement(data.CdrArrangementId);

            //Assert            
            Assert.NotNull(data);
            Assert.Equal(afterPersistdata.CdrArrangementId, data.CdrArrangementId);
            Assert.Equal(afterPersistdata.DataHolderBrandId, data.DataHolderBrandId);
            Assert.Equal(afterPersistdata.ExpiresIn, data.ExpiresIn);
            Assert.Equal(afterPersistdata.CreatedOn, data.CreatedOn);
        }

        [Fact]
        public async Task PersistConsentArrangementsUpdate_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act            
            var data = await consentRepository.GetConsentByArrangement("92d260c1-a625-41e2-a777-c0af1912a74a");             
            data.DataHolderBrandId = "804fc2fb-18a7-4235-9a49-2af393d18bc9";
            await consentRepository.PersistConsent(data);
            var afterPersistdata = await consentRepository.GetConsentByArrangement(data.CdrArrangementId);

            //Assert            
            Assert.NotNull(data);
            Assert.Equal(afterPersistdata.CdrArrangementId, data.CdrArrangementId);
            Assert.Equal(afterPersistdata.DataHolderBrandId, data.DataHolderBrandId);
            Assert.Equal(afterPersistdata.ExpiresIn, data.ExpiresIn);
            Assert.Equal(afterPersistdata.CreatedOn, data.CreatedOn);
        }

        [Fact]
        public async Task ConsentArrangementsUpdateTokens_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();
            var cdrArrangementId = "92d260c1-a625-41e2-a777-c0af1912a74a";

            //Act            
            await consentRepository.UpdateTokens(cdrArrangementId, "fooIdToken", "fooAccessToken", "fooRefreshToken");
            var data = await consentRepository.GetConsentByArrangement(cdrArrangementId);

            //Assert            
            Assert.NotNull(data);
            Assert.Equal(cdrArrangementId , data.CdrArrangementId);
            Assert.Equal("fooIdToken", data.IdToken);
            Assert.Equal("fooAccessToken", data.AccessToken);
            Assert.Equal("fooRefreshToken", data.RefreshToken);
        }

        [Fact]
        public async Task ConsentArrangementsDeleteConsent_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();
            var cdrArrangementId = "92d260c1-a625-41e2-a777-c0af1912a74a";

            //Act                        
            await consentRepository.DeleteConsent(cdrArrangementId);
            var data = await consentRepository.GetConsentByArrangement(cdrArrangementId);

            //Assert            
            Assert.Null(data);
        }

        [Fact]
        public async Task ConsentArrangementsRevokeConsent_Success()
        {
            //Arrange
            var consentRepository = _serviceProvider.GetRequiredService<IConsentsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act            
            var data = await consentRepository.GetConsentByArrangement("92d260c1-a625-41e2-a777-c0af1912a74a");
            var result = await consentRepository.RevokeConsent(data.CdrArrangementId, data.DataHolderBrandId);
            var afterRevokedata = await consentRepository.GetConsentByArrangement(data.CdrArrangementId);

            //Assert            
            Assert.True(result);
            Assert.Null(afterRevokedata);
        }
        #endregion

        #region CDR Registrations 
        [Fact]
        public async Task GetCDRRegistration_Success()
        {
            //Arrange
            var registrationRepository = _serviceProvider.GetRequiredService<IRegistrationsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var data = await registrationRepository.GetRegistration("bad06794-39e2-400c-9e1b-f15a0bb67f46", "804fc2fb-18a7-4235-9a49-2af393d18bc7");

            //Assert            
            Assert.NotNull(data);            
            Assert.Equal("bad06794-39e2-400c-9e1b-f15a0bb67f46", data.ClientId);
            Assert.Equal("A product to help you manage your budget", data.ClientDescription);
            Assert.Equal("https://mocksoftware/mybudgetapp", data.ClientUri);
            Assert.Equal("804fc2fb-18a7-4235-9a49-2af393d18bc7", data.DataHolderBrandId);
            Assert.Equal(1631835434, data.ClientIdIssuedAt);
        }

        [Fact]
        public async Task GetCDRRegistrations_Success()
        {
            //Arrange
            var registrationRepository = _serviceProvider.GetRequiredService<IRegistrationsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act
            var data = await registrationRepository.GetRegistrations();

            //Assert            
            Assert.NotNull(data);
            Assert.Single(data);
        }

        [Fact]
        public async Task CDRRegistrationDeleteRegistration_Success()
        {
            //Arrange
            var registrationRepository = _serviceProvider.GetRequiredService<IRegistrationsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act                        
            var ClientId = "bad06794-39e2-400c-9e1b-f15a0bb67f46";
            var DataHolderBrandId = "804fc2fb-18a7-4235-9a49-2af393d18bc7";
            await registrationRepository.DeleteRegistration(ClientId, DataHolderBrandId);
            
            //No data should be returned
            var afterDeletion = await registrationRepository.GetRegistration("bad06794-39e2-400c-9e1b-f15a0bb67f46", "804fc2fb-18a7-4235-9a49-2af393d18bc7");

            //Assert            
            Assert.Null(afterDeletion);
        }

        [Fact]
        public async Task PersistRegistrationInsert_Success()
        {
            //Arrange
            var registrationRepository = _serviceProvider.GetRequiredService<IRegistrationsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            await sqliteDataAccess.DeleteRegistrationData();
                        
            string jsonDocument = @"{""_id"":{""$oid"":""6143d52c4433e41a861ea58d""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""ClientIdIssuedAt"":1631835434,""ClientDescription"":""A product to help you manage your budget"",""ClientUri"":""https://mocksoftware/mybudgetapp"",""OrgId"":""ffb1c8ba-279e-44d8-96f0-1bc34a6b436f"",""OrgName"":""Mock Finance Tools"",""RedirectUris"":[""https://localhost:9001/consent/callback""],""LogoUri"":""https://mocksoftware/mybudgetapp/img/logo.png"",""TosUri"":""https://mocksoftware/mybudgetapp/terms"",""PolicyUri"":""https://mocksoftware/mybudgetapp/policy"",""JwksUri"":""https://localhost:9001/jwks"",""RevocationUri"":""https://localhost:9001/revocation"",""RecipientBaseUri"":""https://localhost:9001"",""TokenEndpointAuthSigningAlg"":""PS256"",""TokenEndpointAuthMethod"":""private_key_jwt"",""GrantTypes"":[""client_credentials"",""authorization_code"",""refresh_token""],""ResponseTypes"":[""code id_token""],""ApplicationType"":""web"",""IdTokenSignedResponseAlg"":""PS256"",""IdTokenEncryptedResponseAlg"":""RSA-OAEP"",""IdTokenEncryptedResponseEnc"":""A256GCM"",""RequestObjectSigningAlg"":""PS256"",""SoftwareStatement"":""eyJhbGciOiJQUzI1NiIsImtpZCI6IjU0MkE5QjkxNjAwNDg4MDg4Q0Q0RDgxNjkxNkE5RjQ0ODhERDI2NTEiLCJ0eXAiOiJKV1QifQ.ew0KICAicmVjaXBpZW50X2Jhc2VfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEiLA0KICAibGVnYWxfZW50aXR5X2lkIjogIjE4Yjc1YTc2LTU4MjEtNGM5ZS1iNDY1LTQ3MDkyOTFjZjBmNCIsDQogICJsZWdhbF9lbnRpdHlfbmFtZSI6ICJNb2NrIFNvZnR3YXJlIENvbXBhbnkiLA0KICAiaXNzIjogImNkci1yZWdpc3RlciIsDQogICJpYXQiOiAxNjMxODM1NDE3LA0KICAiZXhwIjogMTYzMTgzNjAxNywNCiAgImp0aSI6ICJjNzYzYjU4NzJkNGY0MzIwOWE3NmUzOTU3YTAzMDgwNCIsDQogICJvcmdfaWQiOiAiZmZiMWM4YmEtMjc5ZS00NGQ4LTk2ZjAtMWJjMzRhNmI0MzZmIiwNCiAgIm9yZ19uYW1lIjogIk1vY2sgRmluYW5jZSBUb29scyIsDQogICJjbGllbnRfbmFtZSI6ICJNeUJ1ZGdldEhlbHBlciIsDQogICJjbGllbnRfZGVzY3JpcHRpb24iOiAiQSBwcm9kdWN0IHRvIGhlbHAgeW91IG1hbmFnZSB5b3VyIGJ1ZGdldCIsDQogICJjbGllbnRfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwIiwNCiAgInJlZGlyZWN0X3VyaXMiOiBbDQogICAgImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvY29uc2VudC9jYWxsYmFjayINCiAgXSwNCiAgImxvZ29fdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL2ltZy9sb2dvLnBuZyIsDQogICJ0b3NfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL3Rlcm1zIiwNCiAgInBvbGljeV91cmkiOiAiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvcG9saWN5IiwNCiAgImp3a3NfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvandrcyIsDQogICJyZXZvY2F0aW9uX3VyaSI6ICJodHRwczovL2xvY2FsaG9zdDo5MDAxL3Jldm9jYXRpb24iLA0KICAic29mdHdhcmVfaWQiOiAiYzYzMjdmODctNjg3YS00MzY5LTk5YTQtZWFhY2QzYmI4MjEwIiwNCiAgInNvZnR3YXJlX3JvbGVzIjogImRhdGEtcmVjaXBpZW50LXNvZnR3YXJlLXByb2R1Y3QiLA0KICAic2NvcGUiOiAib3BlbmlkIHByb2ZpbGUgYmFuazphY2NvdW50cy5iYXNpYzpyZWFkIGJhbms6YWNjb3VudHMuZGV0YWlsOnJlYWQgYmFuazp0cmFuc2FjdGlvbnM6cmVhZCBiYW5rOnBheWVlczpyZWFkIGJhbms6cmVndWxhcl9wYXltZW50czpyZWFkIGNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIGNvbW1vbjpjdXN0b21lci5kZXRhaWw6cmVhZCBjZHI6cmVnaXN0cmF0aW9uIg0KfQ.j_UwVV2g28047YN12KdsGxE3pQwXVkF_ZSCwq7_HLdrlnQKZHsReQCprtxk-MV9vH0EGwpMw46WFQV5pTB-mxwZZfhkQx0-U30ufJfmPwvpxxAI90gFl3MFtQbwgC5a8IkkVfjSUoK1-m-pgG3X79rf0zUB9aRZoSigXgVemKfnQeiB-Gx_TI3zi0QkF1Uw052dAATQvUvaZ040oyqWuTFKETG7AzTV6M1ZcxVJYX5gGhemFIoWA0bVqrP3-dEMUOLFhhFwe3otMMB7iaBfOjBmQ9xtlnnmxFGIGvHErBiHouwfGzG0jCqI5dwtKkicjNKoa4uq-ul3EGup8FWY4Vw"",""SoftwareId"":""c6327f87-687a-4369-99a4-eaacd3bb8210"",""Scope"":""openid profile bank:accounts.basic:read bank:transactions:read common:customer.basic:read cdr:registration""}";
            
            var registration = System.Text.Json.JsonSerializer.Deserialize<Registration>(jsonDocument);

            //Act                                    
            await registrationRepository.PersistRegistration(registration);

            //data should be returned
            var afterInsert = await registrationRepository.GetRegistration(registration.ClientId, registration.DataHolderBrandId);

            //Assert            
            Assert.NotNull(afterInsert);
            Assert.Equal("bad06794-39e2-400c-9e1b-f15a0bb67f46", afterInsert.ClientId);
            Assert.Equal("A product to help you manage your budget", afterInsert.ClientDescription);
            Assert.Equal("https://mocksoftware/mybudgetapp", afterInsert.ClientUri);
            Assert.Equal("804fc2fb-18a7-4235-9a49-2af393d18bc7", afterInsert.DataHolderBrandId);
            Assert.Equal(1631835434, afterInsert.ClientIdIssuedAt);
        }

        [Fact]
        public async Task RegistrationUpdate_Success()
        {
            //Arrange
            var registrationRepository = _serviceProvider.GetRequiredService<IRegistrationsRepository>();
            var sqliteDataAccess = _serviceProvider.GetRequiredService<ISqlDataAccess>();
            sqliteDataAccess.RecreateDatabaseWithForTests();

            //Act             
            var ClientId = "bad06794-39e2-400c-9e1b-f15a0bb67f46";
            var DataHolderBrandId = "804fc2fb-18a7-4235-9a49-2af393d18bc7";
            var data = await registrationRepository.GetRegistration(ClientId, DataHolderBrandId);
            data.ClientUri = "https://mocksoftware/mybudgetapp2";
            await registrationRepository.UpdateRegistration(data);
            var afterUpdate = await registrationRepository.GetRegistration(ClientId, DataHolderBrandId);

            //Assert            
            Assert.NotNull(afterUpdate);
            Assert.Equal("bad06794-39e2-400c-9e1b-f15a0bb67f46", afterUpdate.ClientId);
            Assert.Equal("A product to help you manage your budget", afterUpdate.ClientDescription);
            Assert.Equal("https://mocksoftware/mybudgetapp2", afterUpdate.ClientUri);
            Assert.Equal("804fc2fb-18a7-4235-9a49-2af393d18bc7", afterUpdate.DataHolderBrandId);
            Assert.Equal(1631835434, afterUpdate.ClientIdIssuedAt);
        }

        #endregion 
    }
}
