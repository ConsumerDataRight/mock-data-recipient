using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static CDR.DataRecipient.Web.Common.Constants;

namespace CDR.DataRecipient.Web.Controllers
{
    [ClientAuthorize]
    public class RevocationController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
        private readonly ILogger<RevocationController> _logger;

        private ClaimsPrincipal Client => (ClaimsPrincipal)this.HttpContext.Items[ClientAuthorizeAttribute.ClaimsPrincipalKey];
        private string ClientBrandId => Client.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;

        public RevocationController(
            IConfiguration config,
			IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            IConsentsRepository consentsRepository,
            ILogger<RevocationController> logger)
        {
            _config = config;
			_dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            _consentsRepository = consentsRepository;
			_logger = logger;
        }

        [HttpPost]
        [Route(Urls.ClientArrangementRevokeUrl)]
        [MustConsume("application/x-www-form-urlencoded")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Revoke([Required, FromForm] RevocationModel revocationModel)
        {
            _logger.LogDebug("Revocation request received.  cdr_arrangement_id: {CdrArrangementId}. cdr_arrangement_jwt: {CdrArrangementJwt}", revocationModel.CdrArrangementId, revocationModel.CdrArrangementJwt);

            // When the Data Holder sends an arrangement revoke request to the Data Recipient,
            // From March 31st 2022, Data Recipients MUST support "CDR Arrangement JWT" method.
            // Until July 31st 2022, Data Recipients MUST support both "CDR Arrangement Form Parameter" method and "CDR Arrangement JWT".
            // From July 31st 2022, Data Recipients MUST only support "CDR Arrangement JWT" method and MUST reject "CDR Arrangement Form Parameter" method.
            // If only accepting JWT parameter and it has not been supplied.
            if (_config.CdrArrangementAsJwtOnly() && string.IsNullOrEmpty(revocationModel.CdrArrangementJwt))
            {
                return BadRequest(new ErrorListModel(ErrorCodes.MissingField, ErrorTitles.MissingField, CdrArrangementRevocationRequest.CdrArrangementJwt));
            }

            // At least 1 field needs to be provided.
            if (string.IsNullOrEmpty(revocationModel.CdrArrangementId) && string.IsNullOrEmpty(revocationModel.CdrArrangementJwt))
            {
                return BadRequest(new ErrorListModel(ErrorCodes.MissingField, ErrorTitles.MissingField, $"{CdrArrangementRevocationRequest.CdrArrangementJwt} or {CdrArrangementRevocationRequest.CdrArrangementId}"));
            }

            // cdr_arrangement_jwt takes precedence.
            if (!string.IsNullOrEmpty(revocationModel.CdrArrangementJwt))
            {
                var jwksUri = await GetJwksUri();
                var validated = await revocationModel.CdrArrangementJwt.ValidateToken(jwksUri, validateLifetime: false);
                if (!validated.IsValid)
                {
                    return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
                }

                revocationModel.CdrArrangementId = validated.ClaimsPrincipal.Claims
                    .Where(p => p.Type == CdrArrangementRevocationRequest.CdrArrangementId)
                    .FirstOrDefault()?.Value;
            }

            var isDeleted = await _consentsRepository.RevokeConsent(revocationModel.CdrArrangementId, ClientBrandId);
            if (!isDeleted)
            {
                // No matching record in the DB for the arrangement id and brand id combination or failed to delete the consent
                return UnprocessableEntity(new ErrorListModel(ErrorCodes.InvalidConsent, ErrorTitles.InvalidArrangement, $"Invalid arrangement: {revocationModel.CdrArrangementId}"));
            }

            return NoContent();
        }

        private async Task<string> GetJwksUri()
        {
            // Get the current data holder details.
            var dataholderDiscoveryDocument = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(this.ClientBrandId);
            if (dataholderDiscoveryDocument == null)
            {
                // There is no valid brand id in our DB for this issuer.
                return null;
            }

            return dataholderDiscoveryDocument.JwksUri;
        }
    }
}
