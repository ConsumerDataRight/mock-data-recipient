using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CDR.DataRecipient.Repository.SQL;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Enum;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DiscoverDataHolders
{
    public static class DiscoverDataHoldersFunction
    {
        /// <summary>
        /// Discover Data Holders Function
        /// </summary>
        /// <remarks>Gets the Data Holders from the Register and updates the local repository</remarks>
        [FunctionName("DiscoverDataHolders")]
        public static async Task DHBRANDS([TimerTrigger("%Schedule%")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            try
            {
                var isLocalDev = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT").Equals("Development");
                var configBuilder = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory);

                if (isLocalDev)
                {
                    configBuilder = configBuilder.AddJsonFile("local.settings.json", optional: false, reloadOnChange: true);
                }

                var config = configBuilder.AddEnvironmentVariables().Build();

                // Get environment variables.
                string qConnString = Environment.GetEnvironmentVariable("StorageConnectionString");
                string dbConnString = Environment.GetEnvironmentVariable("DataRecipient_DB_ConnectionString");
                string dbLoggingConnString = Environment.GetEnvironmentVariable("DataRecipient_Logging_DB_ConnectionString");
                string tokenEndpoint = Environment.GetEnvironmentVariable("Register_Token_Endpoint");
                string dhBrandsEndpoint = Environment.GetEnvironmentVariable("Register_Get_DH_Brands");
                string xvVer = Environment.GetEnvironmentVariable("Register_Get_DH_Brands_XV");
                string softwareProductId = Environment.GetEnvironmentVariable("Software_Product_Id");
                string clientCert = Environment.GetEnvironmentVariable("Client_Certificate");
                string clientCertPwd = Environment.GetEnvironmentVariable("Client_Certificate_Password");
                string signCert = Environment.GetEnvironmentVariable("Signing_Certificate");
                string signCertPwd = Environment.GetEnvironmentVariable("Signing_Certificate_Password");
                bool ignoreServerCertificateErrors = Environment.GetEnvironmentVariable("Ignore_Server_Certificate_Errors").Equals("true", StringComparison.OrdinalIgnoreCase);

                // DCR queue.
                log.LogInformation("Retrieving count for dynamicclientregistration queue...");
                var qName = "dynamicclientregistration";
                int qCount = await GetQueueCountAsync(qConnString, qName);
                log.LogInformation("qCount = {qCount}", qCount);

                // Client certificate.
                log.LogInformation("Loading the client certificate...");
                byte[] clientCertBytes = Convert.FromBase64String(clientCert);
                X509Certificate2 clientCertificate = new(clientCertBytes, clientCertPwd, X509KeyStorageFlags.MachineKeySet);
                log.LogInformation("Client certificate loaded: {thumbprint}", clientCertificate.Thumbprint);

                // Signing certificate.
                log.LogInformation("Loading the signing certificate...");
                byte[] signCertBytes = Convert.FromBase64String(signCert);
                X509Certificate2 signCertificate = new(signCertBytes, signCertPwd, X509KeyStorageFlags.MachineKeySet);
                log.LogInformation("Signing certificate loaded: {thumbprint}", signCertificate.Thumbprint);

                string msg = $"DHBRANDS";
                int inserted = 0;
                int updated = 0;
                int pendingReg = 0;

                // TESTING USE ONLY
                //await DeleteAllMessagesAsync(log, qConnString, qName);

                Response<Token> tokenRes = await GetAccessToken(tokenEndpoint, softwareProductId, clientCertificate, signCertificate, log, ignoreServerCertificateErrors: ignoreServerCertificateErrors);
                if (tokenRes.IsSuccessful)
                {
                    (string respBody, System.Net.HttpStatusCode statusCode, string reason) = await GetDataHolderBrands(dhBrandsEndpoint, xvVer, tokenRes.Data.AccessToken, clientCertificate, log, ignoreServerCertificateErrors: ignoreServerCertificateErrors);
                    if (statusCode == System.Net.HttpStatusCode.OK)
                    {
                        Response<IList<DataHolderBrand>> dhResponse = JsonConvert.DeserializeObject<Response<IList<DataHolderBrand>>>(respBody);
                        if (dhResponse.Data.Count == 0)
                        {
                            await InsertLog(log, dbConnString, $"{msg}, Unable to get the DHBrands from: {dhBrandsEndpoint}, Ver: {xvVer}", "Error", "DHBRANDS");
                            return;
                        }

                        log.LogInformation("Found {count} data holder brands.", dhResponse.Data.Count);

                        // Sync data holder brands metadata.
                        await SynchroniseDataHolderBrandsMetadata(dhResponse.Data, dbConnString, log);

                        // RETURN a list of ALL DataHolderBrands that are NOT REGISTERED
                        (IList<DataHolderBrand> dhBrandsInsert, IList<DataHolderBrand> dhBrandsUpd) = await new SqlDataAccess(dbConnString).CheckRegistrationsExist(dhResponse.Data);

                        log.LogInformation("{ins} data holder brands inserted. {upd} data holder brands updated.", dhBrandsInsert.Count, dhBrandsUpd.Count);

                        // UPDATE DataHolderBrands
                        if (dhBrandsUpd.Count > 0)
                        {
                            foreach (var dh in dhBrandsUpd)
                            {
                                bool result = await new SqlDataAccess(dbConnString).CheckRegistrationExist(dh.DataHolderBrandId);
                                if (!result)
                                {
                                    var qMsgId = await AddQueueMessageAsync(log, qConnString, qName, dh.DataHolderBrandId, "UPDATE QUEUED");
                                    await AddDcrMessage(log, dbConnString, dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, qMsgId, "UPDATE DcrMessage");
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
                                (string dcrMsgId, string dcrMsgState) = await new SqlDataAccess(dbConnString).CheckDcrMessageExistByDHBrandId(dh.DataHolderBrandId);

                                // NO - DcrMessage DOES NOT EXIST in local repo
                                if (string.IsNullOrEmpty(dcrMsgId))
                                {
                                    qCount = await GetQueueCountAsync(qConnString, qName);
                                    if (qCount == 0)
                                    {
                                        // ADD to EMPTY QUEUE
                                        var qMsgId = await AddQueueMessageAsync(log, qConnString, qName, dh.DataHolderBrandId, "NO REG (ADD to EMPTY QUEUE)");
                                        await AddDcrMessage(log, dbConnString, dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, qMsgId, "ADD to DcrMessage table");
                                        pendingReg++;
                                    }

                                    // CAN ONLY PEEK AT LAST 32 MESSAGES
                                    else if (qCount < 33)
                                    {
                                        var ifExist = await IsMessageInQueue(dcrMsgId, qConnString, qName);
                                        if (!ifExist)
                                        {
                                            // ADD to QUEUE and DcrMessage table
                                            var qMsgId = await AddQueueMessageAsync(log, qConnString, qName, dh.DataHolderBrandId, "NO REG (ADD to QUEUE)");
                                            await AddDcrMessage(log, dbConnString, dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, qMsgId, "ADD to DcrMessage table");
                                            pendingReg++;
                                        }
                                    }
                                }

                                // YES - DcrMessage EXISTS in the local repo
                                else
                                {
                                    qCount = await GetQueueCountAsync(qConnString, qName);
                                    if (qCount == 0)
                                    {
                                        // ADD to EMPTY QUEUE
                                        var newMsgId = await AddQueueMessageAsync(log, qConnString, qName, dh.DataHolderBrandId, "NO REG (ADD to EMPTY QUEUE)");
                                        await UpdateDcrMessage(log, dbConnString, dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, dcrMsgId, MessageEnum.Pending, newMsgId, "UPDATE DcrMessage table");
                                        pendingReg++;
                                    }

                                    // CAN ONLY PEEK AT LAST 32 MESSAGES
                                    else if (qCount < 33)
                                    {
                                        var ifExist = await IsMessageInQueue(dcrMsgId, qConnString, qName);
                                        if (!ifExist && (dcrMsgState.Equals(MessageEnum.Pending.ToString()) || dcrMsgState.Equals(MessageEnum.DCRFailed.ToString())) )
                                        {
                                            Enum.TryParse(dcrMsgState, out MessageEnum dcrMsgStatus);

                                            // DcrMessage STATE = Pending -> ADD MESSAGE to the QUEUE
                                            var newMsgId = await AddQueueMessageAsync(log, qConnString, qName, dh.DataHolderBrandId, "NO REG (ADD to QUEUE)");

                                            // UPDATE EXISTING DcrMessage (with ADDED Queue MessageId)
                                            await UpdateDcrMessage(log, dbConnString, dh.DataHolderBrandId, dh.BrandName, dh.EndpointDetail.InfoSecBaseUri, dcrMsgId, dcrMsgStatus, newMsgId, "Update DcrMessage table");
                                            pendingReg++;
                                        }
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

                        await InsertLog(log, dbLoggingConnString, $"{msg}", "Information", "DHBRANDS");

                        qCount = await GetQueueCountAsync(qConnString, qName);
                        msg = $"DHBRANDS - {qCount} items in {qName} queue";
                        await InsertLog(log, dbLoggingConnString, $"{msg}", "Information", "DHBRANDS");
                    }
                    else
                    {
                        await InsertLog(log, dbConnString, $"{msg}, Unable to get the DHBrands from: {dhBrandsEndpoint}, Ver: {xvVer}", "Error", "DHBRANDS");
                    }
                }
                else
                {
                    await InsertLog(log, dbConnString, $"Unable to get the Access Token for SoftwareProductId - {softwareProductId} - at the endpoint - {tokenEndpoint}", "Error", "DHBRANDS");
                }
            }
            catch (Exception ex)
            {
                await InsertLog(log, Environment.GetEnvironmentVariable("DataRecipient_Logging_DB_ConnectionString"), "Exception Error", "Exception", "DHBRANDS", ex);
            }
        }

        /// <summary>
        /// Save the latest data holder brand metadata to the DataHolderBrand table.
        /// </summary>
        /// <param name="data"></param>
        private async static Task SynchroniseDataHolderBrandsMetadata(IList<DataHolderBrand> data, string dbConnString, ILogger log)
        {
            log.LogInformation("Synchronising {count} data holder brands", data.Count);

            var sql = new SqlDataAccess(dbConnString);
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

            log.LogInformation("Synchronising existing {count} data holder brands", existingBrands?.Count());
            
            foreach (var existingDataHolderBrand in existingBrands)
            {
                //existing data holders that don't exist in mdh should be removed from the mdr
                var exists = data.Any(x => x.DataHolderBrandId.Equals(existingDataHolderBrand.DataHolderBrandId, StringComparison.OrdinalIgnoreCase));

                //Remove additional or extra brands to reflect correct brand data
                if (!exists)
                {
                    log.LogInformation("Deleting existing data holder brand: {brandId}", existingDataHolderBrand.DataHolderBrandId);
                    await sql.DeleteDataHolder(existingDataHolderBrand.DataHolderBrandId);
                }
            }            
        }

        /// <summary>
        /// Get Access Token
        /// </summary>
        /// <returns>JWT</returns>
        private static async Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            // Make the request to the token endpoint.
            log.LogInformation("Retrieving access_token from the Register: {tokenEndpoint}", tokenEndpoint);
            var response = await client.SendPrivateKeyJwtRequest(
                tokenEndpoint,
                signingCertificate,
                clientId,
                clientId,
                scope: Constants.Scopes.CDR_REGISTER,
                grantType: Constants.GrantTypes.CLIENT_CREDENTIALS);

            var body = await response.Content.ReadAsStringAsync();
            var tokenResponse = new Response<Token>()
            {
                StatusCode = response.StatusCode
            };

            log.LogInformation("Register response: {statusCode} - {body}", tokenResponse.StatusCode, body);

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
        private static async Task<(string, System.Net.HttpStatusCode, string)> GetDataHolderBrands(
            string dhBrandsEndpoint,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            // NB: THE MAX VALID PAGE SIZE in MDH is 1000
            dhBrandsEndpoint = dhBrandsEndpoint.AppendQueryString("page-size", "1000");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken, version, ignoreServerCertificateErrors);

            // Make the request to the get data holder brands endpoint.
            log.LogInformation("Retrieving data holder brands from the Register: {dhBrandsEndpoint}", dhBrandsEndpoint);
            var response = await client.GetAsync(dhBrandsEndpoint);
            var body = await response.Content.ReadAsStringAsync();
            log.LogInformation("Register response: {statusCode} - {body}", response.StatusCode, body);

            return (body, response.StatusCode, response.ReasonPhrase.ToString());
        }

        private static HttpClient GetHttpClient(
            X509Certificate2 clientCertificate = null,
            string accessToken = null,
            string version = null,
            bool ignoreServerCertificateErrors = false)
        {
            var clientHandler = new HttpClientHandler();

            // Set the client certificate for the connection if supplied.
            if (clientCertificate != null)
            {
                clientHandler.ClientCertificates.Add(clientCertificate);
            }

            if (ignoreServerCertificateErrors)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(clientHandler);

            // If an access token has been provided then add to the Authorization header of the client.
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            // Add the x-v header to the request if provided.
            if (!string.IsNullOrEmpty(version))
            {
                client.DefaultRequestHeaders.Add("x-v", version);
            }

            return client;
        }

        /// <summary>
        /// Insert the Message into the Queue
        /// </summary>
        /// <returns>Message Id</returns>
        private static async Task<string> AddQueueMessageAsync(ILogger log, string qConnString, string qName, string dhBrandId, string proc)
        {
            QueueClientOptions options = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            QueueClient qClient = new(qConnString, qName, options);
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
            log.LogInformation($"{proc}- dhBrandId: {dhBrandId}, MessageId: {msgReceipt.Value.MessageId}");

            return msgReceipt.Value.MessageId;
        }

        /// <summary>
        /// Insert into the DcrMessage table
        /// </summary>
        /// <returns>Message Id</returns>
        private static async Task AddDcrMessage(ILogger log, string dbConnString, string dhBrandId, string brandName, string infosecBaseUri, string msgId, string proc)
        {
            DcrMessage dcrMsg = new()
            {
                DataHolderBrandId = new Guid(dhBrandId),
                BrandName = brandName,
                InfosecBaseUri = infosecBaseUri,
                MessageId = msgId,
                MessageState = MessageEnum.Pending.ToString()
            };
            await new SqlDataAccess(dbConnString).InsertDcrMessage(dcrMsg);

            // Format console logging message layout to aid with readability
            do
            {
                proc += " ";
            } while (proc.Length < 30);
            log.LogInformation($"{proc}- dhBrandId: {dhBrandId}, brandName: {brandName}, MessageId: {msgId}");
        }

        /// <summary>
        /// Update the DcrMessage table
        /// </summary>
        /// <returns>Message Id</returns>
        private static async Task UpdateDcrMessage(ILogger log, string dbConnString, string dhBrandId, string brandName, string infosecBaseUri, string msgId, MessageEnum messageState, string newMsgId, string proc)
        {
            DcrMessage dcrMsg = new()
            {
                DataHolderBrandId = new Guid(dhBrandId),
                BrandName = brandName,
                InfosecBaseUri = infosecBaseUri,
                MessageId = msgId,                
                MessageState = messageState.ToString()
            };
            await new SqlDataAccess(dbConnString).UpdateDcrMsgReplaceMessageId(dcrMsg, newMsgId);

            // Format console logging message layout to aid with readability
            do
            {
                proc += " ";
            } while (proc.Length < 30);
            log.LogInformation($"{proc}- dhBrandId: {dhBrandId}, brandName: {brandName}, MessageId: {msgId}");
        }

        /// <summary>
        /// Does the DcrMessage exist in the Queue?
        /// </summary>
        /// <returns>[true|false]</returns>
        private static async Task<bool> IsMessageInQueue(string msgId, string qConnString, string qName)
        {
            QueueClient qClient = new(qConnString, qName);
            PeekedMessage[] messages = await qClient.PeekMessagesAsync(32);
            foreach (PeekedMessage message in messages)
            {
                if (message.MessageId.Equals(msgId))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// TESTING USE ONLY - DELETE all queue items
        /// </summary>
        private static async Task DeleteAllMessagesAsync(ILogger log, string qConnString, string qName)
        {
            QueueClient qClient = new(qConnString, qName);
            if (qClient.Exists())
            {
                await qClient.DeleteAsync();
                int qCount = await GetQueueCountAsync(qConnString, qName);
                log.LogInformation($"{qCount} items deleted in {qName} queue");
            }
        }

        /// <summary>
        /// Queue Item Count
        /// </summary>
        private static async Task<int> GetQueueCountAsync(string qConnString, string qName)
        {
            QueueClient queueClient = new(qConnString, qName);
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
        private static async Task InsertLog(ILogger log, string dbConnString, string msg, string lvl, string methodName, Exception exMsg = null)
        {
            log.LogInformation($"{msg}");

            string exMessage = "";

            if (exMsg != null)
            {
                Exception innerException = exMsg;
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
                    exMessage = exMsg.Message;

                // USE the INNER EXCEPTION MESSAGE (INCLUDES the EXCEPTION MESSAGE)	
                else
                    exMessage = innerMsg.ToString();

                exMessage = exMessage.Replace("'", "");
            }

            using (SqlConnection db = new(dbConnString))
            {
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
}