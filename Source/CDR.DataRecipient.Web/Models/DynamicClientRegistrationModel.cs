using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CDR.DataRecipient.SDK.Enumerations;
using CDR.DataRecipient.SDK.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CDR.DataRecipient.Web.Models
{
    public class DynamicClientRegistrationModel : BaseModel
    {
        public IEnumerable<Registration> Registrations { get; set; }

        [Display(Name = "SSA Version")]
        public string SsaVersion { get; set; }

        public string ResponsePayload { get; set; }

        public Industry Industry { get; set; }

        [Display(Name = "Client ID", Prompt = "Only populate for update operations")]
        public string ClientId { get; set; }

        [Display(Name = "DH Brand Name")]
        public string DataHolderBrandId { get; set; }

        [Display(Name = "Software Product ID")]
        public string SoftwareProductId { get; set; }

        [Display(Name = "DR Brand ID")]
        public string DataRecipientBrandId { get; set; }

        public string Scope { get; set; }

        [Display(Name = "Redirect URIs")]
        public string RedirectUris { get; set; }

        [Display(Name = "Token Endpoint Auth Signing Alg")]
        public string TokenEndpointAuthSigningAlg { get; set; }

        [Display(Name = "Token Endpoint Auth Method")]
        public string TokenEndpointAuthMethod { get; set; }

        [Display(Name = "Grant Types")]
        public string GrantTypes { get; set; }

        [Display(Name = "Response Types")]
        public string ResponseTypes { get; set; }

        [Display(Name = "Application Type")]
        public string ApplicationType { get; set; }

        [Display(Name = "Id Token Signed Response Alg")]
        public string IdTokenSignedResponseAlg { get; set; }

        [Display(Name = "Id Token Encrypted Response Alg")]
        public string IdTokenEncryptedResponseAlg { get; set; }

        [Display(Name = "Id Token Encrypted Response Enc")]
        public string IdTokenEncryptedResponseEnc { get; set; }

        [Display(Name = "Request Object Signing Alg")]
        public string RequestObjectSigningAlg { get; set; }

        public List<SelectListItem> DataHolderBrands { get; set; }
        public List<SelectListItem> DataRecipients { get; set; }

        public string TransactionType
        {
            get
            {
                if (!string.IsNullOrEmpty(this.ClientId))
                {
                    return "Update";
                }

                return "Create";
            }
        }

        public DynamicClientRegistrationModel()
        {
            this.Registrations = new List<Registration>();
        }
    }
}
