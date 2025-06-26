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
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
            // LEGACY CODE
            this.AccessToken = new AccessToken() { Scope = string.Empty };
        }

        private AccessToken AccessToken { get; set; }

        public async Task<Response<Token>> GetAccessToken(AccessToken accessToken)
        {
            var tokenResponse = new Response<Token>();

            this.Logger.LogDebug($"Request received to {nameof(AccessTokenService)}.{nameof(this.GetAccessToken)}.");

            this.AccessToken = accessToken;

            // Setup the http client.
            var client = this.GetHttpClient(this.AccessToken.ClientCertificate);

            this.Logger.LogDebug(
                "Requesting access token from: {TokenEndpoint}.  Software Product ID: {ClientId}.  Client Certificate: {Thumbprint}",
                this.AccessToken.TokenEndpoint,
                this.AccessToken.ClientId,
                this.AccessToken.ClientCertificate.Thumbprint);

            // Make the request to the token endpoint.
            var response = await client.SendPrivateKeyJwtRequest(
                this.AccessToken.TokenEndpoint,
                this.AccessToken.SigningCertificate,
                this.AccessToken.ClientId,
                this.AccessToken.ClientId,
                this.AccessToken.Scope,
                this.AccessToken.RedirectUri,
                this.AccessToken.Code,
                this.AccessToken.GrantType,
                pkce: this.AccessToken.Pkce,
                enforceHttpsEndpoint: this.ServiceConfiguration.EnforceHttpsEndpoints);

            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Access Token Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

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
