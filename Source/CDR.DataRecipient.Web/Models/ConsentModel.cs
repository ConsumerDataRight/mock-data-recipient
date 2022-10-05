using CDR.DataRecipient.SDK.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Web.Models
{
	public class ConsentModel : BaseModel
	{
		
		public IEnumerable<Registration> Registrations { get; set; }

		public string AuthorisationUri { get; set; }

		/// <summary>
		/// This has the format of {ClientId}|||{DataHolderBrandId}
		/// </summary>
		[Display(Name = "Registration")]
		[Required]
		public string RegistrationId { get; set; }
		public string ClientId
		{
			get
			{
				return Registration.SplitRegistrationId(RegistrationId).ClientId;
			}
		}
		public string DataHolderBrandId
		{
			get
			{
				return Registration.SplitRegistrationId(RegistrationId).DataHolderBrandId;
			}
		}

		public string DataHolderInfosecBaseUri { get; set; }

		public string RedirectUris { get; set; }

		[Display(Name = "Sharing Duration")]
		public int? SharingDuration { get; set; }

		[Display(Name = "Scope")]
		public string Scope { get; set; }

		[Display(Name = "Use PKCE")]
		public bool UsePkce { get; set; }

		public IEnumerable<SelectListItem> RegistrationListItems { get; set; }

	}
}
