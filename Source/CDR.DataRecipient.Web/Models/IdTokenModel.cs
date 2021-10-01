using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CDR.DataRecipient.Web.Models
{
    public class IdTokenModel : BaseModel
    {
        [Display(Name = "ID Token Encrypted")]
        public string IdTokenEncrypted { get; set; }

        [Display(Name = "ID Token Decrypted")]
        public string IdTokenDecrypted { get; set; }

        [Display(Name = "ID Token Claims")]
        public IEnumerable<Claim> IdTokenClaims { get; set; }
    }
}
