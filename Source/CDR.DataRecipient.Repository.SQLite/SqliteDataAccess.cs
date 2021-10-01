using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository.SQLite.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CDR.DataRecipient.Repository.SQLite
{
    public class SqliteDataAccess : ISqliteDataAccess
    {
        public IConfiguration Configuration { get; }
        public string DatabaseConnectionString { get; set; }
        public SqliteDataAccess(IConfiguration configuration)
        {
            Configuration = configuration;
            DatabaseConnectionString = Configuration.GetConnectionString("DefaultConnection");

            SqliteCreateDatabase();
        }

        #region CdrArragements
        public async Task<ConsentArrangement> GetConsent(string cdrArrangementId)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                using var selectCommand = new SqliteCommand($"select JsonDocument from CdrArrangement where CdrArrangementId = @id", db);
                selectCommand.Parameters.AddWithValue("@id", cdrArrangementId);

                var res = selectCommand.ExecuteScalar();
                if (!string.IsNullOrEmpty(Convert.ToString(res))) 
                {
                    var jsonDocument = Convert.ToString(res);

                    //var consentArrangement1 = System.Text.Json.JsonSerializer.Deserialize<ConsentArrangement>(jsonDocument);
                    var consentArrangement = JsonConvert.DeserializeObject<ConsentArrangement>(jsonDocument, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    
                    db.Close();

                    return consentArrangement;
                }
            }
            return null;
        }

        public async Task<IEnumerable<ConsentArrangement>> GetConsents()
        {
            List<ConsentArrangement> cdrArrangements = new List<ConsentArrangement>();
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                using var commandText = new SqliteCommand($"select CdrArrangementId, JsonDocument from CdrArrangement", db);
                SqliteDataReader reader = commandText.ExecuteReader();
                while (reader.Read())
                {
                    ConsentArrangement cdrArrangement = new ConsentArrangement();

                    var CdrArrangementId = reader.GetString(0);
                    var JsonDocument = reader.GetString(1);

                    var jsonDocument = Convert.ToString(JsonDocument);
                    cdrArrangement = JsonConvert.DeserializeObject<ConsentArrangement>(jsonDocument, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    cdrArrangements.Add(cdrArrangement);
                }

                db.Close();
                return cdrArrangements;
            }
        }

        public async Task InsertCdrArrangement(ConsentArrangement consentArrangement) 
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                var jsonDocument = JsonConvert.SerializeObject(consentArrangement);
                var commandText = $"INSERT INTO CdrArrangement(CdrArrangementId, JsonDocument) VALUES('{consentArrangement.CdrArrangementId}','{jsonDocument}')";

                //special case for CdrArrangements
                commandText = commandText.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                commandText = commandText.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");

                using var command = new SqliteCommand(commandText, db);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        //unit test purposes 
        public async Task DeleteCdrArrangementData()
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();                
                var commandText = $"Delete from CdrArrangement";                
                using var command = new SqliteCommand(commandText, db);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        public async Task DeleteRegistrationData()
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();
                var commandText = $"Delete from Registration";
                using var command = new SqliteCommand(commandText, db);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        public async Task UpdateCdrArrangement(ConsentArrangement consentArrangement)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                var jsonDocument = JsonConvert.SerializeObject(consentArrangement, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                var commandText = $"UPDATE CdrArrangement SET JsonDocument='{jsonDocument}' where CdrArrangementId=@id";

                //special case for CdrArrangements
                commandText = commandText.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                commandText = commandText.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");

                using var command = new SqliteCommand(commandText, db);
                command.Parameters.AddWithValue("@id", consentArrangement.CdrArrangementId);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();
                var commandText = $"Delete from CdrArrangement where CdrArrangementId=@id";

                using var command = new SqliteCommand(commandText, db);
                command.Parameters.AddWithValue("@id", cdrArrangementId);                
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        #endregion


        #region CrdRegistrations 

        public async Task<Registration> GetRegistration(string clientId)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                using var selectCommand = new SqliteCommand($"select JsonDocument from Registration where ClientId = @id", db);
                selectCommand.Parameters.AddWithValue("@id", clientId);

                var res = selectCommand.ExecuteScalar();

                if (!string.IsNullOrEmpty(Convert.ToString(res)))
                {                    
                    var jsonDocument = Convert.ToString(res);
                    var registration = System.Text.Json.JsonSerializer.Deserialize<Registration>(jsonDocument);
                    db.Close();

                    return registration;
                }
            }
            return null;
        }

        public async Task<IEnumerable<Registration>> GetRegistrations()
        {
            List<Registration> registrations = new List<Registration>();
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                using var commandText = new SqliteCommand($"select ClientId, JsonDocument from Registration", db);
                SqliteDataReader reader = commandText.ExecuteReader();
                while (reader.Read())
                {
                    Registration registration = new Registration();

                    var CdrArrangementId = reader.GetString(0);
                    var JsonDocument = reader.GetString(1);

                    var jsonDocument = Convert.ToString(JsonDocument);                    
                    registration = System.Text.Json.JsonSerializer.Deserialize<Registration>(jsonDocument);
                    registrations.Add(registration);
                }

                db.Close();
                return registrations;
            }
        }

        public async Task DeleteRegistration(string clientId)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();
                var commandText = $"Delete from Registration where ClientId=@id";

                using var command = new SqliteCommand(commandText, db);
                command.Parameters.AddWithValue("@id", clientId);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        public async Task InsertRegistration(Registration registration)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(registration);
                var commandText = $"INSERT INTO Registration(ClientId, JsonDocument) VALUES('{registration.ClientId}','{jsonDocument}')";
                
                using var command = new SqliteCommand(commandText, db);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        public async Task UpdateRegistration(Registration registration)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();
                
                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(registration);
                var commandText = $"UPDATE Registration SET JsonDocument='{jsonDocument}' where ClientId=@id";

                //special cases
                //commandText = commandText.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                //commandText = commandText.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");

                using var command = new SqliteCommand(commandText, db);
                command.Parameters.AddWithValue("@id", registration.ClientId);
                command.ExecuteNonQuery();

                db.Close();
            }
        }

        #endregion 
        public bool SqliteCreateDatabase()
        {            
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();
                                
                var commandText = @"CREATE TABLE IF NOT EXISTS CdrArrangement (CdrArrangementId TEXT PRIMARY KEY NOT NULL, JsonDocument TEXT);
                                              CREATE TABLE IF NOT EXISTS DataHolderBrand (DataHolderBrandId TEXT PRIMARY KEY NOT NULL, JsonDocument TEXT);
                                              CREATE TABLE IF NOT EXISTS Registration (ClientId TEXT PRIMARY KEY NOT NULL, JsonDocument TEXT);";
                SqliteCommand sqliteCommand = new SqliteCommand(commandText, db);
                sqliteCommand.ExecuteNonQuery();
                
                db.Close();
                return true;
            }
        }
        

        //For Unit Testing only
        public bool RecreateDatabaseWithForTests()
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                //Purging all exsiting data
                string sqliteCommandText = @"DROP TABLE IF EXISTS CdrArrangement; 
                                             DROP TABLE IF EXISTS DataHolderBrand; 
                                             DROP TABLE IF EXISTS Registration;";

                SqliteCommand sqliteCommand = new SqliteCommand(sqliteCommandText, db);
                sqliteCommand.ExecuteNonQuery();

                //Create fresh db. 
                sqliteCommand.CommandText = @"CREATE TABLE CdrArrangement (CdrArrangementId TEXT PRIMARY KEY NOT NULL, JsonDocument TEXT);
                                              CREATE TABLE DataHolderBrand (DataHolderBrandId TEXT PRIMARY KEY NOT NULL, JsonDocument TEXT);
                                              CREATE TABLE Registration (ClientId TEXT PRIMARY KEY NOT NULL, JsonDocument TEXT);";

                sqliteCommand.ExecuteNonQuery();                
                db.Close();

                //Insert test data
                InsertTestData();

                return true;
            }
        }

        public bool InsertTestData()
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                //Insert data holders data for testing
                var brandId = "cf217aba-e00d-48d5-9c3d-03af0b91cb80";
                var jsonDocument = @"{""_id"":{""$oid"":""613ef59c1a0ee5d9fd426a80""},""DataHolderBrandId"":""cf217aba-e00d-48d5-9c3d-03af0b91cb80"",""BrandName"":""Hall Bank"",""LegalEntity"":{""LegalEntityId"":""924ca498-0f19-402d-ae07-2cb61088f8aa"",""LegalEntityName"":""Hall Bank""},""Status"":""ACTIVE"",""EndpointDetail"":{""Version"":""1"",""PublicBaseUri"":""https://publicapi.hallbank"",""ResourceBaseUri"":""https://api.hallbank"",""InfoSecBaseUri"":""https://idp.hallbank"",""ExtensionBaseUri"":"""",""WebsiteUri"":""https://hallbank/""},""AuthDetails"":[{""RegisterUType"":""SIGNED-JWT"",""JwksEndpoint"":""https://hallbank/idp/jwks""}]}"; ;

                var sqliteCommandText = $"INSERT INTO DataHolderBrand(DataHolderBrandId, JsonDocument) VALUES('{brandId}','{jsonDocument}')";
                SqliteCommand sqliteCommand = new SqliteCommand(sqliteCommandText, db);                
                sqliteCommand.ExecuteNonQuery();

                brandId = "cf217aba-e00d-48d5-9c3d-03af0b91cb81";
                jsonDocument = @"{""_id"":{""$oid"":""613ef59c1a0ee5d9fd426a81""},""DataHolderBrandId"":""cf217aba-e00d-48d5-9c3d-03af0b91cb81"",""BrandName"":""Hall Bank"",""LegalEntity"":{""LegalEntityId"":""924ca498-0f19-402d-ae07-2cb61088f8aa"",""LegalEntityName"":""Hall Bank""},""Status"":""ACTIVE"",""EndpointDetail"":{""Version"":""1"",""PublicBaseUri"":""https://publicapi.hallbank"",""ResourceBaseUri"":""https://api.hallbank"",""InfoSecBaseUri"":""https://idp.hallbank"",""ExtensionBaseUri"":"""",""WebsiteUri"":""https://hallbank/""},""AuthDetails"":[{""RegisterUType"":""SIGNED-JWT"",""JwksEndpoint"":""https://hallbank/idp/jwks""}]}"; ;
                sqliteCommand.CommandText = $"INSERT INTO DataHolderBrand(DataHolderBrandId, JsonDocument) VALUES('{brandId}','{jsonDocument}')";
                sqliteCommand.ExecuteNonQuery();

                //Insert cdr-aggranamgnets data for testing
                var cdrArranamgentId = "92d260c1-a625-41e2-a777-c0af1912a74a";
                jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":null,""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74a"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}"; 
                sqliteCommand.CommandText = $"INSERT INTO CdrArrangement(CdrArrangementId, JsonDocument) VALUES('{cdrArranamgentId}','{jsonDocument}')";
                sqliteCommand.ExecuteNonQuery();

                cdrArranamgentId = "92d260c1-a625-41e2-a777-c0af1912a74b";
                jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":null,""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74b"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}";
                sqliteCommand.CommandText = $"INSERT INTO CdrArrangement(CdrArrangementId, JsonDocument) VALUES('{cdrArranamgentId}','{jsonDocument}')";
                sqliteCommand.ExecuteNonQuery();

                //Insert cdr-registrations data for testing 
                var clientId = "bad06794-39e2-400c-9e1b-f15a0bb67f46";
                jsonDocument = @"{""_id"":{""$oid"":""6143d52c4433e41a861ea58d""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""ClientIdIssuedAt"":1631835434,""ClientDescription"":""A product to help you manage your budget"",""ClientUri"":""https://mocksoftware/mybudgetapp"",""OrgId"":""ffb1c8ba-279e-44d8-96f0-1bc34a6b436f"",""OrgName"":""Mock Finance Tools"",""RedirectUris"":[""https://localhost:9001/consent/callback""],""LogoUri"":""https://mocksoftware/mybudgetapp/img/logo.png"",""TosUri"":""https://mocksoftware/mybudgetapp/terms"",""PolicyUri"":""https://mocksoftware/mybudgetapp/policy"",""JwksUri"":""https://localhost:9001/jwks"",""RevocationUri"":""https://localhost:9001/revocation"",""RecipientBaseUri"":""https://localhost:9001"",""TokenEndpointAuthSigningAlg"":""PS256"",""TokenEndpointAuthMethod"":""private_key_jwt"",""GrantTypes"":[""client_credentials"",""authorization_code"",""refresh_token""],""ResponseTypes"":[""code id_token""],""ApplicationType"":""web"",""IdTokenSignedResponseAlg"":""PS256"",""IdTokenEncryptedResponseAlg"":""RSA-OAEP"",""IdTokenEncryptedResponseEnc"":""A256GCM"",""RequestObjectSigningAlg"":""PS256"",""SoftwareStatement"":""eyJhbGciOiJQUzI1NiIsImtpZCI6IjU0MkE5QjkxNjAwNDg4MDg4Q0Q0RDgxNjkxNkE5RjQ0ODhERDI2NTEiLCJ0eXAiOiJKV1QifQ.ew0KICAicmVjaXBpZW50X2Jhc2VfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEiLA0KICAibGVnYWxfZW50aXR5X2lkIjogIjE4Yjc1YTc2LTU4MjEtNGM5ZS1iNDY1LTQ3MDkyOTFjZjBmNCIsDQogICJsZWdhbF9lbnRpdHlfbmFtZSI6ICJNb2NrIFNvZnR3YXJlIENvbXBhbnkiLA0KICAiaXNzIjogImNkci1yZWdpc3RlciIsDQogICJpYXQiOiAxNjMxODM1NDE3LA0KICAiZXhwIjogMTYzMTgzNjAxNywNCiAgImp0aSI6ICJjNzYzYjU4NzJkNGY0MzIwOWE3NmUzOTU3YTAzMDgwNCIsDQogICJvcmdfaWQiOiAiZmZiMWM4YmEtMjc5ZS00NGQ4LTk2ZjAtMWJjMzRhNmI0MzZmIiwNCiAgIm9yZ19uYW1lIjogIk1vY2sgRmluYW5jZSBUb29scyIsDQogICJjbGllbnRfbmFtZSI6ICJNeUJ1ZGdldEhlbHBlciIsDQogICJjbGllbnRfZGVzY3JpcHRpb24iOiAiQSBwcm9kdWN0IHRvIGhlbHAgeW91IG1hbmFnZSB5b3VyIGJ1ZGdldCIsDQogICJjbGllbnRfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwIiwNCiAgInJlZGlyZWN0X3VyaXMiOiBbDQogICAgImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvY29uc2VudC9jYWxsYmFjayINCiAgXSwNCiAgImxvZ29fdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL2ltZy9sb2dvLnBuZyIsDQogICJ0b3NfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL3Rlcm1zIiwNCiAgInBvbGljeV91cmkiOiAiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvcG9saWN5IiwNCiAgImp3a3NfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvandrcyIsDQogICJyZXZvY2F0aW9uX3VyaSI6ICJodHRwczovL2xvY2FsaG9zdDo5MDAxL3Jldm9jYXRpb24iLA0KICAic29mdHdhcmVfaWQiOiAiYzYzMjdmODctNjg3YS00MzY5LTk5YTQtZWFhY2QzYmI4MjEwIiwNCiAgInNvZnR3YXJlX3JvbGVzIjogImRhdGEtcmVjaXBpZW50LXNvZnR3YXJlLXByb2R1Y3QiLA0KICAic2NvcGUiOiAib3BlbmlkIHByb2ZpbGUgYmFuazphY2NvdW50cy5iYXNpYzpyZWFkIGJhbms6YWNjb3VudHMuZGV0YWlsOnJlYWQgYmFuazp0cmFuc2FjdGlvbnM6cmVhZCBiYW5rOnBheWVlczpyZWFkIGJhbms6cmVndWxhcl9wYXltZW50czpyZWFkIGNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIGNvbW1vbjpjdXN0b21lci5kZXRhaWw6cmVhZCBjZHI6cmVnaXN0cmF0aW9uIg0KfQ.j_UwVV2g28047YN12KdsGxE3pQwXVkF_ZSCwq7_HLdrlnQKZHsReQCprtxk-MV9vH0EGwpMw46WFQV5pTB-mxwZZfhkQx0-U30ufJfmPwvpxxAI90gFl3MFtQbwgC5a8IkkVfjSUoK1-m-pgG3X79rf0zUB9aRZoSigXgVemKfnQeiB-Gx_TI3zi0QkF1Uw052dAATQvUvaZ040oyqWuTFKETG7AzTV6M1ZcxVJYX5gGhemFIoWA0bVqrP3-dEMUOLFhhFwe3otMMB7iaBfOjBmQ9xtlnnmxFGIGvHErBiHouwfGzG0jCqI5dwtKkicjNKoa4uq-ul3EGup8FWY4Vw"",""SoftwareId"":""c6327f87-687a-4369-99a4-eaacd3bb8210"",""Scope"":""openid profile bank:accounts.basic:read bank:transactions:read common:customer.basic:read cdr:registration""}";
                sqliteCommand.CommandText = $"INSERT INTO Registration(ClientId, JsonDocument) VALUES('{clientId}','{jsonDocument}')";
                sqliteCommand.ExecuteNonQuery();

                db.Close();
                return true;
            }
        }

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {            
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                using var selectCommand = new SqliteCommand($"select JsonDocument from DataHolderBrand where DataHolderBrandId = @id", db);
                selectCommand.Parameters.AddWithValue("@id", brandId);

                var res = selectCommand.ExecuteScalar();
                if (!string.IsNullOrEmpty(Convert.ToString(res)))
                {
                    var jsonDocument = Convert.ToString(res);                    
                    var dataholderbrand = System.Text.Json.JsonSerializer.Deserialize<DataHolderBrand>(jsonDocument);

                    db.Close();
                    return dataholderbrand;
                }
            }
            return null;
        }

        public async Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands()
        {
            List<DataHolderBrand> dataHolderBrands =  new List<DataHolderBrand>(); 
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();

                using var commandText = new SqliteCommand($"select DataHolderBrandId, JsonDocument from DataHolderBrand", db);
                SqliteDataReader reader = commandText.ExecuteReader();
                while (reader.Read())
                {
                    DataHolderBrand dataHolderBrand = new DataHolderBrand();
                    
                    var DataHolderBrandId = reader.GetString(0);
                    var DataHolderBrandjson = reader.GetString(1);

                    var jsonDocument = Convert.ToString(DataHolderBrandjson);                    
                    dataHolderBrand = System.Text.Json.JsonSerializer.Deserialize<DataHolderBrand>(jsonDocument);

                    dataHolderBrands.Add(dataHolderBrand);
                }

                db.Close();
                return dataHolderBrands;
            }            
        }
        
        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            if (dataHolderBrands.Any())
            {
                //data exisiting data first. 
             
                using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
                {
                    db.Open();

                    var commandText = $"Delete From DataHolderBrand";
                    using var command = new SqliteCommand(commandText, db);
                    command.ExecuteNonQuery();
                    db.Close();

                    dataHolderBrands.ToList().ForEach(dataholder => InsertTableDataHolder( dataholder));
                }                
            }            
        }

        private void InsertTableDataHolder(DataHolderBrand dataholder)
        {
            using (SqliteConnection db = new SqliteConnection(DatabaseConnectionString))
            {
                db.Open();                
                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(dataholder);

                var commandText = $"INSERT INTO DataHolderBrand(DataHolderBrandId, JsonDocument) VALUES('{dataholder.DataHolderBrandId}','{jsonDocument}')";
                using var command = new SqliteCommand(commandText, db);
                command.ExecuteNonQuery();
                
                db.Close();
            }
        }
    }
}
