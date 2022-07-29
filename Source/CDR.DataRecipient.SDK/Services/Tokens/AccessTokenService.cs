using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services.Tokens
{
    public class AccessTokenService : BaseService, IAccessTokenService
    {
        public AccessTokenService(
            IConfiguration config,
            ILogger<AccessTokenService> logger,
            IServiceConfiguration serviceConfiguration) : base(config, logger, serviceConfiguration)
        {
        }

        public async Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope,
            string redirectUri = null,
            string code = null,
            string grantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            Pkce pkce = null)
        {
            var tokenResponse = new Response<Token>();

            _logger.LogDebug($"Request received to {nameof(AccessTokenService)}.{nameof(GetAccessToken)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            _logger.LogDebug("Requesting access token from: {tokenEndpoint}.  Software Product ID: {clientId}.  Client Certificate: {thumbprint}", tokenEndpoint, clientId, clientCertificate.Thumbprint);

            // Make the request to the token endpoint.
            var response = await client.SendPrivateKeyJwtRequest(
                tokenEndpoint, 
                signingCertificate,
                clientId,
                clientId,
                scope, 
                redirectUri, 
                code, 
                grantType,
                pkce: pkce,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Access Token Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            tokenResponse.StatusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                tokenResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Token>(body);
            }
            else
            {
                tokenResponse.Message = body;
            }

            return tokenResponse;
        }
    }
}
