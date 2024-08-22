using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CDR.DataRecipient.Repository.SQL;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Enum;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DiscoverDataHolders
{
    public class DiscoverDataHoldersFunction
    {
        private readonly ILogger _logger;
        private readonly DHOptions _options;

        private readonly X509Certificate2 _signCertificate;
        private readonly IHttpClientFactory _httpClientFactory;

        public DiscoverDataHoldersFunction(ILogger<DiscoverDataHoldersFunction> logger, IOptions<DHOptions> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _options = options.Value;

            byte[] clientCertBytes = Convert.FromBase64String(_options.Client_Certificate);
            X509Certificate2 clientCertificate = new(clientCertBytes, _options.Client_Certificate_Password, X509KeyStorageFlags.MachineKeySet);
            _logger.LogInformation("Client certificate loaded: {thumbprint}", clientCertificate.Thumbprint);

            _logger.LogInformation("Loading the signing certificate...");
            byte[] signCertBytes = Convert.FromBase64String(_options.Signing_Certificate);
            _signCertificate = new(signCertBytes, _options.Signing_Certificate_Password, X509KeyStorageFlags.MachineKeySet);
            _logger.LogInformation("Signing certificate loaded: {thumbprint}", _signCertificate.Thumbprint);
            _httpClientFactory=httpClientFactory;
        }

        /// <summary>
        /// Discover Data Holders Function
        /// </summary>
        /// <remarks>Gets the Data Holders from the Register and updates the local repository</remarks>
        [Function("DiscoverDataHolders")]
        public async Task DHBRANDS([TimerTrigger("%Schedule%")] TimerInfo myTimer)
        {
            try
            {
                _logger.LogInformation("Retrieving count for dynamicclientregistration queue...");
                int qCount = await GetQueueCountAsync(_options.StorageConnectionString, _options.QueueName);
                _logger.LogInformation("qCount = {qCount}", qCount);
                _logger.LogInformation("Loading the client certificate...");
                

                string msg = $"DHBRANDS";
                int inserted = 0;
                int updated = 0;
                int pendingReg = 0;

                Response<Token> tokenRes = await GetAccessToken();
                if (tokenRes.IsSuccessful)
                {
                    var dataHolderBrandsResult = await GetDataHolderBrands(tokenRes.Data.AccessToken);
                    if (dataHolderBrandsResult.statusCode == System.Net.HttpStatusCode.OK)
                    {
                        Response<IList<DataHolderBrand>> dhResponse = JsonConvert.DeserializeObject<Response<IList<DataHolderBrand>>>(dataHolderBrandsResult.body);
                        if (dhResponse.Data.Count == 0)
                        {
                            await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg}, Unable to get the DHBrands from: {_options.Register_Get_DH_Brands}, Ver: {_options.Register_Get_DH_Brands_XV}", "Error", "DHBRANDS");                            
                            return;
                        }

                        _logger.LogInformation("Found {count} data holder brands.", dhResponse.Data.Count);

                        // Sync data holder brands metadata.
                        await SynchroniseDataHolderBrandsMetadata(dhResponse.Data, _options.DataRecipient_DB_ConnectionString, _logger);

                        // RETURN a list of ALL DataHolderBrands that are NOT REGISTERED
                        (IList<DataHolderBrand> dhBrandsInsert, IList<DataHolderBrand> dhBrandsUpd) = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).CheckRegistrationsExist(dhResponse.Data);
                        _logger.LogInformation("{ins} data holder brands inserted. {upd} data holder brands updated.", dhBrandsInsert.Count, dhBrandsUpd.Count);

                        // UPDATE DataHolderBrands
                        if (dhBrandsUpd.Count > 0)
                        {
                            foreach (var dh in dhBrandsUpd)
                            {
                                bool result = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).CheckRegistrationExist(dh.DataHolderBrandId);
                                if (!result)
                                {
                                    var qMsgId = await AddQueueMessageAsync(_logger, _options.StorageConnectionString, _options.QueueName, dh.DataHolderBrandId, "UPDATE QUEUED");
                                    await AddDcrMessage(dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, qMsgId, "UPDATE DcrMessage");
                                    updated++;
                                }
                            }
                        }

                        // ALL DataHolderBrands that are TO REGISTER
                        if (dhBrandsInsert.Count > 0)
                        {
                            foreach (var dh in dhBrandsInsert)
                            {
                                // DOES the DcrMessage exist?
                                (string dcrMsgId, string dcrMsgState) = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).CheckDcrMessageExistByDHBrandId(dh.DataHolderBrandId);

                                // NO - DcrMessage DOES NOT EXIST in DcrMessage table
                                if (string.IsNullOrEmpty(dcrMsgId))
                                {
                                    qCount = await GetQueueCountAsync(_options.StorageConnectionString, _options.QueueName);
                                    var proc = (qCount == 0) ? "NO REG (ADD to EMPTY QUEUE)" : "NO REG (ADD to QUEUE)";

                                    // ADD to QUEUE and DcrMessage table
                                    var qMsgId = await AddQueueMessageAsync(_logger, _options.StorageConnectionString, _options.QueueName, dh.DataHolderBrandId, proc);                                    
                                    await AddDcrMessage(dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, qMsgId, "ADD to DcrMessage table");
                                    pendingReg++;
                                }

                                // YES - DcrMessage EXISTS in DcrMessage table
                                else
                                {
                                    qCount = await GetQueueCountAsync(_options.StorageConnectionString, _options.QueueName);
                                    var proc = (qCount == 0) ? "NO REG (ADD to EMPTY QUEUE)" : "NO REG (ADD to QUEUE)";

                                    if ((dcrMsgState.Equals(Message.Pending.ToString()) || dcrMsgState.Equals(Message.DCRFailed.ToString())))
                                    {
                                        Enum.TryParse(dcrMsgState, out Message dcrMsgStatus);

                                        // DcrMessage STATE = Pending -> ADD MESSAGE to the QUEUE
                                        var newMsgId = await AddQueueMessageAsync(_logger, _options.StorageConnectionString, _options.QueueName, dh.DataHolderBrandId, proc);

                                        // UPDATE EXISTING DcrMessage (with ADDED Queue MessageId)                                        
                                        await UpdateDcrMessage(dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, dcrMsgId, dcrMsgStatus, newMsgId, "Update DcrMessage table");
                                        pendingReg++;
                                    }
                                }
                            }
                        }

                        if (inserted == 0 && updated == 0 && pendingReg == 0)
                        {
                            msg += $" - no additional data holder brands added.";
                        }
                        else
                        {
                            if (inserted > 0)
                                msg += $" - {inserted} new data holder brands loaded.";

                            if (updated > 0)
                                msg += $" - {updated} existing data holder brands updated.";

                            if (pendingReg > 0)
                                msg += $" - {pendingReg} existing data holder brands queued to be registered.";
                        }

                        await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}", "Information", "DHBRANDS");

                        qCount = await GetQueueCountAsync(_options.StorageConnectionString, _options.QueueName);
                        msg = $"DHBRANDS - {qCount} items in {_options.QueueName} queue";
                        await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}", "Information", "DHBRANDS");
                    }
                    else
                    {
                        await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg}, Unable to get the DHBrands from: {_options.Register_Get_DH_Brands}, Ver: {_options.Register_Get_DH_Brands_XV}", "Error", "DHBRANDS");
                    }
                }
                else
                {
                    await InsertLog(_options.DataRecipient_DB_ConnectionString, $"Unable to get the Access Token for SoftwareProductId - {_options.Software_Product_Id} - at the endpoint - {_options.Register_Token_Endpoint}", "Error", "DHBRANDS");
                }
            }
            catch (Exception ex)
            {
                await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, "Exception Error", "Exception", "DHBRANDS", ex);
            }
        }

        /// <summary>
        /// Save the latest data holder brand metadata to the DataHolderBrand table.
        /// </summary>
        /// <param name="data"></param>
        private async static Task SynchroniseDataHolderBrandsMetadata(IList<DataHolderBrand> data, string DataRecipient_DB_ConnectionString, ILogger log)
        {
            log.LogInformation("Synchronising {count} data holder brands", data.Count);

            var sql = new SqlDataAccess(DataRecipient_DB_ConnectionString);
            var existingBrands = await sql.GetDataHolderBrands();

            if (existingBrands == null)
            {
                log.LogInformation("No existing data holder brands are found");
                return;
            }

            foreach (var latestBrand in data)
            {
                var exists = existingBrands.Any(x => x.DataHolderBrandId.Equals(latestBrand.DataHolderBrandId, StringComparison.OrdinalIgnoreCase));

                // Insert new brand.
                if (!exists)
                {
                    log.LogInformation("Inserting new data holder brand: {brandId}", latestBrand.DataHolderBrandId);
                    await sql.InsertDataHolder(latestBrand);
                }
                else
                {
                    // Update brand.
                    log.LogInformation("Updating existing data holder brand: {brandId}", latestBrand.DataHolderBrandId);
                    await sql.UpdateDataHolder(latestBrand);
                }
            }

            log.LogInformation("Synchronising existing {count} data holder brands", existingBrands.Count());
            foreach (var existingDataHolderBrandId in existingBrands.Select(brand => brand.DataHolderBrandId))
            {
                //existing data holders that don't exist in mdh should be removed from the mdr
                var exists = data.Any(x => x.DataHolderBrandId.Equals(existingDataHolderBrandId, StringComparison.OrdinalIgnoreCase));

                //Remove additional or extra brands to reflect correct brand data
                if (!exists)
                {
                    log.LogInformation("Deleting existing data holder brand: {brandId}", existingDataHolderBrandId);
                    await sql.DeleteDataHolder(existingDataHolderBrandId);
                }
            }
        }

        /// <summary>
        /// Get Access Token
        /// </summary>
        /// <returns>JWT</returns>
        private async Task<Response<Token>> GetAccessToken()
        {
            // Setup the http client.
            var client = GetHttpClient();

            // Make the request to the token endpoint.
            _logger.LogInformation("Retrieving access_token from the Register: {tokenEndpoint}", _options.Register_Token_Endpoint);
            var response = await client.SendPrivateKeyJwtRequest(
                _options.Register_Token_Endpoint,
                _signCertificate,
                _options.Software_Product_Id,
                _options.Software_Product_Id,
                scope: Constants.Scopes.CDR_REGISTER,
                grantType: Constants.GrantTypes.CLIENT_CREDENTIALS);

            var body = await response.Content.ReadAsStringAsync();
            var tokenResponse = new Response<Token>()
            {
                StatusCode = response.StatusCode
            };

            _logger.LogInformation("Register response: {statusCode} - {body}", tokenResponse.StatusCode, body);

            if (response.IsSuccessStatusCode)
                tokenResponse.Data = JsonConvert.DeserializeObject<Token>(body);
            else
                tokenResponse.Message = body;

            return tokenResponse;
        }

        /// <summary>
        /// Get the Data Holder Brands from the Register
        /// </summary>
        /// <returns>Raw data</returns>
        private async Task<(string body, System.Net.HttpStatusCode statusCode, string reason)> GetDataHolderBrands(string accessToken)
        {
            // NB: THE MAX VALID PAGE SIZE in MDH is 1000
            var dhBrandsEndpoint = _options.Register_Get_DH_Brands.AppendQueryString("page-size", "1000");

            // Setup the http client.
            var client = GetHttpClient(accessToken, _options.Register_Get_DH_Brands_XV);

            // Make the request to the get data holder brands endpoint.
            _logger.LogInformation("Retrieving data holder brands from the Register: {dhBrandsEndpoint}", dhBrandsEndpoint);
            var response = await client.GetAsync(dhBrandsEndpoint);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Register response: {statusCode} - {body}", response.StatusCode, body);

            return (body, response.StatusCode, response.ReasonPhrase.ToString());
        }

        private HttpClient GetHttpClient(string accessToken = null, string version = null)
        {
            var httpClient = _httpClientFactory.CreateClient(DiscoverDHConstants.DHHttpClientName);

            // If an access token has been provided then add to the Authorization header of the client.
            if (!string.IsNullOrEmpty(accessToken))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Add the x-v header to the request if provided.
            if (!string.IsNullOrEmpty(version))
                httpClient.DefaultRequestHeaders.Add("x-v", version);

            return httpClient;
        }

        /// <summary>
        /// Insert the Message into the Queue
        /// </summary>
        /// <returns>Message Id</returns>
        private static async Task<string> AddQueueMessageAsync(ILogger log, string StorageConnectionString, string qName, string dhBrandId, string proc)
        {
            QueueClientOptions options = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            QueueClient qClient = new(StorageConnectionString, qName, options);
            await qClient.CreateIfNotExistsAsync();

            DcrQueueMessage qMsg = new()
            {
                MessageVersion = "1.0",
                DataHolderBrandId = dhBrandId
            };
            string qMessage = JsonConvert.SerializeObject(qMsg);
            var msgReceipt = await qClient.SendMessageAsync(qMessage);

            // Format console logging message layout to aid with readability
            do
            {
                proc += " ";
            } while (proc.Length < 30);
            log.LogInformation("{proc}- dhBrandId: {dhBrandId}, MessageId: {messageId}", proc, dhBrandId, msgReceipt.Value.MessageId);

            return msgReceipt.Value.MessageId;
        }

        /// <summary>
        /// Insert into the DcrMessage table
        /// </summary>
        /// <returns>Message Id</returns>
        private async Task AddDcrMessage(string dhBrandId, string brandName, string infosecBaseUri, string msgId, string proc)
        {
            DcrMessage dcrMsg = new()
            {
                DataHolderBrandId = new Guid(dhBrandId),
                BrandName = brandName,
                InfosecBaseUri = infosecBaseUri,
                MessageId = msgId,
                MessageState = Message.Pending.ToString()
            };
            await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).InsertDcrMessage(dcrMsg);

            // Format console logging message layout to aid with readability
            do
            {
                proc += " ";
            } while (proc.Length < 30);
            _logger.LogInformation("{proc}- dhBrandId: {dhBrandId}, brandName: {brandName}, MessageId: {msgId}", proc, dhBrandId, brandName, msgId);
        }

        /// <summary>
        /// Update the DcrMessage table
        /// </summary>
        /// <returns>Message Id</returns>
        private async Task UpdateDcrMessage(string dhBrandId, string brandName, string infosecBaseUri, string msgId, Message messageState, string newMsgId, string proc)
        {
            DcrMessage dcrMsg = new()
            {
                DataHolderBrandId = new Guid(dhBrandId),
                BrandName = brandName,
                InfosecBaseUri = infosecBaseUri,
                MessageId = msgId,
                MessageState = messageState.ToString()
            };
            await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgReplaceMessageId(dcrMsg, newMsgId);

            // Format console logging message layout to aid with readability
            do
            {
                proc += " ";
            } while (proc.Length < 30);
            _logger.LogInformation("{proc}- dhBrandId: {dhBrandId}, brandName: {brandName}, MessageId: {msgId}", proc, dhBrandId, brandName, msgId);
        }

        /// <summary>
        /// Queue Item Count
        /// </summary>
        private static async Task<int> GetQueueCountAsync(string storageConnectionString, string qName)
        {
            QueueClient queueClient = new(storageConnectionString, qName);
            if (queueClient.Exists())
            {
                QueueProperties properties = await queueClient.GetPropertiesAsync();
                return properties.ApproximateMessagesCount;
            }
            return 0;
        }

        /// <summary>
        /// Update the Log table
        /// </summary>
        private async Task InsertLog(string dataRecipient_DB_ConnectionString, string msg, string lvl, string methodName, Exception ex = null)
        {
            string exMessage = "";
           

            if (ex != null)
            {
                Exception innerException = ex;
                StringBuilder innerMsg = new();
                int ctr = 0;

                do
                {
                    // skip the first inner exeception message as it is the same as the exception message
                    if (ctr > 0)
                    {
                        innerMsg.Append(string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                        innerMsg.Append("\r\n");
                    }
                    else
                    {
                        ctr++;
                    }

                    innerException = innerException.InnerException;
                }
                while (innerException != null);

                // USE the EXCEPTION MESSAGE
                if (innerMsg.Length == 0)
                    exMessage = ex.Message;

                // USE the INNER EXCEPTION MESSAGE (INCLUDES the EXCEPTION MESSAGE)	
                else
                    exMessage = innerMsg.ToString();

                exMessage = exMessage.Replace("'", "");

                _logger.LogError("{message}", exMessage);
            }
            else
            {
                _logger.LogInformation("{message}", msg);
            }

            using SqlConnection db = new(dataRecipient_DB_ConnectionString);
            db.Open();
            var cmdText = "";

            if (string.IsNullOrEmpty(exMessage))
                cmdText = $"INSERT INTO [LogEventsDcrService] ([Message], [Level], [TimeStamp], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(),@procName,@methodName,@srcContext)";
            else
                cmdText = $"INSERT INTO [LogEventsDcrService] ([Message], [Level], [TimeStamp], [Exception], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(), @exMessage,@procName,@methodName,@srcContext)";

            using var cmd = new SqlCommand(cmdText, db);
            cmd.Parameters.AddWithValue("@msg", msg);
            cmd.Parameters.AddWithValue("@lvl", lvl);
            cmd.Parameters.AddWithValue("@exMessage", exMessage);
            cmd.Parameters.AddWithValue("@procName", "Azure Function");
            cmd.Parameters.AddWithValue("@methodName", methodName);
            cmd.Parameters.AddWithValue("@srcContext", "CDR.DiscoverDataHolders");
            await cmd.ExecuteNonQueryAsync();
            db.Close();
        }
    }
}