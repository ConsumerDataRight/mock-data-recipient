using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services.Tokens
{
    public class AccessTokenService : BaseService, IAccessTokenService
    {
        private AccessToken AccessToken { get; set; }

        public AccessTokenService(
            IConfiguration config,
            ILogger<AccessTokenService> logger,
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
            // LEGACY CODE
            AccessToken = new AccessToken() { Scope = string.Empty };
        }

        public async Task<Response<Token>> GetAccessToken(AccessToken accessToken)
        {
            var tokenResponse = new Response<Token>();

            _logger.LogDebug($"Request received to {nameof(AccessTokenService)}.{nameof(GetAccessToken)}.");

            AccessToken = accessToken;

            // Setup the http client.
            var client = GetHttpClient(AccessToken.ClientCertificate);

            _logger.LogDebug(
                "Requesting access token from: {TokenEndpoint}.  Software Product ID: {ClientId}.  Client Certificate: {Thumbprint}",
                AccessToken.TokenEndpoint,
                AccessToken.ClientId,
                AccessToken.ClientCertificate.Thumbprint);

            // Make the request to the token endpoint.
            var response = await client.SendPrivateKeyJwtRequest(
                AccessToken.TokenEndpoint,
                AccessToken.SigningCertificate,
                AccessToken.ClientId,
                AccessToken.ClientId,
                AccessToken.Scope,
                AccessToken.RedirectUri,
                AccessToken.Code,
                AccessToken.GrantType,
                pkce: AccessToken.Pkce,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Access Token Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

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
