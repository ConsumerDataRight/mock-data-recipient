using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CDR.DataRecipient.Web.Models
{
    public class PrivateKeyJwtModel : BaseModel
    {
        [Required]
        [Display(Name = "Key ID")]
        public string Kid { get; set; }

        [Required]
        [Display(Name = "Issuer")]
        public string Issuer { get; set; }

        [Required]
        [Display(Name = "Audience")]
        public string Audience { get; set; }

        [Display(Name = "Private Key")]
        [Required]
        public string PrivateKey { get; set; }

        [Display(Name = "Expiry Minutes")]
        [Required]
        public int ExpiryMinutes { get; set; }

        [Display(Name = "jti")]
        public string Jti { get; set; }

        public string ClientAssertion { get; set; }
        public IEnumerable<Claim> ClientAssertionClaims { get; set; }

    }
}
