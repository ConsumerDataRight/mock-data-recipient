using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CDR.DataRecipient.Repository.SQL;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Enum;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DCR
{
    public static class DynamicClientRegistrationFunction
    {
        /// <summary>
        /// Dynamic Client Registration Function
        /// </summary>
        /// <remarks>Registers the Data Holders in the messaging queue and updates the local repository</remarks>
        [FunctionName("FunctionDCR")]
        public static async Task DCR([QueueTrigger("dynamicclientregistration", Connection = "StorageConnectionString")] CloudQueueMessage myQueueItem, ILogger log, ExecutionContext context)
        {
            string msg = string.Empty;
            string dataHolderBrandName = string.Empty;
            string infosecBaseUri = string.Empty;
            string regEndpoint = string.Empty;

            DcrQueueMessage myQMsg = JsonConvert.DeserializeObject<DcrQueueMessage>(myQueueItem.AsString);
                        
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
                string ssaEndpoint = Environment.GetEnvironmentVariable("Register_Get_SSA_Endpoint");
                string xv = Environment.GetEnvironmentVariable("Register_Get_SSA_XV");
                string brandId = Environment.GetEnvironmentVariable("Brand_Id");
                string softwareProductId = Environment.GetEnvironmentVariable("Software_Product_Id");
                string redirectUri = Environment.GetEnvironmentVariable("Redirect_Uri");
                string clientCert = Environment.GetEnvironmentVariable("Client_Certificate");
                string clientCertPwd = Environment.GetEnvironmentVariable("Client_Certificate_Password");
                string signCert = Environment.GetEnvironmentVariable("Signing_Certificate");
                string signCertPwd = Environment.GetEnvironmentVariable("Signing_Certificate_Password");
                var retries = Convert.ToInt16(Environment.GetEnvironmentVariable("Retries"));
                bool ignoreServerCertificateErrors = Environment.GetEnvironmentVariable("Ignore_Server_Certificate_Errors").Equals("true", StringComparison.OrdinalIgnoreCase);

                // DCR queue.
                log.LogInformation("Retrieving count for dynamicclientregistration queue...");
                string qName = "dynamicclientregistration";
                int qCount = await GetQueueCountAsync(qConnString, qName);
                log.LogInformation($"qCount = {qCount}");

                if (string.IsNullOrEmpty(myQMsg.DataHolderBrandId))
                {
                    // Add messsage to deadletter queue
                    await AddDeadLetterQueMsgAsync(log, dbConnString, qConnString, myQMsg.DataHolderBrandId, myQueueItem, "deadletter");
                }
                else
                {
                    msg = $"DHBrandId - {myQMsg.DataHolderBrandId}";

                    log.LogInformation("Loading the client certificate...");
                    byte[] clientCertBytes = Convert.FromBase64String(clientCert);
                    X509Certificate2 clientCertificate = new(clientCertBytes, clientCertPwd, X509KeyStorageFlags.MachineKeySet);
                    log.LogInformation("Client certificate loaded: {thumbprint}", clientCertificate.Thumbprint);

                    log.LogInformation("Loading the signing certificate...");
                    byte[] signCertBytes = Convert.FromBase64String(signCert);
                    X509Certificate2 signCertificate = new(signCertBytes, signCertPwd, X509KeyStorageFlags.MachineKeySet);
                    log.LogInformation("Signing certificate loaded: {thumbprint}", signCertificate.Thumbprint);

                    Response<Token> tokenResponse = await GetAccessToken(tokenEndpoint, softwareProductId, clientCertificate, signCertificate, log, ignoreServerCertificateErrors);
                    if (tokenResponse.IsSuccessful)
                    {
                        var ssa = await GetSoftwareStatementAssertion(ssaEndpoint, xv, tokenResponse.Data.AccessToken, clientCertificate, brandId, softwareProductId, log, ignoreServerCertificateErrors);
                        if (ssa.IsSuccessful)
                        {                            
                            //DOES the Data Holder Brand EXIST in the REPO?
                            DataHolderBrand dh = await new SqlDataAccess(dbConnString).GetDataHolderBrand(myQMsg.DataHolderBrandId);
                            if (dh == null)
                            {
                                // NO - DOES the DcrMessage exist?
                                (string dcrMsgId, string dcrMsgState) = await new SqlDataAccess(dbConnString).CheckDcrMessageExistByDHBrandId(myQMsg.DataHolderBrandId);
                                if (!string.IsNullOrEmpty(dcrMsgId))
                                {
                                    // YES - UPDATE EXISTING DcrMessage (with ADDED Queue MessageId, Failed STATE and ERROR)
                                    DcrMessage dcrMsg = new()
                                    {
                                        DataHolderBrandId = Guid.Empty,
                                        MessageId = dcrMsgId,
                                        MessageState = MessageEnum.DCRFailed.ToString(),
                                        MessageError = $"{msg} - does not exist in the repo"
                                    };
                                    await new SqlDataAccess(dbConnString).UpdateDcrMsgReplaceMessageIdWithoutBrand(dcrMsg, myQueueItem.Id);
                                }
                                await InsertLog(log, dbConnString, $"{msg} - does not exist in the repo", "Error", "DCR");
                            }
                            else
                            {
                                dataHolderBrandName = dh.BrandName;                                
                                // YES - DOES a Registration already exist for the DataHolderBrandId in the local repo?
                                Guid clientId = await new SqlDataAccess(dbConnString).GetRegByDHBrandId(dh.DataHolderBrandId);
                                if (clientId == Guid.Empty)
                                {
                                    // NO - register this Data Holder Brand
                                    infosecBaseUri = dh.EndpointDetail.InfoSecBaseUri;
                                    var oidcDiscovery = (await GetOidcDiscovery(infosecBaseUri, ignoreServerCertificateErrors: ignoreServerCertificateErrors)).Data;
                                    if (oidcDiscovery != null)
                                    {
                                        regEndpoint = oidcDiscovery.RegistrationEndpoint;
                                        var dcrRequestJwt = PopulateDCRRequestJwt(softwareProductId, redirectUri, ssa.Data, oidcDiscovery.Issuer, signCertificate);

                                        // Process Registration - retry 3 times
                                        bool regSuccess = false;
                                        string regStatusCode = "";
                                        string regMessage = "";
                                        string regClientId = "";
                                        string jsonDoc = "";
                                        do
                                        {
                                            var dcrResponse = await Register(regEndpoint, clientCertificate, dcrRequestJwt, ignoreServerCertificateErrors: ignoreServerCertificateErrors);
                                            if (dcrResponse.IsSuccessful)
                                            {
                                                regSuccess = true;
                                                regClientId = dcrResponse.Data.ClientId;
                                                dcrResponse.Data.DataHolderBrandId = dh.DataHolderBrandId;
                                                jsonDoc = System.Text.Json.JsonSerializer.Serialize(dcrResponse.Data);
                                            }
                                            else
                                            {
                                                regStatusCode = dcrResponse.StatusCode.ToString();
                                                regMessage = dcrResponse.Message;
                                                retries--;
                                            }
                                        } while (!regSuccess && retries > 0);

                                        // Successful -> Update DcrMessage and Insert into Data Holder Registration repo
                                        if (regSuccess)
                                        {
                                            DcrMessage dcrMsg = new()
                                            {
                                                ClientId = regClientId,
                                                DataHolderBrandId = new Guid(dh.DataHolderBrandId),
                                                BrandName = dh.BrandName,
                                                InfosecBaseUri = infosecBaseUri,
                                                MessageState = MessageEnum.DCRComplete.ToString()
                                            };
                                            await new SqlDataAccess(dbConnString).UpdateDcrMsgByDHBrandId(dcrMsg);

                                            var dcrInserted = await new SqlDataAccess(dbConnString).InsertDcrRegistration(regClientId, dh.DataHolderBrandId, jsonDoc);
                                            if (dcrInserted)
                                                await InsertLog(log, dbConnString, $"{msg}, REGISTERED as ClientId - {regClientId}, {qCount - 1} items remain in queue, ", "Information", "DCR");
                                            else
                                                await InsertLog(log, dbConnString, $"{msg}, REGISTERED as ClientId - {regClientId}, {qCount - 1} items remain in queue - FAILED to add to MDR REPO", "Error", "DCR");
                                        }

                                        // FAILED -> Update DcrMessage as DCRFailed
                                        else
                                        {
                                            regMessage = regMessage.Replace("'", "");
                                            DcrMessage dcrMsg = new()
                                            {
                                                DataHolderBrandId = new Guid(dh.DataHolderBrandId),
                                                BrandName = dh.BrandName,
                                                InfosecBaseUri = infosecBaseUri,
                                                MessageState = MessageEnum.DCRFailed.ToString(),
                                                MessageError = $"StatusCode: {regStatusCode}, {regMessage}"
                                            };
                                            await new SqlDataAccess(dbConnString).UpdateDcrMsgByDHBrandId(dcrMsg);
                                            await InsertLog(log, dbConnString, $"{msg}, REGISTRATION FAILED, StatusCode: {regStatusCode}, {regMessage}", "Error", "DCR");
                                        }
                                    }
                                    else
                                    {
                                        // Oidc Discovery failed
                                        DcrMessage dcrMsg = new()
                                        {
                                            DataHolderBrandId = new Guid(myQMsg.DataHolderBrandId),
                                            BrandName = dh.BrandName,
                                            InfosecBaseUri = infosecBaseUri,
                                            MessageState = MessageEnum.DCRFailed.ToString(),
                                            MessageError = "OidcDiscovery failed InfosecBaseUri: " + infosecBaseUri
                                        };
                                        await new SqlDataAccess(dbConnString).UpdateDcrMsgByDHBrandId(dcrMsg);

                                        string extraMsg = "";
                                        if (!string.IsNullOrEmpty(infosecBaseUri))
                                            extraMsg = " - InfosecBaseUri: " + infosecBaseUri;

                                        await InsertLog(log, dbLoggingConnString, $"{msg}, REGISTRATION FAILED{extraMsg}", "Exception", "DCR");
                                    }
                                }
                                else
                                {
                                    // YES - log this result
                                    await InsertLog(log, dbConnString, $"{msg} - is trying to be REGISTERED, but is already REGISTERED to ClientId - {clientId}", "Error", "DCR");
                                }
                            }
                        }
                        else
                        {
                            await InsertLog(log, dbConnString, $"{msg}, Unable to get the SSA from: {ssaEndpoint}, Ver: {xv}, BrandId: {brandId}, SoftwareProductId - {softwareProductId}", "Error", "DCR");
                        }
                    }
                    else
                    {
                        await InsertLog(log, dbConnString, $"{msg}, Unable to get the Access Token for SoftwareProductId - {softwareProductId} - at the endpoint - {tokenEndpoint}", "Error", "DCR");
                    }
                }
            }
            catch (Exception ex)
            {
                string dbConnString = Environment.GetEnvironmentVariable("DataRecipient_DB_ConnectionString");
                string dbLoggingConnString = Environment.GetEnvironmentVariable("DataRecipient_Logging_DB_ConnectionString");

                DcrMessage dcrMsg = new()
                {
                    DataHolderBrandId = new Guid(myQMsg.DataHolderBrandId),
                    BrandName = dataHolderBrandName,
                    InfosecBaseUri = infosecBaseUri,
                    MessageState = MessageEnum.DCRFailed.ToString(),
                    MessageError = ex.Message
                };
                await new SqlDataAccess(dbConnString).UpdateDcrMsgByDHBrandId(dcrMsg);

                string extraMsg = "";
                if (!string.IsNullOrEmpty(infosecBaseUri))
                    extraMsg = " - InfosecBaseUri: " + infosecBaseUri;

                if (!string.IsNullOrEmpty(regEndpoint))
                    extraMsg = " - RegistrationEndpoint: " + regEndpoint;

                if (ex is JsonReaderException)
                {
                    await InsertLog(log, dbLoggingConnString, $"{msg}, REGISTRATION FAILED: OidcDiscovery can't be desiearlized {extraMsg}", "Exception", "DCR", ex);
                }
                else 
                    await InsertLog(log, dbLoggingConnString, $"{msg}, REGISTRATION FAILED{extraMsg}", "Exception", "DCR", ex);
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
        /// Generate the SSA
        /// </summary>
        private static async Task<Response<string>> GetSoftwareStatementAssertion(
            string ssaEndpoint,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string brandId,
            string softwareProductId,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            // Setup the request to the get ssa endpoint.
            var endpoint = $"{ssaEndpoint}{brandId}/software-products/{softwareProductId}/ssa";

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken, version, ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            log.LogInformation("Retrieving SSA from the Register: {ssaEndpoint}", endpoint);

            // Make the request to the get data holder brands endpoint.
            var response = await client.GetAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();
            var ssaResponse = new Response<string>()
            {
                StatusCode = response.StatusCode
            };

            log.LogInformation("SSA response: {statusCode} - {body}", ssaResponse.StatusCode, body);

            if (response.IsSuccessStatusCode)
            {
                ssaResponse.Data = body;
                ssaResponse.Message = "SSA Generated";
            }
            else
            {
                ssaResponse.Message = $"Failed to generate an SSA: {body}";
            }
            return ssaResponse;
        }

        /// <summary>
        /// Get the OpenID Discovery
        /// </summary>
        private static async Task<Response<OidcDiscovery>> GetOidcDiscovery(string infosecBaseUri, bool ignoreServerCertificateErrors = false)
        {
            var oidcResponse = new Response<OidcDiscovery>();

            var clientHandler = new HttpClientHandler();

            if (ignoreServerCertificateErrors)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(clientHandler);

            var configUrl = string.Concat(infosecBaseUri.TrimEnd('/'), "/.well-known/openid-configuration");
            var configResponse = await client.GetAsync(configUrl);

            oidcResponse.StatusCode = configResponse.StatusCode;

            if (configResponse.IsSuccessStatusCode)
            {
                var body = await configResponse.Content.ReadAsStringAsync();
                oidcResponse.Data = JsonConvert.DeserializeObject<OidcDiscovery>(body);
            }

            return oidcResponse;
        }

        /// <summary>
        /// Generate the DCR Request JWT
        /// </summary>
        private static string PopulateDCRRequestJwt(string softwareProductId, string redirectUris, string ssa, string audience, X509Certificate2 signCertificate)
        {
            var claims = new List<Claim>
            {
                new Claim("jti", Guid.NewGuid().ToString()),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer),
                new Claim("token_endpoint_auth_signing_alg", "PS256"),
                new Claim("token_endpoint_auth_method", "private_key_jwt"),
                new Claim("application_type", "web"),
                new Claim("id_token_signed_response_alg", "PS256"),
                new Claim("id_token_encrypted_response_alg", "RSA-OAEP"),
                new Claim("id_token_encrypted_response_enc", "A256GCM"),
                new Claim("request_object_signing_alg", "PS256"),
                new Claim("software_statement", ssa ?? ""),
                new Claim("redirect_uris", redirectUris),
                new Claim("grant_types", "client_credentials"),
                new Claim("grant_types", "authorization_code"),
                new Claim("grant_types", "refresh_token"),
                new Claim("response_types", "code id_token")
            };

            var jwt = new JwtSecurityToken(
                issuer: softwareProductId,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new X509SigningCredentials(signCertificate, SecurityAlgorithms.RsaSsaPssSha256));

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(jwt);
        }

        /// <summary>
        /// DCR
        /// </summary>
        private static async Task<DcrResponse> Register(
            string dcrEndpoint, 
            X509Certificate2 clientCertificate, 
            string payload,
            bool ignoreServerCertificateErrors = false)
        {
            // Setup the http client.
            var client = GetHttpClient(clientCertificate, ignoreServerCertificateErrors: ignoreServerCertificateErrors);

            // Create the post content.
            var content = new StringContent(payload);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/jwt");

            // Make the request to the data holder's registration endpoint.
            var response = await client.PostAsync(dcrEndpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            return new DcrResponse()
            {
                Data = JsonConvert.DeserializeObject<Registration>(body),
                StatusCode = response.StatusCode,
                Message = response.IsSuccessStatusCode ? "Registration successful." : $"Failed to register: {body}",
                Payload = body
            };
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
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Add the x-v header to the request if provided.
            if (!string.IsNullOrEmpty(version))
                client.DefaultRequestHeaders.Add("x-v", version);

            return client;
        }

        /// <summary>
        /// Insert the Message into the Queue
        /// </summary>
        private static async Task AddDeadLetterQueMsgAsync(ILogger log, string dbConnString, string qConnString, string dhBrandId, CloudQueueMessage myQueueItem, string qName)
        {
            QueueClientOptions options = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            QueueClient qClient = new(qConnString, qName, options);
            await qClient.CreateIfNotExistsAsync();

            DeadLetterQueueMessage qMsg = new()
            {
                MessageVersion = "1.0",
                MessageSource = "dynamicclientregistration",
                SourceMessageId = myQueueItem.Id,
                SourceMessageInsertionTime = myQueueItem.InsertionTime.ToString(),
                DataHolderBrandId = dhBrandId
            };
            string qMessage = JsonConvert.SerializeObject(qMsg);
            await qClient.SendMessageAsync(qMessage);

            int qCount = await GetQueueCountAsync(qConnString, qName);
            DcrMessage dcrMsg = new()
            {
                MessageId = myQueueItem.Id,
                MessageState = MessageEnum.Abandoned.ToString(),
                MessageError = $"DCR - {qCount} items queued, this DataHolderBrandId is malformed"
            };
            await new SqlDataAccess(dbConnString).UpdateDcrMsgByMessageId(dcrMsg);
            await InsertLog(log, dbConnString, $"DCR - {qCount} items in {qName} queue", "Error", "DCR");
        }

        /// <summary>
        /// Queue Item Count
        /// </summary>
        private static async Task<int> GetQueueCountAsync(string qConnString, string qName)
        {
            QueueClient qClient = new(qConnString, qName);
            if (qClient.Exists())
            {
                QueueProperties properties = await qClient.GetPropertiesAsync();
                return properties.ApproximateMessagesCount;
            }
            return 0;
        }

        /// <summary>
        /// Update the Log table
        /// </summary>
        private static async Task InsertLog(ILogger log, string dbConnString, string msg, string lvl, string methodName, Exception exMsg = null)
        {
            log.LogInformation($"{methodName} - {msg}");

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
                cmd.Parameters.AddWithValue("@srcContext", "CDR.DCR");
                await cmd.ExecuteNonQueryAsync();
                db.Close();
            }
        }
    }
}