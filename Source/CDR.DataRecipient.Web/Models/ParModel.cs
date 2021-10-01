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

        [Display(Name = "Registration")]
        [Required]
        public string ClientId { get; set; }

        [Display(Name = "CDR Arrangement")]
        public string CdrArrangementId { get; set; }

        [Display(Name = "Sharing Duration")]
        public int? SharingDuration { get; set; }

        [Display(Name = "Scope")]
        [Required]
        public string Scope { get; set; }

        public IEnumerable<SelectListItem> RegistrationListItems { get; set; }

        public IEnumerable<SelectListItem> ConsentArrangementListItems { get; set; }

        public string AuthorisationUri { get; set; }

        public PushedAuthorisation PushedAuthorisation { get; set; }
    }
}
