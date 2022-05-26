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

        [Display(Name = "Registration")]
        [Required]
        public string ClientId { get; set; }

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
