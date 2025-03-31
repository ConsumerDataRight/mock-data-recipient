using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CDR.DataRecipient.Repository.SQL;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Enum;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DCR.Extensions;
using CDR.DCR.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DCR
{
    public class DynamicClientRegistrationFunction
    {
        private readonly ILogger _logger;
        private readonly DcrOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly X509Certificate2 _signingCertificate;

        public DynamicClientRegistrationFunction(ILogger<DynamicClientRegistrationFunction> logger, IOptions<DcrOptions> options, IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            //get the certs
            _logger.LogInformation("Loading the client certificate...");
            byte[] clientCertBytes = Convert.FromBase64String(_options.Client_Certificate);
            X509Certificate2 cclientCertificate = new(clientCertBytes, _options.Client_Certificate_Password, X509KeyStorageFlags.MachineKeySet);
            _logger.LogInformation("Client certificate loaded: {thumbprint}", cclientCertificate.Thumbprint);


            _logger.LogInformation("Loading the signing certificate...");
            byte[] signCertBytes = Convert.FromBase64String(_options.Signing_Certificate);
            _signingCertificate = new(signCertBytes, _options.Signing_Certificate_Password, X509KeyStorageFlags.MachineKeySet);
            _logger.LogInformation("Signing certificate loaded: {thumbprint}", _signingCertificate.Thumbprint);
        }

        /// <summary>
        /// Dynamic Client Registration Function
        /// </summary>
        /// <remarks>Registers the Data Holders in the messaging queue and updates the local repository</remarks>
        [Function("FunctionDCR")]
        public async Task DCR([QueueTrigger("dynamicclientregistration", Connection = "StorageConnectionString")] DcrQueueMessage myQueueItem, FunctionContext context)
        {
            string msg = string.Empty;
            string dataHolderBrandName = string.Empty;
            string infosecBaseUri = string.Empty;
            string regEndpoint = string.Empty;

            try
            {
                _logger.LogInformation("Retrieving count for dynamicclientregistration queue...");
                string qName = "dynamicclientregistration";
                int qCount = await GetQueueCountAsync(_options.StorageConnectionString, qName);
                _logger.LogInformation("qCount = {count}", qCount);

                if (string.IsNullOrEmpty(myQueueItem.DataHolderBrandId))
                {
                    // Add messsage to deadletter queue
                    await AddDeadLetterQueMsgAsync(myQueueItem, _options.DeadLetterQueueName, context);
                }
                else
                {
                    msg = $"DHBrandId - {myQueueItem.DataHolderBrandId}";

                    Response<Token> tokenResponse = await GetAccessToken();
                    if (tokenResponse.IsSuccessful)
                    {
                        var ssa = await GetSoftwareStatementAssertion(tokenResponse.Data.AccessToken);
                        if (ssa.IsSuccessful)
                        {
                            //DOES the Data Holder Brand EXIST in the REPO?
                            DataHolderBrand dh = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).GetDataHolderBrand(myQueueItem.DataHolderBrandId);
                            if (dh == null)
                            {
                                // NO - DOES the DcrMessage exist?
                                var result = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).CheckDcrMessageExistByDHBrandId(myQueueItem.DataHolderBrandId);
                                if (!string.IsNullOrEmpty(result.MsgId))
                                {
                                    // YES - UPDATE EXISTING DcrMessage (with ADDED Queue MessageId, Failed STATE and ERROR)
                                    DcrMessage dcrMsg = new()
                                    {
                                        DataHolderBrandId = Guid.Empty,
                                        MessageId = result.MsgId,
                                        MessageState = Message.DCRFailed.ToString(),
                                        MessageError = $"{msg} - does not exist in the repo"
                                    };

                                    await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgReplaceMessageIdWithoutBrand(dcrMsg, context.BindingContext.BindingData["Id"].ToString());
                                }
                                await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg} - does not exist in the repo", "Error", "DCR");
                            }
                            else
                            {
                                dataHolderBrandName = dh.BrandName;
                                // YES - DOES a Registration already exist for the DataHolderBrandId in the local repo? If yes, then no need to do any registration
                                string clientId = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).GetRegByDHBrandId(dh.DataHolderBrandId);
                                if (string.IsNullOrEmpty(clientId))
                                {
                                    // NO - register this Data Holder Brand
                                    infosecBaseUri = dh.EndpointDetail.InfoSecBaseUri;
                                    var oidcDiscovery = (await GetOidcDiscovery(infosecBaseUri)).Data;
                                    if (oidcDiscovery != null)
                                    {
                                        regEndpoint = oidcDiscovery.RegistrationEndpoint;

                                        var dcrRequest = new DcrRequest()
                                        {
                                            SoftwareProductId = _options.Software_Product_Id,
                                            RedirectUris = _options.Redirect_Uris,
                                            Ssa = ssa.Data,
                                            Audience = oidcDiscovery.Issuer,
                                            ResponseTypesSupported = oidcDiscovery.ResponseTypesSupported,
                                            AuthorizationSigningResponseAlgValuesSupported = oidcDiscovery.AuthorizationSigningResponseAlgValuesSupported,
                                            AuthorizationEncryptionResponseEncValuesSupported = oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported,
                                            AuthorizationEncryptionResponseAlgValuesSupported = oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported,
                                            SignCertificate = _signingCertificate
                                        };

                                        (string errorMessage, string dcrRequestJwt) = PopulateDCRRequestJwt(dcrRequest);

                                        // Process Registration - retry 3 times
                                        bool regSuccess = false;
                                        string regStatusCode = "";
                                        string regMessage = "";
                                        string regClientId = "";
                                        string jsonDoc = "";

                                        // DO NOT register if FAPI claims are invalid
                                        if (string.IsNullOrEmpty(errorMessage))
                                        {
                                            do
                                            {
                                                var dcrResponse = await Register(regEndpoint, dcrRequestJwt);
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

                                                    //no need to retry if the error is duplicate registration.
                                                    if (dcrResponse.Message.Contains("ERR-DCR-001"))
                                                    {
                                                        break;
                                                    }
                                                    _options.Retries--;
                                                }
                                            } while (!regSuccess && _options.Retries > 0);
                                        }
                                        // Successful -> Update DcrMessage and Insert into Data Holder Registration repo
                                        // DO NOT register if FAPI claims are invalid
                                        if (regSuccess && string.IsNullOrEmpty(errorMessage))
                                        {
                                            DcrMessage dcrMsg = new()
                                            {
                                                ClientId = regClientId,
                                                DataHolderBrandId = new Guid(dh.DataHolderBrandId),
                                                BrandName = dh.BrandName,
                                                InfosecBaseUri = infosecBaseUri,
                                                MessageState = Message.DCRComplete.ToString()
                                            };
                                            await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgByDHBrandId(dcrMsg);

                                            var dcrInserted = await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).InsertDcrRegistration(regClientId, dh.DataHolderBrandId, jsonDoc);
                                            if (dcrInserted)
                                                await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg}, REGISTERED as ClientId - {regClientId}, {qCount - 1} items remain in queue, ", "Information", "DCR");
                                            else
                                                await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg}, REGISTERED as ClientId - {regClientId}, {qCount - 1} items remain in queue - FAILED to add to MDR REPO", "Error", "DCR");
                                        }

                                        // FAILED -> Update DcrMessage as DCRFailed
                                        // FAPI 1.0 validation errors should also be logged
                                        else if (!string.IsNullOrEmpty(errorMessage))
                                        {
                                            DcrMessage dcrMsg = new()
                                            {
                                                DataHolderBrandId = new Guid(dh.DataHolderBrandId),
                                                BrandName = dh.BrandName,
                                                InfosecBaseUri = infosecBaseUri,
                                                MessageState = Message.DCRFailed.ToString(),
                                                MessageError = $"{errorMessage}"
                                            };
                                            await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgByDHBrandId(dcrMsg);
                                            await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg}, REGISTRATION CLAIMS VALIDATIONS FAILED, {errorMessage}", "Error", "DCR");
                                        }
                                        else if (!regSuccess)
                                        {
                                            regMessage = regMessage.Replace("'", "");
                                            DcrMessage dcrMsg = new()
                                            {
                                                DataHolderBrandId = new Guid(dh.DataHolderBrandId),
                                                BrandName = dh.BrandName,
                                                InfosecBaseUri = infosecBaseUri,
                                                MessageState = Message.DCRFailed.ToString(),
                                                MessageError = $"StatusCode: {regStatusCode}, {regMessage}"
                                            };
                                            await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgByDHBrandId(dcrMsg);
                                            await InsertLog(_options.DataRecipient_DB_ConnectionString, $"{msg}, REGISTRATION FAILED, StatusCode: {regStatusCode}, {regMessage}", "Error", "DCR");
                                        }
                                    }
                                    else
                                    {
                                        // Oidc Discovery failed
                                        DcrMessage dcrMsg = new()
                                        {
                                            DataHolderBrandId = new Guid(myQueueItem.DataHolderBrandId),
                                            BrandName = dh.BrandName,
                                            InfosecBaseUri = infosecBaseUri,
                                            MessageState = Message.DCRFailed.ToString(),
                                            MessageError = "OidcDiscovery failed InfosecBaseUri: " + infosecBaseUri
                                        };
                                        await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgByDHBrandId(dcrMsg);

                                        string extraMsg = "";
                                        if (!string.IsNullOrEmpty(infosecBaseUri))
                                            extraMsg = " - InfosecBaseUri: " + infosecBaseUri;

                                        await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}, REGISTRATION FAILED{extraMsg}", "Exception", "DCR");
                                    }
                                }
                            }
                        }
                        else
                        {
                            await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}, Unable to get the SSA from: {_options.Register_Get_SSA_Endpoint}, Ver: {_options.Register_Get_SSA_XV}, BrandId: {_options.Brand_Id}, SoftwareProductId - {_options.Software_Product_Id}", "Error", "DCR");
                        }
                    }
                    else
                    {
                        await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}, Unable to get the Access Token for SoftwareProductId - {_options.Software_Product_Id} - at the endpoint - {_options.Register_Token_Endpoint}", "Error", "DCR");
                    }
                }
            }
            catch (Exception ex)
            {
                DcrMessage dcrMsg = new()
                {
                    DataHolderBrandId = new Guid(myQueueItem.DataHolderBrandId),
                    BrandName = dataHolderBrandName,
                    InfosecBaseUri = infosecBaseUri,
                    MessageState = Message.DCRFailed.ToString(),
                    MessageError = ex.Message
                };
                await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgByDHBrandId(dcrMsg);

                string extraMsg = "";
                if (!string.IsNullOrEmpty(infosecBaseUri))
                    extraMsg = " - InfosecBaseUri: " + infosecBaseUri;

                if (!string.IsNullOrEmpty(regEndpoint))
                    extraMsg = " - RegistrationEndpoint: " + regEndpoint;

                if (ex is JsonReaderException)
                {
                    await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}, REGISTRATION FAILED: OidcDiscovery can't be deserialized {extraMsg}", "Exception", "DCR", ex);
                }
                else
                    await InsertLog(_options.DataRecipient_Logging_DB_ConnectionString, $"{msg}, REGISTRATION FAILED{extraMsg}", "Exception", "DCR", ex);
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
                _signingCertificate,
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
        /// Generate the SSA
        /// </summary>
        private async Task<Response<string>> GetSoftwareStatementAssertion(string accessToken)
        {
            // Setup the request to the get ssa endpoint.
            var endpoint = $"{_options.Register_Get_SSA_Endpoint}{_options.Brand_Id}/software-products/{_options.Software_Product_Id}/ssa";

            // Setup the http client.
            var client = GetHttpClient(accessToken, _options.Register_Get_SSA_XV);

            _logger.LogInformation("Retrieving SSA from the Register: {ssaEndpoint}", endpoint);

            // Make the request to the get data holder brands endpoint.
            var response = await client.GetAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();
            var ssaResponse = new Response<string>()
            {
                StatusCode = response.StatusCode
            };

            _logger.LogInformation("SSA response: {statusCode} - {body}", ssaResponse.StatusCode, body);

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
        private async Task<Response<OidcDiscovery>> GetOidcDiscovery(string infosecBaseUri)
        {
            var oidcResponse = new Response<OidcDiscovery>();

            var client = GetHttpClient();

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
        private static (string, string) PopulateDCRRequestJwt(DcrRequest dcrRequest)
        {
            var (claims, errorMessage) = dcrRequest.CreateClaimsForDCRRequest();

            var jwt = new JwtSecurityToken(
                issuer: dcrRequest.SoftwareProductId,
                audience: dcrRequest.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new X509SigningCredentials(dcrRequest.SignCertificate, SecurityAlgorithms.RsaSsaPssSha256));

            var tokenHandler = new JwtSecurityTokenHandler();
            return (errorMessage, tokenHandler.WriteToken(jwt));
        }

        /// <summary>
        /// DCR
        /// </summary>
        private async Task<DcrResponse> Register(
            string dcrEndpoint,
            string payload)
        {
            // Setup the http client.
            var client = GetHttpClient();

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

        private HttpClient GetHttpClient(string accessToken = null, string version = null)
        {
            var httpClient = _httpClientFactory.CreateClient(DcrConstants.DcrHttpClientName);

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
        private async Task AddDeadLetterQueMsgAsync(DcrQueueMessage myQueueItem, string qName, FunctionContext context)
        {
            QueueClientOptions options = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            QueueClient qClient = new(_options.StorageConnectionString, qName, options);
            await qClient.CreateIfNotExistsAsync();

            DeadLetterQueueMessage qMsg = new()
            {
                MessageVersion = "1.0",
                MessageSource = _options.QueueName,
                SourceMessageId = context.BindingContext.BindingData["Id"].ToString(),
                SourceMessageInsertionTime = context.BindingContext.BindingData["InsertionTime"].ToString(),
                DataHolderBrandId = myQueueItem.DataHolderBrandId
            };
            string qMessage = JsonConvert.SerializeObject(qMsg);
            await qClient.SendMessageAsync(qMessage);

            int qCount = await GetQueueCountAsync(_options.StorageConnectionString, qName);
            DcrMessage dcrMsg = new()
            {
                MessageId = context.BindingContext.BindingData["Id"].ToString(),
                MessageState = Message.Abandoned.ToString(),
                MessageError = $"DCR - {qCount} items queued, this DataHolderBrandId is malformed"
            };
            await new SqlDataAccess(_options.DataRecipient_DB_ConnectionString).UpdateDcrMsgByMessageId(dcrMsg);
            await InsertLog(_options.DataRecipient_DB_ConnectionString, $"DCR - {qCount} items in {qName} queue", "Error", "DCR");
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
        private async Task InsertLog(string DataRecipient_DB_ConnectionString, string msg, string lvl, string methodName, Exception exMsg = null)
        {
            _logger.LogInformation("{methodName} - {message}", methodName, msg);

            string exMessage = string.Empty;

            if (exMsg != null)
            {
                Exception innerException = exMsg;
                StringBuilder innerMsg = new();
                int ctr = 0;

                do
                {
                    // skip the first inner exception message as it is the same as the exception message
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

            using SqlConnection db = new(DataRecipient_DB_ConnectionString);
            db.Open();
            var cmdText = string.Empty;

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
