using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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

            // When the Data Holder sends an arrangement revoke request to the Data Recipient:
            // From 31 July 2022:
            //   - Mock Data Recipient(MDR) must validate that the cdr_arrangement_jwt form parameter is passed in the request and includes a cdr_arrangement_id field.
            //   - MDR cannot reject a request that contains a cdr_arrangement_id form parameter.
            //   - If both the cdr_arrangement_id and cdr_arrangement_jwt form parameters are passed by the data holder, then the MDR must validate that both cdr_arrangement_id values match
            // From 15 November 2022:
            //   - if the Self-Signed JWT claims (iss, sub, exp, aud, jti) are presented in the cdr_arrangement_jwt form parameter, MDR must validate in accordance with "Data Holders calling Data Recipients using Self-Signed JWT Client Authentication"(https://consumerdatastandardsaustralia.github.io/standards/#client-authentication)

            // Validate that the cdr arrangement jwt parameter has been passed.
            if (string.IsNullOrEmpty(revocationModel.CdrArrangementJwt))
            {
                _logger.LogDebug("The cdr_arrangement_jwt was missing");
                return BadRequest(new ErrorListModel(ErrorCodes.MissingField, ErrorTitles.MissingField, CdrArrangementRevocationRequest.CdrArrangementJwt));
            }

            // Retrieve the cdr_arrangement_id from the JWT.
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(revocationModel.CdrArrangementJwt);

            if (token == null || token.Claims == null || !token.Claims.Any())
            {
                _logger.LogDebug("The cdr_arrangement_jwt did not have claims");
                return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
            }

            // cdr_arrangement_id claim was not found in cdr_arrangement_jwt.
            var cdrArrangementIdClaim = token.Claims.FirstOrDefault(c => c.Type.Equals(CdrArrangementRevocationRequest.CdrArrangementId));
            if (cdrArrangementIdClaim == null)
            {
                _logger.LogDebug("The cdr_arrangement_jwt did not contain a cdr_arrangement_id claim");
                return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
            }

            // If a cdr_arrangement_id form parameter has also been passed, then validate the value is the same as the value in the cdr_arrangement_jwt.
            if (!string.IsNullOrEmpty(revocationModel.CdrArrangementId)
                && !revocationModel.CdrArrangementId.Equals(cdrArrangementIdClaim.Value, System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("The provided cdr_arrangement_id values did not match");
                return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementId));
            }

            var sp = _config.GetSoftwareProductConfig();

            // Find the matching cdr arrangement.
            var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementIdClaim.Value);

            // If the arrangement was not found or if the arrangement does not belong to the calling data holder, then return an error.
            // Note: The client_id in the bearer token contains the Data Holder Brand Id.
            if (arrangement == null
                || !arrangement.DataHolderBrandId.Equals(ClientBrandId, System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("The arrangement could not be found or was not owned by the calling data holder brand. Arrangement: {cdrArrangementId}, Data Holder Brand: {clientBrandId}", cdrArrangementIdClaim.Value, ClientBrandId);
                return UnprocessableEntity(new ErrorListModel(ErrorCodes.InvalidConsent, ErrorTitles.InvalidArrangement, cdrArrangementIdClaim.Value));
            }

            // Try and extract the "Self-Signed JWT Client Authentication" claims from the cdr_arrangement_jwt.
            var issClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("iss"));
            var subClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("sub"));
            var audClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("aud"));
            var jtiClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("jti"));
            var expClaim = token.Claims.FirstOrDefault(c => c.Type.Equals("exp"));

            // From 15/11/2022, full cdr_arrangement_jwt is required is self-signed jwt parameters are included.
            string validIssuer = null;
            string validAudience = null;
            bool validateLifetime = false;
            bool fullValidationRequired = 
                DateTime.UtcNow > _config.AttemptValidateCdrArrangementJwtFromDate()
                && HasValue(issClaim) 
                && HasValue(subClaim) 
                && HasValue(audClaim) 
                && HasValue(jtiClaim) 
                && HasValue(expClaim);

            if (fullValidationRequired)
            {
                _logger.LogDebug("Full validation of the cdr_arrangement_jwt should occur...");

                // iss claim and sub claim should have the same value.
                if (!issClaim.Value.Equals(subClaim.Value, System.StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("The iss and sub claim values did not match in the cdr_arrangement_jwt");
                    return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
                }

                // Set the values that will be used in cdr_arrangement_jwt token validation.
                validIssuer = ClientBrandId;
                validAudience = sp.RevocationUri;
                validateLifetime = true;
            }

            // Validate the cdr_arrangement_jwt either using "full" or "minimal" validation.
            var jwksUri = await GetJwksUri();
            var validated = await revocationModel.CdrArrangementJwt.ValidateToken(
                jwksUri,
                _logger,
                validIssuer: validIssuer,
                validAudiences: validAudience != null ? new string[] { validAudience } : null,
                validateLifetime: validateLifetime,
                acceptAnyServerCertificate: _config.IsAcceptingAnyServerCertificate());

            if (!validated.IsValid)
            {
                _logger.LogDebug("Token validation failed on cdr_arrangement_jwt");
                return BadRequest(new ErrorListModel(ErrorCodes.InvalidField, ErrorTitles.InvalidField, CdrArrangementRevocationRequest.CdrArrangementJwt));
            }

            revocationModel.CdrArrangementId = cdrArrangementIdClaim.Value;

            var isDeleted = await _consentsRepository.RevokeConsent(revocationModel.CdrArrangementId, ClientBrandId);
            if (!isDeleted)
            {
                _logger.LogDebug("An error occurred when attempting to delete the arrangement");

                // No matching record in the DB for the arrangement id and brand id combination or failed to delete the consent
                return UnprocessableEntity(new ErrorListModel(ErrorCodes.InvalidConsent, ErrorTitles.InvalidArrangement, $"Invalid arrangement: {revocationModel.CdrArrangementId}"));
            }

            return NoContent();
        }

        private static bool HasValue(Claim claim)
        {
            return claim != null && claim.Value != null;
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
