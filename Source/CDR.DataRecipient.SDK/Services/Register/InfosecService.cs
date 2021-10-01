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
            IAccessTokenService accessTokenService) : base(config, logger)
        {
            _accessTokenService = accessTokenService;
        }

        public async Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetAccessToken)}.");

            return await _accessTokenService.GetAccessToken(tokenEndpoint, clientId, clientCertificate, signingCertificate, Constants.Scopes.CDR_REGISTER);
        }

    }
}
