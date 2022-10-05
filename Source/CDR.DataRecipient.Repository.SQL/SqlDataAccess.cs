using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.SDK.Enum;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public SqlDataAccess(string connString)
        {
            _dbConn = connString;
        }

        #region CdrArragements
        public async Task<ConsentArrangement> GetConsentByArrangement(string cdrArrangementId)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();

                    using var sqlCommand = new SqlCommand("SELECT JsonDocument FROM dbo.CdrArrangement WHERE CdrArrangementId = @id", db);
                    sqlCommand.Parameters.AddWithValue("@id", cdrArrangementId);

                    var res = await sqlCommand.ExecuteScalarAsync();
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

        public async Task<IEnumerable<ConsentArrangement>> GetConsents(string clientId, string dataHolderBrandId, string userId)
        {
            List<ConsentArrangement> cdrArrangements = new();
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = new StringBuilder();
                sqlQuery.Append("SELECT [CdrArrangementId], [ClientId], [JsonDocument], UserId FROM [CdrArrangement]");
                sqlQuery.Append(" WHERE UserId = @userId");

                if (!string.IsNullOrEmpty(clientId))
                {
                    sqlQuery.Append(" AND ClientId = @clientId");
                }

                using var sqlCommand = new SqlCommand(sqlQuery.ToString(), db);
                sqlCommand.Parameters.AddWithValue("@userId", userId);

                if (!string.IsNullOrEmpty(clientId))
                {
                    sqlCommand.Parameters.AddWithValue("@clientId", clientId);
                }

                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var cdrArrangement = new ConsentArrangement();
                    var jsonDocument = Convert.ToString(reader.GetString(2));
                    cdrArrangement = JsonConvert.DeserializeObject<ConsentArrangement>(jsonDocument, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    // Check if the arrangement belongs to the same data holder.
                    if (string.IsNullOrEmpty(dataHolderBrandId) || cdrArrangement.DataHolderBrandId.Equals(dataHolderBrandId, StringComparison.OrdinalIgnoreCase))
                    {
                        cdrArrangements.Add(cdrArrangement);
                    }
                }
                db.Close();

                if (cdrArrangements.Any())
                {
                    foreach (var arr in cdrArrangements)
                    {
                        arr.BrandName = await GetDataHolderBrandName(arr.DataHolderBrandId);
                    }
                }
                return cdrArrangements;
            }
        }

        public async Task InsertCdrArrangement(ConsentArrangement consentArrangement)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var clientId = new Guid(consentArrangement.ClientId);
                var jsonDocument = JsonConvert.SerializeObject(consentArrangement);

                //special case for CdrArrangements
                jsonDocument = jsonDocument.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                jsonDocument = jsonDocument.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");

                var sqlQuery = "";
                if (string.IsNullOrEmpty(consentArrangement.UserId))                    
                    sqlQuery = "INSERT INTO dbo.CdrArrangement(CdrArrangementId, ClientId, JsonDocument) VALUES(@arrangementId, @clientId, @jsonDocument)";
                else                    
                    sqlQuery = "INSERT INTO dbo.CdrArrangement(CdrArrangementId, ClientId, JsonDocument, UserId) VALUES(@arrangementId, @clientId, @jsonDocument, @userId)";
                                
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@arrangementId", consentArrangement.CdrArrangementId);
                sqlCommand.Parameters.AddWithValue("@clientId", clientId);
                sqlCommand.Parameters.AddWithValue("@jsonDocument", jsonDocument);
                    
                if (!string.IsNullOrEmpty(consentArrangement.UserId))
                    sqlCommand.Parameters.AddWithValue("@userId", consentArrangement.UserId);
                    
                await sqlCommand.ExecuteNonQueryAsync();
                db.Close();                                
            }
        }

        public async Task UpdateCdrArrangement(ConsentArrangement consentArrangement)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var jsonDocument = JsonConvert.SerializeObject(consentArrangement, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
                //special case for CdrArrangements
                jsonDocument = jsonDocument.Replace(@"""CreatedOn"":""0001-01-01T00:00:00""", @"""CreatedOn"": null");
                jsonDocument = jsonDocument.Replace(@"""ExpiresIn"":0", @"""ExpiresIn"": null");                
                var sqlQuery = "UPDATE dbo.CdrArrangement SET JsonDocument=@jsonDocument WHERE CdrArrangementId=@id";

                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", consentArrangement.CdrArrangementId);
                sqlCommand.Parameters.AddWithValue("@jsonDocument", jsonDocument);
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteCdrArrangementData()
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlQuery = "DELETE FROM dbo.CdrArrangement";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteCdrArrangementData(string clientId)
        {
            using (SqlConnection db = new SqlConnection(_dbConn))
            {
                db.Open();
                var sqlQuery = "DELETE FROM dbo.CdrArrangement WHERE ClientId = @clientId";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("clientId", clientId);
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteRegistrationData()
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlQuery = "DELETE FROM dbo.Registration";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlQuery = "DELETE FROM dbo.CdrArrangement WHERE CdrArrangementId=@id";

                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", cdrArrangementId);
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        #endregion

        #region CrdRegistrations 

        /// <summary>
        /// Get the Registrations for this Data Holders
        /// </summary>
        /// <param name="dhBrandId">The STRING DataHolderBrandId</param>
        /// <remarks>
        /// This is called from Azure DiscoverDataHolders Function, it is used to add the DCR message to the queue
        /// </remarks>
        /// <returns>[true|false]</returns>
        public async Task<bool> CheckRegistrationExist(string dhBrandId)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                using var sqlCommand = new SqlCommand("SELECT [DataHolderBrandId] FROM [Registration] WHERE [DataHolderBrandId] = @id", db);
                sqlCommand.Parameters.AddWithValue("@id", dhBrandId);
                var res = await sqlCommand.ExecuteScalarAsync();
                db.Close();

                if (!string.IsNullOrEmpty(Convert.ToString(res)))
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Check if the Data Holder Brands are Registered - (Sandbox Mode)
        /// </summary>
        /// <param name="newDhBrands">List of Discovered Data Holders</param>
        /// <remarks>
        /// This is called from Azure DiscoverDataHolders Function, it is used to process the Insert and Update list
        /// of data holders for use in performing the DCR.
        /// </remarks>
        /// <returns>Lists if data holders to be Registered</returns>
        public async Task<(IList<DataHolderBrand>, IList<DataHolderBrand>)> CheckRegistrationsExist(IList<DataHolderBrand> newDhBrands)
        {
            IList<DataHolderBrand> dhBrandsIns = new List<DataHolderBrand>();
            IList<DataHolderBrand> dhBrandsUpd = new List<DataHolderBrand>();

            using (SqlConnection db = new(_dbConn))
            {
                foreach (var dh in newDhBrands)
                {
                    db.Open();
                    using var sqlCommand = new SqlCommand("SELECT [JsonDocument] FROM [Registration] WHERE [DataHolderBrandId] = @id", db);
                    sqlCommand.Parameters.AddWithValue("@id", dh.DataHolderBrandId.ToString());
                    SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                    if (reader.HasRows)
                    {
                        bool update = false;
                        while (reader.Read() && !update)
                        {
                            var jsonDocument = Convert.ToString(reader.GetString(0));
                            var reg = System.Text.Json.JsonSerializer.Deserialize<Registration>(jsonDocument);

                            if (!string.Equals(dh.BrandName, reg.BrandName))
                                update = true;
                        }
                        if (update)
                        {
                            dhBrandsUpd.Add(new DataHolderBrand
                            {
                                DataHolderBrandId = dh.DataHolderBrandId.ToString(),
                                BrandName = dh.BrandName,
                                LastUpdated = dh.LastUpdated,
                                EndpointDetail = new EndpointDetail
                                {
                                    InfoSecBaseUri = dh.EndpointDetail.InfoSecBaseUri
                                }
                            });
                        }
                    }
                    else
                    {
                        dhBrandsIns.Add(new DataHolderBrand
                        {
                            DataHolderBrandId = dh.DataHolderBrandId.ToString(),
                            BrandName = dh.BrandName,
                            LastUpdated = dh.LastUpdated,
                            EndpointDetail = new EndpointDetail
                            {
                                InfoSecBaseUri = dh.EndpointDetail.InfoSecBaseUri
                            }
                        });
                    }

                    db.Close();
                }
            }
            return (dhBrandsIns, dhBrandsUpd);
        }

        public async Task<Registration> GetRegistration(string clientId, string dataHolderBrandId)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                using var sqlCommand = new SqlCommand("SELECT [JsonDocument] FROM [Registration] WHERE [ClientId] = @clientId AND [DataHolderBrandId] = @dataHolderBrandId", db);
                sqlCommand.Parameters.AddWithValue("@clientId", clientId);
                sqlCommand.Parameters.AddWithValue("@dataHolderBrandId", dataHolderBrandId);

                var res = await sqlCommand.ExecuteScalarAsync();

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

        /// <summary>
        /// Return the Registration detail from the local repo
        /// </summary>
        /// <param name="dhBrandId">The registered DataHolderBrandId</param>
        /// <remarks>
        /// This is called from Azure DCR Function.
        /// </remarks>
        /// <returns>[true|false]</returns>
        /// This needs to be string?
        public async Task<Guid> GetRegByDHBrandId(string dhBrandId)
        {
            Guid clientId = Guid.Empty;
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                using var sqlCommand = new SqlCommand("SELECT [ClientId], [DataHolderBrandId] FROM [Registration] WHERE [DataHolderBrandId] = @id", db);
                sqlCommand.Parameters.AddWithValue("@id", dhBrandId);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        clientId = reader.GetGuid(0);
                    }
                }

                db.Close();
            }
            return clientId;
        }

        public async Task<IEnumerable<Registration>> GetRegistrations()
        {
            List<Registration> registrations = new();
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                using var sqlCommand = new SqlCommand("SELECT ClientId, JsonDocument FROM dbo.Registration", db);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var jsonDocument = Convert.ToString(reader.GetString(1));
                    var registration = System.Text.Json.JsonSerializer.Deserialize<Registration>(jsonDocument);
                    registrations.Add(registration);
                }
                db.Close();

                if (registrations.Any())
                {
                    foreach (var reg in registrations)
                    {
                        reg.BrandName = await GetDataHolderBrandName(reg.DataHolderBrandId);
                    }
                }
                return registrations;
            }
        }

        public async Task DeleteRegistration(string clientId, string dataHolderBrandId)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlCommand = "DELETE FROM dbo.Registration WHERE [ClientId] = @clientId AND [DataHolderBrandId] = @dataHolderBrandId";

                using var command = new SqlCommand(sqlCommand, db);
                command.Parameters.AddWithValue("@clientId", clientId);
                command.Parameters.AddWithValue("@dataHolderBrandId", dataHolderBrandId);
                await command.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        public async Task<bool> InsertRegistration(Registration registration)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();
                    var dhBrandId = new Guid(registration.DataHolderBrandId);
                    var jsonDocument = System.Text.Json.JsonSerializer.Serialize(registration);
                    var sqlCommand = "INSERT INTO [Registration] ([ClientId], [DataHolderBrandId], [JsonDocument]) VALUES(@clientId, @dhBrndId, @jsonDoc)";
                    using var cmd = new SqlCommand(sqlCommand, db);
                    cmd.Parameters.AddWithValue("@clientId", registration.ClientId);
                    cmd.Parameters.AddWithValue("@dhBrndId", dhBrandId);
                    cmd.Parameters.AddWithValue("@jsonDoc", jsonDocument);
                    await cmd.ExecuteNonQueryAsync();
                    db.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Return a list of Registrations from the DcrMessage table in the local repo
        /// </summary>
        /// <remarks>
        /// This is used in the DCR View using the table data populated from the Azure DCR Function.
        /// </remarks>
        /// <returns>The list of registrations</returns>
        public async Task<IEnumerable<Registration>> GetDcrMessageRegistrations()
        {
            List<Registration> registrations = new();
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlQuery = "SELECT [ClientId],[DataHolderBrandId],[BrandName],[MessageState],[LastUpdated] FROM [dbo].[DcrMessage] WHERE [MessageState] != @msgState";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@msgState", MessageEnum.Pending.ToString());
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var registration = new Registration
                        {
                            ClientId = reader.IsDBNull(0) ? "" : reader.GetString(0),
                            DataHolderBrandId = Convert.ToString(reader.GetGuid(1)),
                            BrandName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            MessageState = reader.GetString(3),
                            LastUpdated = reader.GetDateTime(4)
                        };
                        registrations.Add(registration);
                    }
                }
                db.Close();
                return registrations;
            }
        }

        /// <summary>
        /// Insert the Registration details into the local repo
        /// </summary>
        /// <param name="regClientId">ClientId as returned from the Register</param>
        /// <param name="dcrDHBrandId">The DataHolderBrandId being registered</param>
        /// <param name="jsonDocument">The response as a json string</param>
        /// <remarks>
        /// This is called from Azure DCR Function, it updates the local repo after performing the DCR in the Register.
        /// </remarks>
        /// <returns>[true|false]</returns>
        public async Task<bool> InsertDcrRegistration(string regClientId, string dcrDHBrandId, string jsonDocument)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();
                    var dhBrandId = new Guid(dcrDHBrandId);
                    var sqlQuery = "INSERT INTO [Registration] ([ClientId], [DataHolderBrandId], [JsonDocument]) VALUES(@clientId, @dhBrndId, @jsonDoc)";
                    using var sqlCommand = new SqlCommand(sqlQuery, db);
                    sqlCommand.Parameters.AddWithValue("@clientId", regClientId);
                    sqlCommand.Parameters.AddWithValue("@dhBrndId", dhBrandId);
                    sqlCommand.Parameters.AddWithValue("@jsonDoc", jsonDocument);
                    await sqlCommand.ExecuteNonQueryAsync();
                    db.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task UpdateRegistration(Registration registration)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(registration);                
                var sqlQuery = "UPDATE dbo.Registration SET JsonDocument=@jsonDocument WHERE [ClientId] = @clientId AND [DataHolderBrandId] = @dataHolderBrandId";

                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@clientId", registration.ClientId);
                sqlCommand.Parameters.AddWithValue("@dataHolderBrandId", registration.DataHolderBrandId);
                sqlCommand.Parameters.AddWithValue("@jsonDocument", jsonDocument);                
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        #endregion

        #region DataHolderBrand

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                using var sqlCommand = new SqlCommand("SELECT JsonDocument FROM dbo.DataHolderBrand WHERE DataHolderBrandId = @id", db);
                sqlCommand.Parameters.AddWithValue("@id", brandId);

                var res = await sqlCommand.ExecuteScalarAsync();

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

        public async Task<string> GetDataHolderBrandName(string brandId)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                using var sqlCommand = new SqlCommand("SELECT JsonDocument FROM dbo.DataHolderBrand WHERE DataHolderBrandId = @id", db);
                sqlCommand.Parameters.AddWithValue("@id", brandId);

                var res = await sqlCommand.ExecuteScalarAsync();

                if (!string.IsNullOrEmpty(Convert.ToString(res)))
                {
                    var jsonDocument = Convert.ToString(res);
                    var dataholderbrand = System.Text.Json.JsonSerializer.Deserialize<DataHolderBrand>(jsonDocument);

                    db.Close();
                    return dataholderbrand.BrandName;
                }
            }
            return null;
        }

        public async Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands()
        {
            List<DataHolderBrand> dataHolderBrands = new();
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                using var sqlCommand = new SqlCommand("SELECT DataHolderBrandId, JsonDocument FROM dbo.DataHolderBrand", db);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
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
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = "DELETE FROM dbo.DataHolderBrand";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                await sqlCommand.ExecuteNonQueryAsync();

                db.Close();
            }
        }

        /// <summary>
        /// Aggregate the Data Holders - updates the repo (Mock Mode)
        /// </summary>
        /// <param name="dhBrandsNew">List of Discovered Data Holders</param>
        /// <returns>Count of data holders to be inserted and updated</returns>
        public async Task<(int, int)> AggregateDataHolderBrands(IList<DataHolderBrand> dhBrandsNew)
        {
            IList<DataHolderBrand> dhBrandsIns = new List<DataHolderBrand>();
            IList<DataHolderBrand> dhBrandsUpd = new List<DataHolderBrand>();

            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                IList<DataHolderBrand> dataHolderBrandsOrig = new List<DataHolderBrand>();
                using var sqlCommand = new SqlCommand("SELECT [DataHolderBrandId], [JsonDocument], [LastUpdated] FROM [DataHolderBrand]", db);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        // NB: LastUpdated populated from jsonDocument when deserialised through entity
                        //     this is used below to compare with received data to build updated list where LastUpdated is newer
                        var jsonDocument = Convert.ToString(reader.GetString(1));
                        dataHolderBrandsOrig.Add(System.Text.Json.JsonSerializer.Deserialize<DataHolderBrand>(jsonDocument));
                    }
                }

                db.Close();

                if (dataHolderBrandsOrig.Any())
                {
                    // Insert
                    dhBrandsIns = dhBrandsNew.Where(n => !dataHolderBrandsOrig.Any(o => o.DataHolderBrandId == n.DataHolderBrandId)).ToList();

                    // Update - when receiving list of new brands update tmp list below will be populated - compares LastUpdated from new list
                    //          with LastUpdated in DataHolderBrand table IF they don't exist then compares below to make updated list
                    IList<DataHolderBrand> dataHolderBrandsUpdTmp = dhBrandsNew.Where(n => !dataHolderBrandsOrig.Any(o => o.DataHolderBrandId == n.DataHolderBrandId
                                                                                                    && o.LastUpdated == n.LastUpdated)).ToList();

                    // compare new to update lists above, any not in both lists are to be updated
                    dhBrandsUpd = dataHolderBrandsUpdTmp.Where(n => !dhBrandsIns.Any(o => o.DataHolderBrandId == n.DataHolderBrandId)).ToList();

                    if (dhBrandsIns.Any())
                    {
                        dhBrandsIns.ToList().ForEach(async dataholder => await InsertDataHolder(dataholder));
                    }
                    if (dhBrandsUpd.Any())
                    {
                        dhBrandsUpd.ToList().ForEach(async dataholder => await UpdateDataHolder(dataholder));
                    }
                }
                else
                {
                    // Old data does not exist ADD New data
                    if (dhBrandsNew.Any())
                    {
                        dhBrandsIns = dhBrandsNew.ToList();
                        dhBrandsNew.ToList().ForEach(async dataholder => await InsertDataHolder(dataholder));
                    }
                }
            }
            return (dhBrandsIns.Count, dhBrandsUpd.Count);
        }

        /// <summary>
        /// Insert the Data Holder into the repo
        /// </summary>
        /// <param name="dataholder">The Data Holder to be inserted</param>
        /// <remarks>
        /// This is called from above to Insert the Data Holder into the repo.
        /// This is also called from Azure DiscoverDataHolders Function, it is used to Insert the data holder after performing the DCR.
        /// </remarks>
        /// <returns>Boolean status only consumed in the Azure Functions DiscoverDataHolders</returns>
        public async Task<bool> InsertDataHolder(DataHolderBrand dataholder)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();
                    var jsonDocument = System.Text.Json.JsonSerializer.Serialize(dataholder);
                    var sqlQuery = "INSERT INTO [DataHolderBrand] ([DataHolderBrandId], [JsonDocument], [LastUpdated]) VALUES (@dhBrndId, @jsonDoc, GETUTCDATE())";
                    using var sqlCommand = new SqlCommand(sqlQuery, db);
                    sqlCommand.Parameters.AddWithValue("@dhBrndId", dataholder.DataHolderBrandId);
                    sqlCommand.Parameters.AddWithValue("@jsonDoc", jsonDocument);
                    await sqlCommand.ExecuteNonQueryAsync();
                    db.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Update the Data Holder
        /// </summary>
        /// <param name="dataholder">The Data Holder to be updated</param>
        /// <remarks>
        /// This is called from above to Update the Data Holder in the repo.
        /// This is also called from Azure DiscoverDataHolders Function, it is used to Upate the data holder after performing the DCR.
        /// </remarks>
        /// <returns>Boolean status only consumed in the Azure Functions DiscoverDataHolders</returns>
        public async Task<bool> UpdateDataHolder(DataHolderBrand dataholder)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();

                    var jsonDocument = System.Text.Json.JsonSerializer.Serialize(dataholder);                    
                    var sqlQuery = "UPDATE [DataHolderBrand] SET [JsonDocument] = @jsonDocument, [LastUpdated] = GETUTCDATE() WHERE [DataHolderBrandId] = @id";
                    using var sqlCommand = new SqlCommand(sqlQuery, db);
                    sqlCommand.Parameters.AddWithValue("@id", dataholder.DataHolderBrandId);
                    sqlCommand.Parameters.AddWithValue("@jsonDocument", jsonDocument);
                    await sqlCommand.ExecuteNonQueryAsync();

                    db.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Delete the Data Holder into the repo
        /// </summary>
        /// <param name="dataholder">The Data Holder to be deleted</param>
        /// <remarks>
        /// This is called from above to delete the Data Holder into the repo.
        /// This is also called from Azure DiscoverDataHolders Function, it is used to delete the data holder after performing the DCR.
        /// </remarks>
        /// <returns>Boolean status only consumed in the Azure Functions DiscoverDataHolders</returns>
        public async Task<bool> DeleteDataHolder(string dataholderBrndId)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();
                    
                    var sqlQuery = "SELECT [ClientId] FROM [dbo].[DcrMessage] WHERE [DataHolderBrandId] = @dcrBrandId";                    
                    using var sqlCommand = new SqlCommand(sqlQuery, db);
                    sqlCommand.Parameters.AddWithValue("@dcrBrandId", dataholderBrndId);
                    
                    var dcrClientId = await sqlCommand.ExecuteScalarAsync();

                    if (!String.IsNullOrEmpty(dcrClientId.ToString()))
                    {
                        //Remove cdrArrangements
                        sqlQuery = "DELETE FROM [CdrArrangement] WHERE [ClientId] = @cdrClientId";
                        using var sqlCdrCommand = new SqlCommand(sqlQuery, db);
                        sqlCdrCommand.Parameters.AddWithValue("@cdrClientId", dcrClientId.ToString());
                        await sqlCdrCommand.ExecuteNonQueryAsync();

                        //Remove Registrations
                        sqlQuery = "DELETE FROM [Registration] WHERE [ClientId] = @regClientId";
                        using var sqlRegCommand = new SqlCommand(sqlQuery, db);
                        sqlRegCommand.Parameters.AddWithValue("@regClientId", dcrClientId.ToString());
                        await sqlRegCommand.ExecuteNonQueryAsync();
                    }

                    //Remove DcrMessage 
                    sqlQuery = "DELETE FROM [dbo].[DcrMessage] WHERE [DataHolderBrandId] = @dcrDataHolderBrandId";
                    using var sqldcrDhBrandQuery = new SqlCommand(sqlQuery, db);
                    sqldcrDhBrandQuery.Parameters.AddWithValue("@dcrDataHolderBrandId", dataholderBrndId);
                    await sqldcrDhBrandQuery.ExecuteNonQueryAsync();

                    //Remove DataHolder brands
                    sqlQuery = "DELETE FROM [DataHolderBrand] WHERE [DataHolderBrandId] = @dhBrandId";
                    using var sqldhBrandQuery = new SqlCommand(sqlQuery, db);
                    sqldhBrandQuery.Parameters.AddWithValue("@dhBrandId", dataholderBrndId);
                    await sqldhBrandQuery.ExecuteNonQueryAsync();

                    db.Close();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = "DELETE FROM DataHolderBrand";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                await sqlCommand.ExecuteNonQueryAsync();
                db.Close();

                if (dataHolderBrands.Any())
                    dataHolderBrands.ToList().ForEach(async dataholder => await InsertDataHolder(dataholder));
            }
        }

        public async Task<IEnumerable<DataRecipientViewModel>> GetSoftwareProducts()
        {
            List<DataRecipientViewModel> rtnList = new();

            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                using var sqlCommand = new SqlCommand("SELECT SoftwareProductId, SoftwareProductName FROM SoftwareProduct", db);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var swProductId = reader.GetGuid(0);
                        var swProductName = reader.GetString(1);

                        rtnList.Add(new DataRecipientViewModel
                        {
                            SoftwareProductId = swProductId.ToString(),
                            SoftwareProductName = swProductName
                        });
                    }
                }

                db.Close();
                return rtnList;
            }
        }

        public async Task PersistSoftwareProducts(IEnumerable<DataRecipientModel> dataRecipients)
        {
            if (dataRecipients.Any())
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();
                    var sqlQuery = "DELETE FROM SoftwareProduct";
                    using var sqlCommand = new SqlCommand(sqlQuery, db);
                    await sqlCommand.ExecuteNonQueryAsync();
                    db.Close();

                    dataRecipients.ToList().ForEach(async dataRecipient => await InsertSoftwareProduct(dataRecipient.DataRecipientBrands));
                }
            }
        }

        private async Task InsertSoftwareProduct(IEnumerable<DRBrand> drBrands)
        {
            if (drBrands == null || !drBrands.Any())
            {
                // Nothing to insert.
                return;
            }

            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                foreach (var brand in drBrands)
                {
                    if (brand.SoftwareProducts != null && brand.SoftwareProducts.Count > 0)
                    {
                        foreach (var swProduct in brand.SoftwareProducts)
                        {                            
                            var sqlQuery = "INSERT INTO SoftwareProduct (SoftwareProductId, BrandId, SoftwareProductName, SoftwareProductDescription, LogoUri, RecipientBaseUri, RedirectUri, Scope, Status) " +
                                "VALUES (@softwareProductId, @dataRecipientBrandId, @softwareProductName, @softwareProductDescription, @logoUri, @recipientBaseUri, @redirectUri, @scope, @status)";
                            using var sqlCommand = new SqlCommand(sqlQuery, db);
                            sqlCommand.Parameters.AddWithValue("@softwareProductId", swProduct.SoftwareProductId);
                            sqlCommand.Parameters.AddWithValue("@dataRecipientBrandId", brand.DataRecipientBrandId);
                            sqlCommand.Parameters.AddWithValue("@softwareProductName", swProduct.SoftwareProductName);
                            sqlCommand.Parameters.AddWithValue("@softwareProductDescription", swProduct.SoftwareProductDescription);
                            sqlCommand.Parameters.AddWithValue("@logoUri", swProduct.LogoUri);
                            sqlCommand.Parameters.AddWithValue("@recipientBaseUri", swProduct.RecipientBaseUri);
                            sqlCommand.Parameters.AddWithValue("@redirectUri", swProduct.RedirectUri);
                            sqlCommand.Parameters.AddWithValue("@scope", swProduct.Scope);
                            sqlCommand.Parameters.AddWithValue("@status", swProduct.Status);
                            await sqlCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
                db.Close();
            }
        }

        /// <summary>
        /// Check if the DataHolderBrandId exist
        /// </summary>
        /// <param name="dhBrandId">The DataHolderBrandId</param>
        /// <remarks>
        /// This is called from Azure DiscoverDataHolders and DCR Functions, to prevent multiple queue entries for the same DataHolderBrandId
        /// </remarks>
        /// <returns>The DataHolderBrandId and BrandName</returns>
        public async Task<DataHolderBrand> GetDHBrandById(string dhBrandId)
        {
            DataHolderBrand dh = new DataHolderBrand();
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = "SELECT [DataHolderBrandId], [BrandName], [InfosecBaseUri] FROM [DcrMessage] WHERE [DataHolderBrandId] = @id";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", dhBrandId);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        dh.DataHolderBrandId = dhBrandId;
                        dh.BrandName = reader.GetString(1);
                        dh.EndpointDetail = new EndpointDetail
                        {
                            InfoSecBaseUri = reader.GetString(2)
                        };
                    }
                }

                db.Close();
            }
            return dh;
        }

        /// <summary>
        /// Check if the queue message exists by DataHolderBrandId
        /// </summary>
        /// <param name="dhBrandId">The DataHolderBrandId</param>
        /// <remarks>
        /// This is called from Azure DiscoverDataHolders and DCR Functions, to prevent multiple queue entries for the same DataHolderBrandId
        /// </remarks>
        /// <returns>The MessageId and the MessageState</returns>
        public async Task<(string, string)> CheckDcrMessageExistByDHBrandId(string dhBrandId)
        {
            string msgId = "";
            string msgState = "";
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = "SELECT [MessageId], [MessageState] FROM [DcrMessage] WHERE [DataHolderBrandId] = @id";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", dhBrandId);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        msgId = reader.GetString(0);
                        msgState = reader.GetString(1);
                    }
                }

                db.Close();
            }
            return (msgId, msgState);
        }

        /// <summary>
        /// Check if the queue message exists by the Queue MessageId
        /// </summary>
        /// <param name="dhMessageId">The message object to be inserted</param>
        /// <remarks>
        /// This is called from Azure Functions DiscoverDataHolders, to prevent multiple queue entries for the same DataHolderBrandId
        /// </remarks>
        /// <returns>The MessageId and the MessageState</returns>
        public async Task<(string, string)> CheckDcrMessageExistByMessageId(string dhMessageId)
        {
            string msgId = "";
            string msgState = "";
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = "SELECT [MessageId], [MessageState] FROM [DcrMessage] WHERE [MessageId] = @id";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", dhMessageId);
                SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        msgId = reader.GetString(0);
                        msgState = reader.GetString(1);
                    }
                }

                db.Close();
            }
            return (msgId, msgState);
        }

        /// <summary>
        /// Insert the queue message status
        /// </summary>
        /// <param name="dcrMessage">The message object to be inserted</param>
        /// <remarks>
        /// This is called from Azure DiscoverDataHolders Function
        /// </remarks>
        /// <returns>[true|false]</returns>
        public async Task<bool> InsertDcrMessage(DcrMessage dcrMessage)
        {
            try
            {
                using (SqlConnection db = new(_dbConn))
                {
                    db.Open();
                    var sqlQuery = "INSERT INTO [DcrMessage] ([DataHolderBrandId], [MessageId], [MessageState], [MessageError], [LastUpdated], [ClientId], [BrandName], [Created], [InfosecBaseUri]) VALUES (@dhBrandId, @msgId, @msgState, @msgErr, GETUTCDATE(), @clientId, @brandName, GETUTCDATE(), @infosecBaseUri)";
                    using var sqlCommand = new SqlCommand(sqlQuery, db);
                    sqlCommand.Parameters.AddWithValue("@dhBrandId", dcrMessage.DataHolderBrandId);
                    sqlCommand.Parameters.AddWithValue("@msgId", dcrMessage.MessageId);
                    sqlCommand.Parameters.AddWithValue("@msgState", dcrMessage.MessageState);
                    sqlCommand.Parameters.AddWithValue("@msgErr", string.IsNullOrEmpty(dcrMessage.MessageError) ? DBNull.Value : dcrMessage.MessageError);
                    sqlCommand.Parameters.AddWithValue("@clientId", string.IsNullOrEmpty(dcrMessage.ClientId) ? DBNull.Value : dcrMessage.ClientId);
                    sqlCommand.Parameters.AddWithValue("@brandName", string.IsNullOrEmpty(dcrMessage.BrandName) ? DBNull.Value : dcrMessage.BrandName);
                    sqlCommand.Parameters.AddWithValue("@infosecBaseUri", dcrMessage.InfosecBaseUri);
                    await sqlCommand.ExecuteNonQueryAsync();
                    db.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Update the DcrMessage MessageState and MessageError by DataHolderBrandId
        /// </summary>
        /// <param name="dcrMessage">The message object to be updated</param>
        /// <remarks>
        /// This is called from Azure Functions DCR
        /// </remarks>
        public async Task UpdateDcrMsgByDHBrandId(DcrMessage dcrMessage)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();

                var sqlQuery = "UPDATE [DcrMessage] SET [BrandName] = @brandName, [InfosecBaseUri] = @infosecBaseUri, [MessageState] = @msgState, [MessageError] = @msgErr, [LastUpdated] = GETUTCDATE(), [ClientId] = @clientId WHERE [DataHolderBrandId] = @id";
                                
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", dcrMessage.DataHolderBrandId);
                sqlCommand.Parameters.AddWithValue("@msgState", dcrMessage.MessageState);
                sqlCommand.Parameters.AddWithValue("@msgErr", string.IsNullOrEmpty(dcrMessage.MessageError) ? DBNull.Value : dcrMessage.MessageError);
                sqlCommand.Parameters.AddWithValue("@clientId", string.IsNullOrEmpty(dcrMessage.ClientId) ? DBNull.Value : dcrMessage.ClientId);
                sqlCommand.Parameters.AddWithValue("@brandName", string.IsNullOrEmpty(dcrMessage.BrandName) ? DBNull.Value : dcrMessage.BrandName);
                sqlCommand.Parameters.AddWithValue("@infosecBaseUri", string.IsNullOrEmpty(dcrMessage.InfosecBaseUri) ? DBNull.Value : dcrMessage.InfosecBaseUri);
                await sqlCommand.ExecuteNonQueryAsync();
                db.Close();
            }
        }

        /// <summary>
        /// Update the DcrMessage MessageState and MessageError by the Queue MessageId
        /// </summary>
        /// <param name="dcrMessage">The message object to be updated</param>
        /// msgState Abandoned brand name not available and can be ignored
        /// <remarks>
        /// This is called from Azure Functions DCR
        /// </remarks>
        public async Task UpdateDcrMsgByMessageId(DcrMessage dcrMessage)
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();                
                var sqlQuery = "UPDATE [DcrMessage] SET [MessageState] = @msgState, [MessageError] = @msgErr, [LastUpdated] = GETUTCDATE() WHERE [MessageId] = @id";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@id", dcrMessage.MessageId);
                sqlCommand.Parameters.AddWithValue("@msgState", dcrMessage.MessageState);
                sqlCommand.Parameters.AddWithValue("@msgErr", string.IsNullOrEmpty(dcrMessage.MessageError) ? DBNull.Value : dcrMessage.MessageError);
                await sqlCommand.ExecuteNonQueryAsync();
                db.Close();
            }
        }

        /// <summary>
        /// Update the DcrMessage MessageId (new added queue item id), MessageState and MessageError by the Queue MessageId
        /// </summary>
        /// <param name="dcrMessage">The message object to be updated</param>        
        /// This is called from Azure DCR Functions
        /// BrandName not available when DCR        
        /// </remarks>
        public async Task UpdateDcrMsgReplaceMessageIdWithoutBrand(DcrMessage dcrMessage, string replacementMsgId = "")
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlQuery = "UPDATE [DcrMessage] SET [MessageId] = @replaceMsgId, [MessageState] = @msgState, [MessageError] = @msgErr, [LastUpdated] = GETUTCDATE() WHERE [MessageId] = @id";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@replaceMsgId", replacementMsgId);
                sqlCommand.Parameters.AddWithValue("@msgState", dcrMessage.MessageState);
                sqlCommand.Parameters.AddWithValue("@msgErr", string.IsNullOrEmpty(dcrMessage.MessageError) ? DBNull.Value : dcrMessage.MessageError);
                sqlCommand.Parameters.AddWithValue("@id", dcrMessage.MessageId);
                await sqlCommand.ExecuteNonQueryAsync();
                db.Close();
            }
        }


        /// <summary>
        /// Update the DcrMessage MessageId (new added queue item id), MessageState and MessageError by the Queue MessageId
        /// </summary>
        /// <param name="dcrMessage">The message object to be updated</param>
        /// Update BrandName for Discovery Data Holder
        /// <remarks>
        /// This is called from Azure DiscoverDataHolders
        /// </remarks>
        public async Task UpdateDcrMsgReplaceMessageId(DcrMessage dcrMessage, string replacementMsgId = "")
        {
            using (SqlConnection db = new(_dbConn))
            {
                db.Open();
                var sqlQuery = "UPDATE [DcrMessage] SET [MessageId] = @replaceMsgId, [BrandName] = @brandName, [InfosecBaseUri] = @infosecBaseUri, [MessageState] = @msgState, [MessageError] = @msgErr, [LastUpdated] = GETUTCDATE() WHERE [MessageId] = @id";
                using var sqlCommand = new SqlCommand(sqlQuery, db);
                sqlCommand.Parameters.AddWithValue("@replaceMsgId", replacementMsgId);
                sqlCommand.Parameters.AddWithValue("@msgState", dcrMessage.MessageState);
                sqlCommand.Parameters.AddWithValue("@msgErr", string.IsNullOrEmpty(dcrMessage.MessageError) ? DBNull.Value : dcrMessage.MessageError);
                sqlCommand.Parameters.AddWithValue("@id", dcrMessage.MessageId);
                sqlCommand.Parameters.AddWithValue("@brandName", string.IsNullOrEmpty(dcrMessage.BrandName) ? DBNull.Value : dcrMessage.BrandName);
                sqlCommand.Parameters.AddWithValue("@infosecBaseUri", string.IsNullOrEmpty(dcrMessage.InfosecBaseUri) ? DBNull.Value : dcrMessage.InfosecBaseUri);                
                await sqlCommand.ExecuteNonQueryAsync();
                db.Close();
            }
        }

        #endregion

    }
}