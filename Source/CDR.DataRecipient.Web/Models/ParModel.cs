using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.SDK.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CDR.DataRecipient.Web.Models
{
    public class ParModel : BaseModel
    {
        public IEnumerable<Registration> Registrations { get; set; }

        public IEnumerable<ConsentArrangement> ConsentArrangements { get; set; }

        /// <summary>
		/// This has the format of {ClientId}|||{DataHolderBrandId}
		/// </summary>
		[Display(Name = "Registration")]
        [Required(ErrorMessage = "Please select a registration")]
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

        [Display(Name = "CDR Arrangement")]
        public string CdrArrangementId { get; set; }

        [Display(Name = "Sharing Duration")]
        public int? SharingDuration { get; set; }

        [Display(Name = "Scope")]
        [Required]
        public string Scope { get; set; }

        [Display(Name = "Use PKCE")]
        public bool UsePkce { get; set; }

        [Display(Name = "Response Type")]
        public string ResponseType { get; set; }

        [Display(Name = "Response Mode")]
        public string ResponseMode { get; set; }
        
        public IEnumerable<SelectListItem> RegistrationListItems { get; set; }

        public IEnumerable<SelectListItem> ConsentArrangementListItems { get; set; }

        public string AuthorisationUri { get; set; }

        public PushedAuthorisation PushedAuthorisation { get; set; }
    }
}
