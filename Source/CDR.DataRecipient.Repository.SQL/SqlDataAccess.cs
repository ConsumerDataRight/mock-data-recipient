using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlDataAccess : ISqlDataAccess
    {
        public IConfiguration _config { get; }
        public string _dbConn { get; set; }
        protected readonly RecipientDatabaseContext _mdrDatabaseContext;

        public SqlDataAccess(IConfiguration configuration, RecipientDatabaseContext recipientDatabaseContext)
        {
            _config = configuration;
            _dbConn = _config.GetConnectionString(DbConstants.ConnectionStringNames.Default);
            _mdrDatabaseContext = recipientDatabaseContext;
        }

        #region CdrArragements
        public async Task<ConsentArrangement> GetConsentByArrangement(string cdrArrangementId)
        {
            try
            {
                using (SqlConnection db = new SqlConnection(_dbConn))
                {
                    db.Open();

                    using var selectCommand = new SqlCommand($"SELECT JsonDocument FROM dbo.CdrArrangement WHERE CdrArrangementId = @id", db);
                    selectCommand.Parameters.AddWithValue("@id", cdrArrangementId);

                    var res = await selectCommand.ExecuteScalarAsync();
                    if (!string.IsNullOrEmpty(Convert.ToString(res)))
                    {
                        var jsonDocument = Convert.ToString(res);
                        var consentArrangement = JsonConvert.DeserializeObject<ConsentArrangement>(jsonDocument, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        db.Close();

                        return consentArrangement;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        public async Task<IEnumerable<ConsentArrangement>> GetConsents(string clientId, string userId)
        {
            List<ConsentArrangement> cdrArrangements = new List<ConsentArrangement>();
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                string sqlQuery = "SELECT [CdrArrangementId], [ClientId], [JsonDocument], UserId FROM [CdrArrangement]";

                if (!string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(userId))
                    sqlQuery += $" WHERE ClientId = @clientId";

                else if (string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(userId))
                    sqlQuery += $" WHERE UserId = @userId";

                else if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(userId))
                    sqlQuery += $" WHERE ClientId = @clientId AND UserId = @userId";

                using var commandText = new SqlCommand(sqlQuery, db);

                if (!string.IsNullOrEmpty(clientId))
                    commandText.Parameters.AddWithValue("@clientId", clientId);

                if (!string.IsNullOrEmpty(userId))
                    commandText.Parameters.AddWithValue("@userId", userId);

                SqlDataReader reader = await commandText.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var cdrArrangement = new ConsentArrangement();
                    var jsonDocument = Convert.ToString(reader.GetString(2));
                    cdrArrangement = JsonConvert.DeserializeObject<ConsentArrangement>(jsonDocument, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    cdrArrangements.Add(cdrArrangement);
                }

                db.Close();
                return cdrArrangements;
            }
        }

        public async Task InsertCdrArrangement(ConsentArrangement consentArrangement)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var clientId = new Guid(consentArrangement.ClientId);
                var jsonDocument = JsonConvert.SerializeObject(consentArrangement);

                var commandText = "";
                if (string.IsNullOrEmpty(consentArrangement.UserId))
                    commandText = $"INSERT INTO dbo.CdrArrangement(CdrArrangementId, ClientId, JsonDocument) VALUES('{consentArrangement.CdrArrangementId}','{clientId}','{jsonDocument}')";
                else
                    commandText = $"INSERT INTO dbo.CdrArrangement(CdrArrangementId, ClientId, JsonDocument, UserId) VALUES('{consentArrangement.CdrArrangementId}','{clientId}','{jsonDocument}','{consentArrangement.UserId}')";

                //special case for CdrArrangements
                commandText = commandText.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                commandText = commandText.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");

                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task UpdateCdrArrangement(ConsentArrangement consentArrangement)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var jsonDocument = JsonConvert.SerializeObject(consentArrangement, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                var commandText = $"UPDATE dbo.CdrArrangement SET JsonDocument='{jsonDocument}' WHERE CdrArrangementId=@id";

                //special case for CdrArrangements
                commandText = commandText.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                commandText = commandText.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");

                using var command = new SqlCommand(commandText, db);
                command.Parameters.AddWithValue("@id", consentArrangement.CdrArrangementId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteCdrArrangementData()
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var commandText = "DELETE FROM dbo.CdrArrangement";
                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteCdrArrangementData(string clientId)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var commandText = "DELETE FROM dbo.CdrArrangement WHERE ClientId = @clientId";
                using var command = new SqlCommand(commandText, db);
                command.Parameters.AddWithValue("clientId", clientId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteRegistrationData()
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var commandText = $"DELETE FROM dbo.Registration";
                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var commandText = $"DELETE FROM dbo.CdrArrangement WHERE CdrArrangementId=@id";

                using var command = new SqlCommand(commandText, db);
                command.Parameters.AddWithValue("@id", cdrArrangementId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        #endregion

        #region CrdRegistrations 

        public async Task<Registration> GetRegistration(string clientId)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                using var selectCommand = new SqlCommand($"SELECT JsonDocument FROM dbo.Registration WHERE ClientId = @id", db);
                selectCommand.Parameters.AddWithValue("@id", clientId);

                var res = await selectCommand.ExecuteScalarAsync();

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
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                using var commandText = new SqlCommand($"SELECT ClientId, JsonDocument FROM dbo.Registration", db);
                SqlDataReader reader = await commandText.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var jsonDocument = Convert.ToString(reader.GetString(1));
                    var registration = System.Text.Json.JsonSerializer.Deserialize<Registration>(jsonDocument);
                    registrations.Add(registration);
                }

                db.Close();
                return registrations;
            }
        }

        public async Task DeleteRegistration(string clientId)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var commandText = $"DELETE FROM dbo.Registration WHERE ClientId=@id";

                using var command = new SqlCommand(commandText, db);
                command.Parameters.AddWithValue("@id", clientId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task InsertRegistration(Registration registration)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(registration);
                var commandText = $"INSERT INTO dbo.Registration(ClientId, JsonDocument) VALUES('{registration.ClientId}','{jsonDocument}')";

                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task UpdateRegistration(Registration registration)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(registration);
                var commandText = $"UPDATE dbo.Registration SET JsonDocument='{jsonDocument}' WHERE ClientId=@id";

                using var command = new SqlCommand(commandText, db);
                command.Parameters.AddWithValue("@id", registration.ClientId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        #endregion

        #region DataHolderBrand

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                using var selectCommand = new SqlCommand($"SELECT JsonDocument FROM dbo.DataHolderBrand WHERE DataHolderBrandId = @id", db);
                selectCommand.Parameters.AddWithValue("@id", brandId);

                var res = await selectCommand.ExecuteScalarAsync();

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
            var dataHolderBrands = new List<DataHolderBrand>();

            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                using var commandText = new SqlCommand($"SELECT DataHolderBrandId, JsonDocument FROM dbo.DataHolderBrand", db);
                SqlDataReader reader = await commandText.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var dataHolderBrandjson = reader.GetString(1);
                        var jsonDocument = Convert.ToString(dataHolderBrandjson);
                        dataHolderBrands.Add(System.Text.Json.JsonSerializer.Deserialize<DataHolderBrand>(jsonDocument));
                    }
                }

                db.Close();

                return dataHolderBrands
                    .OrderBy(x => x.LegalEntity.LegalEntityName)
                    .ThenBy(x => x.BrandName)
                    .ToList();
            }
        }

        public async Task DataHolderBrandsDelete()
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var commandText = $"DELETE FROM dbo.DataHolderBrand";
                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task<(int, int)> AggregateDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrandsNew)
        {
            int inserted = 0;
            int updated = 0;
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var dataHolderBrandsOrig = new List<DataHolderBrand>();
                using var commandText = new SqlCommand($"SELECT DataHolderBrandId, JsonDocument, LastUpdated FROM dbo.DataHolderBrand", db);
                SqlDataReader reader = await commandText.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var dataHolderBrandjson = reader.GetString(1);
                        var jsonDocument = Convert.ToString(dataHolderBrandjson);
                        dataHolderBrandsOrig.Add(System.Text.Json.JsonSerializer.Deserialize<DataHolderBrand>(jsonDocument));

                        // NB: LastUpdated populated from jsonDocument when deserialised through entity
                        //     this is used below to compare with receied data to build updated list where LastUpdated is newer
                    }
                }

                db.Close();

                if (dataHolderBrandsOrig.Any())
                {
                    // Insert
                    IEnumerable<DataHolderBrand> dataHolderBrandsIns = dataHolderBrandsNew.Where(n => !dataHolderBrandsOrig.Any(o => o.DataHolderBrandId == n.DataHolderBrandId)).ToList();

                    // Update - when receiving list of new brands update tmp list below will be populated - compares LastUpdated from new list
                    //          with LastUpdated in DataHolderBrand table IF they don't exist then compares below to make updated list
                    IEnumerable<DataHolderBrand> dataHolderBrandsUpdTmp = dataHolderBrandsNew.Where(n => !dataHolderBrandsOrig.Any(o => o.DataHolderBrandId == n.DataHolderBrandId
                                                                                                    && o.LastUpdated == n.LastUpdated)).ToList();

                    // compare new to update lists above, any not in both lists are to be updated
                    IEnumerable<DataHolderBrand> dataHolderBrandsUpd = dataHolderBrandsUpdTmp.Where(n => !dataHolderBrandsIns.Any(o => o.DataHolderBrandId == n.DataHolderBrandId)).ToList();

                    if (dataHolderBrandsIns.Any())
                    {
                        inserted = dataHolderBrandsIns.Count();
                        dataHolderBrandsIns.ToList().ForEach(async dataholder => await InsertDataHolder(dataholder));
                    }
                    if (dataHolderBrandsUpd.Any())
                    {
                        updated = dataHolderBrandsUpd.Count();
                        dataHolderBrandsUpd.ToList().ForEach(async dataholder => await UpdateDataHolder(dataholder));
                    }
                }
                else
                {
                    // Old data does not exist ADD New data
                    if (dataHolderBrandsNew.Any())
                    {
                        inserted = dataHolderBrandsNew.Count();
                        dataHolderBrandsNew.ToList().ForEach(async dataholder => await InsertDataHolder(dataholder));
                    }
                }
            }
            return (inserted, updated);
        }

        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var commandText = $"DELETE FROM dbo.DataHolderBrand";
                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();
                db.Close();

                if (dataHolderBrands.Any())
                    dataHolderBrands.ToList().ForEach(async dataholder => await InsertDataHolder(dataholder));
            }
        }

        private async Task InsertDataHolder(DataHolderBrand dataholder)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(dataholder);

                var commandText = $"INSERT INTO dbo.DataHolderBrand (DataHolderBrandId, JsonDocument) VALUES ('{dataholder.DataHolderBrandId}','{jsonDocument}')";
                using var command = new SqlCommand(commandText, db);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task UpdateDataHolder(DataHolderBrand dataholder)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(dataholder);
                var commandText = $"UPDATE dbo.DataHolderBrand SET JsonDocument='{jsonDocument}' WHERE DataHolderBrandId=@id";

                using var command = new SqlCommand(commandText, db);
                command.Parameters.AddWithValue("@id", dataholder.DataHolderBrandId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }


        #endregion

        #region UnitTestSetup

        //For Unit Testing only
        public bool RecreateDatabaseWithForTests()
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                //Purging all exsiting data
                string sqlCommandText = @"IF EXISTS (SELECT * FROM sysobjects WHERE name='CdrArrangement' AND xtype='U') DROP TABLE IF EXISTS CdrArrangement; 
                                             IF EXISTS (SELECT * FROM sysobjects WHERE name='DataHolderBrand' AND xtype='U') DROP TABLE IF EXISTS DataHolderBrand; 
                                             IF EXISTS (SELECT * FROM sysobjects WHERE name='Registration' AND xtype='U') DROP TABLE IF EXISTS Registration;";

                SqlCommand SqlCommand = new SqlCommand(sqlCommandText, db);
                SqlCommand.ExecuteNonQuery();

                //Create fresh db. 
                SqlCommand.CommandText = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CdrArrangement' AND xtype='U') CREATE TABLE CdrArrangement ([CdrArrangementId] [uniqueidentifier] NOT NULL, [ClientId] [uniqueidentifier] NOT NULL, JsonDocument VARCHAR(MAX), [UserId] nvarchar(100) NULL);
                                           IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DataHolderBrand' AND xtype='U') CREATE TABLE DataHolderBrand ([DataHolderBrandId] [uniqueidentifier] NOT NULL, JsonDocument VARCHAR(MAX));
                                           IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Registration' AND xtype='U') CREATE TABLE Registration ([ClientId] [uniqueidentifier] NOT NULL, JsonDocument VARCHAR(MAX));";
                SqlCommand.ExecuteNonQuery();
                db.Close();

                //Insert test data
                InsertTestData();

                return true;
            }
        }

        public bool InsertTestData()
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();

                //Insert data holders data for testing
                var brandId = "cf217aba-e00d-48d5-9c3d-03af0b91cb80";
                var jsonDocument = @"{""_id"":{""$oid"":""613ef59c1a0ee5d9fd426a80""},""DataHolderBrandId"":""cf217aba-e00d-48d5-9c3d-03af0b91cb80"",""BrandName"":""Hall Bank"",""LegalEntity"":{""LegalEntityId"":""924ca498-0f19-402d-ae07-2cb61088f8aa"",""LegalEntityName"":""Hall Bank""},""Status"":""ACTIVE"",""EndpointDetail"":{""Version"":""1"",""PublicBaseUri"":""https://publicapi.hallbank"",""ResourceBaseUri"":""https://api.hallbank"",""InfoSecBaseUri"":""https://idp.hallbank"",""ExtensionBaseUri"":"""",""WebsiteUri"":""https://hallbank/""},""AuthDetails"":[{""RegisterUType"":""SIGNED-JWT"",""JwksEndpoint"":""https://hallbank/idp/jwks""}]}";

                var sqlCommandText = $"INSERT INTO DataHolderBrand (DataHolderBrandId, JsonDocument) VALUES ('{brandId}','{jsonDocument}')";
                SqlCommand SqlCommand = new SqlCommand(sqlCommandText, db);
                SqlCommand.ExecuteNonQuery();

                brandId = "cf217aba-e00d-48d5-9c3d-03af0b91cb81";
                jsonDocument = @"{""_id"":{""$oid"":""613ef59c1a0ee5d9fd426a81""},""DataHolderBrandId"":""cf217aba-e00d-48d5-9c3d-03af0b91cb81"",""BrandName"":""Hall Bank"",""LegalEntity"":{""LegalEntityId"":""924ca498-0f19-402d-ae07-2cb61088f8aa"",""LegalEntityName"":""Hall Bank""},""Status"":""ACTIVE"",""EndpointDetail"":{""Version"":""1"",""PublicBaseUri"":""https://publicapi.hallbank"",""ResourceBaseUri"":""https://api.hallbank"",""InfoSecBaseUri"":""https://idp.hallbank"",""ExtensionBaseUri"":"""",""WebsiteUri"":""https://hallbank/""},""AuthDetails"":[{""RegisterUType"":""SIGNED-JWT"",""JwksEndpoint"":""https://hallbank/idp/jwks""}]}";
                SqlCommand.CommandText = $"INSERT INTO DataHolderBrand (DataHolderBrandId, JsonDocument) VALUES ('{brandId}','{jsonDocument}')";
                SqlCommand.ExecuteNonQuery();

                //Insert cdr-arrangments data for testing
                var clientId = "bad06794-39e2-400c-9e1b-f15a0bb67f46";
                var cdrArrangementId = "92d260c1-a625-41e2-a777-c0af1912a74a";
                jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74a"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}";
                SqlCommand.CommandText = $"INSERT INTO CdrArrangement (CdrArrangementId, ClientId, JsonDocument) VALUES ('{cdrArrangementId}','{clientId}','{jsonDocument}')";
                SqlCommand.ExecuteNonQuery();

                cdrArrangementId = "92d260c1-a625-41e2-a777-c0af1912a74b";
                jsonDocument = @"{""_id"":{""$oid"":""613ef5b11a0ee5d9fd426a99""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""SharingDuration"":null,""CdrArrangementId"":""92d260c1-a625-41e2-a777-c0af1912a74b"",""IdToken"":null,""AccessToken"":null,""RefreshToken"":null,""ExpiresIn"":null,""Scope"":null,""TokenType"":null,""CreatedOn"":null}";
                SqlCommand.CommandText = $"INSERT INTO CdrArrangement (CdrArrangementId, ClientId, JsonDocument) VALUES ('{cdrArrangementId}','{clientId}','{jsonDocument}')";
                SqlCommand.ExecuteNonQuery();

                //Insert cdr-registrations data for testing 
                jsonDocument = @"{""_id"":{""$oid"":""6143d52c4433e41a861ea58d""},""DataHolderBrandId"":""804fc2fb-18a7-4235-9a49-2af393d18bc7"",""ClientId"":""bad06794-39e2-400c-9e1b-f15a0bb67f46"",""ClientIdIssuedAt"":1631835434,""ClientDescription"":""A product to help you manage your budget"",""ClientUri"":""https://mocksoftware/mybudgetapp"",""OrgId"":""ffb1c8ba-279e-44d8-96f0-1bc34a6b436f"",""OrgName"":""Mock Finance Tools"",""RedirectUris"":[""https://localhost:9001/consent/callback""],""LogoUri"":""https://mocksoftware/mybudgetapp/img/logo.png"",""TosUri"":""https://mocksoftware/mybudgetapp/terms"",""PolicyUri"":""https://mocksoftware/mybudgetapp/policy"",""JwksUri"":""https://localhost:9001/jwks"",""RevocationUri"":""https://localhost:9001/revocation"",""RecipientBaseUri"":""https://localhost:9001"",""TokenEndpointAuthSigningAlg"":""PS256"",""TokenEndpointAuthMethod"":""private_key_jwt"",""GrantTypes"":[""client_credentials"",""authorization_code"",""refresh_token""],""ResponseTypes"":[""code id_token""],""ApplicationType"":""web"",""IdTokenSignedResponseAlg"":""PS256"",""IdTokenEncryptedResponseAlg"":""RSA-OAEP"",""IdTokenEncryptedResponseEnc"":""A256GCM"",""RequestObjectSigningAlg"":""PS256"",""SoftwareStatement"":""eyJhbGciOiJQUzI1NiIsImtpZCI6IjU0MkE5QjkxNjAwNDg4MDg4Q0Q0RDgxNjkxNkE5RjQ0ODhERDI2NTEiLCJ0eXAiOiJKV1QifQ.ew0KICAicmVjaXBpZW50X2Jhc2VfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEiLA0KICAibGVnYWxfZW50aXR5X2lkIjogIjE4Yjc1YTc2LTU4MjEtNGM5ZS1iNDY1LTQ3MDkyOTFjZjBmNCIsDQogICJsZWdhbF9lbnRpdHlfbmFtZSI6ICJNb2NrIFNvZnR3YXJlIENvbXBhbnkiLA0KICAiaXNzIjogImNkci1yZWdpc3RlciIsDQogICJpYXQiOiAxNjMxODM1NDE3LA0KICAiZXhwIjogMTYzMTgzNjAxNywNCiAgImp0aSI6ICJjNzYzYjU4NzJkNGY0MzIwOWE3NmUzOTU3YTAzMDgwNCIsDQogICJvcmdfaWQiOiAiZmZiMWM4YmEtMjc5ZS00NGQ4LTk2ZjAtMWJjMzRhNmI0MzZmIiwNCiAgIm9yZ19uYW1lIjogIk1vY2sgRmluYW5jZSBUb29scyIsDQogICJjbGllbnRfbmFtZSI6ICJNeUJ1ZGdldEhlbHBlciIsDQogICJjbGllbnRfZGVzY3JpcHRpb24iOiAiQSBwcm9kdWN0IHRvIGhlbHAgeW91IG1hbmFnZSB5b3VyIGJ1ZGdldCIsDQogICJjbGllbnRfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwIiwNCiAgInJlZGlyZWN0X3VyaXMiOiBbDQogICAgImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvY29uc2VudC9jYWxsYmFjayINCiAgXSwNCiAgImxvZ29fdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL2ltZy9sb2dvLnBuZyIsDQogICJ0b3NfdXJpIjogImh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL3Rlcm1zIiwNCiAgInBvbGljeV91cmkiOiAiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvcG9saWN5IiwNCiAgImp3a3NfdXJpIjogImh0dHBzOi8vbG9jYWxob3N0OjkwMDEvandrcyIsDQogICJyZXZvY2F0aW9uX3VyaSI6ICJodHRwczovL2xvY2FsaG9zdDo5MDAxL3Jldm9jYXRpb24iLA0KICAic29mdHdhcmVfaWQiOiAiYzYzMjdmODctNjg3YS00MzY5LTk5YTQtZWFhY2QzYmI4MjEwIiwNCiAgInNvZnR3YXJlX3JvbGVzIjogImRhdGEtcmVjaXBpZW50LXNvZnR3YXJlLXByb2R1Y3QiLA0KICAic2NvcGUiOiAib3BlbmlkIHByb2ZpbGUgYmFuazphY2NvdW50cy5iYXNpYzpyZWFkIGJhbms6YWNjb3VudHMuZGV0YWlsOnJlYWQgYmFuazp0cmFuc2FjdGlvbnM6cmVhZCBiYW5rOnBheWVlczpyZWFkIGJhbms6cmVndWxhcl9wYXltZW50czpyZWFkIGNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIGNvbW1vbjpjdXN0b21lci5kZXRhaWw6cmVhZCBjZHI6cmVnaXN0cmF0aW9uIg0KfQ.j_UwVV2g28047YN12KdsGxE3pQwXVkF_ZSCwq7_HLdrlnQKZHsReQCprtxk-MV9vH0EGwpMw46WFQV5pTB-mxwZZfhkQx0-U30ufJfmPwvpxxAI90gFl3MFtQbwgC5a8IkkVfjSUoK1-m-pgG3X79rf0zUB9aRZoSigXgVemKfnQeiB-Gx_TI3zi0QkF1Uw052dAATQvUvaZ040oyqWuTFKETG7AzTV6M1ZcxVJYX5gGhemFIoWA0bVqrP3-dEMUOLFhhFwe3otMMB7iaBfOjBmQ9xtlnnmxFGIGvHErBiHouwfGzG0jCqI5dwtKkicjNKoa4uq-ul3EGup8FWY4Vw"",""SoftwareId"":""c6327f87-687a-4369-99a4-eaacd3bb8210"",""Scope"":""openid profile bank:accounts.basic:read bank:transactions:read common:customer.basic:read cdr:registration""}";
                SqlCommand.CommandText = $"INSERT INTO Registration (ClientId, JsonDocument) VALUES ('{clientId}','{jsonDocument}')";
                SqlCommand.ExecuteNonQuery();

                db.Close();
                return true;
            }
        }

        #endregion
    }
}