using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
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
                var sp = _config.GetSoftwareProductConfig();

                // Retrieve the cdr_arrangement_id from the JWT.
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(revocationModel.CdrArrangementJwt);

                if (token == null || token.Claims == null || !token.Claims.Any())
                {
                    return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
                }

                var cdrArrangementIdClaim = token.Claims.FirstOrDefault(c => c.Type.Equals(CdrArrangementRevocationRequest.CdrArrangementId));
                if (cdrArrangementIdClaim == null)
                {
                    return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
                }

                // Check for mandatory claims in the cdr_arrangement_jwt.
                var issClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("iss"));
                var subClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("sub"));
                var audClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("aud"));
                var jtiClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("jti"));
                var expClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("exp"));
                if (subClaim == null || string.IsNullOrEmpty(subClaim.Value)
                    || issClaim == null || string.IsNullOrEmpty(issClaim.Value)
                    || audClaim == null || string.IsNullOrEmpty(audClaim.Value)
                    || jtiClaim == null || string.IsNullOrEmpty(jtiClaim.Value)
                    || expClaim == null || string.IsNullOrEmpty(expClaim.Value)
                    || !subClaim.Value.Equals(issClaim.Value))
                {
                    return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
                }

                // Find the matching cdr arrangement.
                var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementIdClaim.Value);
                if (arrangement == null 
                    || !arrangement.DataHolderBrandId.Equals(issClaim.Value, System.StringComparison.OrdinalIgnoreCase))
                {
                    return UnprocessableEntity(new ErrorListModel(ErrorCodes.InvalidConsent, ErrorTitles.InvalidArrangement, $"Invalid arrangement: {cdrArrangementIdClaim.Value}"));
                }

                // Validate the cdr_arrangement_jwt using the brand id associated with the arrangement.
                var jwksUri = await GetJwksUri();
                var validated = await revocationModel.CdrArrangementJwt.ValidateToken(
                    jwksUri,
                    validIssuer: arrangement.DataHolderBrandId, // DH Brand Id
                    validAudiences: new[] { sp.RevocationUri },
                    acceptAnyServerCertificate: _config.IsAcceptingAnyServerCertificate());

                if (!validated.IsValid)
                {
                    return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
                }

                revocationModel.CdrArrangementId = cdrArrangementIdClaim.Value;
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
