using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Models
{
	public class RevocationModel : BaseModel
	{
		[FromForm(Name = "cdr_arrangement_id")]
		public string CdrArrangementId { get; set; }

		[FromForm(Name = "cdr_arrangement_jwt")]
		public string CdrArrangementJwt { get; set; }
	}
}
