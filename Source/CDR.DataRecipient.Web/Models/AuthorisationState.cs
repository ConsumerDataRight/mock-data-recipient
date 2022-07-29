using CDR.DataRecipient.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Models
{
    public class AuthorisationState
    {
        public string StateKey { get; set; }
        public string DataHolderInfosecBaseUri { get; set; }
        public string DataHolderBrandId { get; set; }
        public string ClientId { get; set; }
        public string Scope { get; set; }
        public int? SharingDuration { get; set; }
        public string RedirectUri { get; set; }
        public string UserId { get; set; }
        public Pkce Pkce { get; set; }
    }
}
