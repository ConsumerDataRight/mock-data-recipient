using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Controllers
{
	[ClientAuthorize]
	public class RevocationController : Controller
	{
		private readonly IConsentsRepository _consentsRepository;
		private readonly ILogger<RevocationController> _logger;

		private ClaimsPrincipal Client => (ClaimsPrincipal)this.HttpContext.Items[ClientAuthorize.ClaimsPrincipalKey];
		private string ClientBrandId => Client.Claims.Where(c => c.Type == "iss").FirstOrDefault()?.Value;
				
		public RevocationController(
			IConsentsRepository consentsRepository,
			ILogger<RevocationController> logger)
		{
			_consentsRepository = consentsRepository;
			_logger = logger;
		}

		[HttpPost]
		[Route(Constants.Urls.ClientArrangementRevokeUrl)]
		[MustConsume("application/x-www-form-urlencoded")]
		public async Task<IActionResult> Revoke([Required, FromForm] RevocationModel revocationModel)
		{
			_logger.LogInformation($"Request received to {nameof(RevocationController)}.{nameof(Revoke)}");
			
			if (string.IsNullOrEmpty(revocationModel.CdrArrangementId))
			{
				return BadRequest(new ErrorListModel(Constants.ErrorCodes.MissingField, Constants.ErrorTitles.MissingField, "cdr_arrangement_id"));
			}

			var isDeleted = await _consentsRepository.RevokeConsent(revocationModel.CdrArrangementId, ClientBrandId);
			if (!isDeleted)
			{
				// No matching record in the DB for the arrangement id and brand id combination or failed to delete the consent
				return UnprocessableEntity(new ErrorListModel(Constants.ErrorCodes.InvalidArrangement, Constants.ErrorTitles.InvalidArrangement, string.Empty));
			}

			return NoContent();
		}
	}
}
