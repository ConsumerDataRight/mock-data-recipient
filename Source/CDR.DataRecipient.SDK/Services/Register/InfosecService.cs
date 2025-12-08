using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public class InfosecService : BaseService, IInfosecService
    {
        private readonly IAccessTokenService _accessTokenService;

        public InfosecService(
            IConfiguration config,
            ILogger<InfosecService> logger,
            IAccessTokenService accessTokenService,
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
            this._accessTokenService = accessTokenService;
        }

        public async Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope = Constants.Scopes.CDR_REGISTER)
        {
            this.Logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(this.GetAccessToken)}.");

            var accessToken = new AccessToken()
            {
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientCertificate = clientCertificate,
                SigningCertificate = signingCertificate,
                Scope = scope,
            };

            return await this._accessTokenService.GetAccessToken(accessToken);
        }

        public async Task<Response<OidcDiscovery>> GetOidcDiscovery(string registerOidcConfigEndpoint)
        {
            var oidcResponse = new Response<OidcDiscovery>();

            this.Logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(this.GetOidcDiscovery)}.");

            this.Logger.LogDebug($"Attempting register oidc config.");

            this.Logger.LogDebug("Oidc uri: {Uri}.", registerOidcConfigEndpoint);

            var client = this.GetHttpClient();
            var configResponse = await client.GetAsync(this.EnsureValidEndpoint(registerOidcConfigEndpoint));

            this.Logger.LogDebug($"Oidc config call completed.");

            if (configResponse == null)
            {
                this.Logger.LogDebug($"Oidc config response is null");
            }

            if (configResponse != null)
            {
                this.Logger.LogDebug("Oidc response: {StatusCode}.", configResponse.StatusCode);
                oidcResponse.StatusCode = configResponse.StatusCode;

                if (configResponse.IsSuccessStatusCode)
                {
                    var body = await configResponse.Content.ReadAsStringAsync();
                    oidcResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<OidcDiscovery>(body);
                }
            }

            return oidcResponse;
        }

        public async Task<string> GetTokenEndpoint(string registerOidcConfigEndpoint)
        {
            var oidd = await this.GetOidcDiscovery(registerOidcConfigEndpoint);
            return oidd.Data.TokenEndpoint;
        }
    }
}
