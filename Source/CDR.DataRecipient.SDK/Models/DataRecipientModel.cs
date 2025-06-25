using System;
using System.Collections.Generic;

namespace CDR.DataRecipient.SDK.Models
{
    public class DataRecipientModel
    {
        public string LegalEntityId { get; set; }

        public string LegalEntityName { get; set; }

        public string Industry { get; set; }

        public string LogoUri { get; set; }

        public string Status { get; set; }

        public List<DRBrand> DataRecipientBrands { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
