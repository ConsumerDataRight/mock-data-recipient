using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Models
{
	public class RevocationModel : BaseModel
	{
		[FromForm(Name = "cdr_arrangement_id")]
		[Required]
		public string CdrArrangementId { get; set; }
	}
}
