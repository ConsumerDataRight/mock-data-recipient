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
            IServiceConfiguration serviceConfiguration) : base(config, logger, serviceConfiguration)
        {
            _accessTokenService = accessTokenService;
        }

        public async Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope = Constants.Scopes.CDR_REGISTER)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetAccessToken)}.");

            return await _accessTokenService.GetAccessToken(
                tokenEndpoint, 
                clientId, 
                clientCertificate, 
                signingCertificate, 
                scope);
        }

        public async Task<Response<OidcDiscovery>> GetOidcDiscovery(string registerOidcConfigEndpoint)
        {
            var oidcResponse = new Response<OidcDiscovery>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetOidcDiscovery)}.");

            var client = GetHttpClient();
            var configResponse = await client.GetAsync(EnsureValidEndpoint(registerOidcConfigEndpoint));

            oidcResponse.StatusCode = configResponse.StatusCode;

            if (configResponse.IsSuccessStatusCode)
            {
                var body = await configResponse.Content.ReadAsStringAsync();
                oidcResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<OidcDiscovery>(body);
            }

            return oidcResponse;
        }

        public async Task<string> GetTokenEndpoint(string registerOidcConfigEndpoint)
        {
            var oidd = await GetOidcDiscovery(registerOidcConfigEndpoint);
            return oidd.Data.TokenEndpoint;
        }
    }
}
