using System;
using System.Collections.Generic;

namespace CDR.DataRecipient.SDK.Models
{
    public class DRProduct
    {
        public string SoftwareProductId { get; set; }

        public string SoftwareProductName { get; set; }

        public string SoftwareProductDescription { get; set; }

        public string LogoUri { get; set; }

        public string RecipientBaseUri { get; set; }

        public string RedirectUri { get; set; }

        public string JwksUri { get; set; }

        public string Scope { get; set; }

        public string Status { get; set; }
    }
}
