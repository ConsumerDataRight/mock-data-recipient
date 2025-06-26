using System;
using System.Collections.Generic;

namespace CDR.DataRecipient.SDK.Models
{
    public class DRBrand
    {
        public string DataRecipientBrandId { get; set; }

        public string BrandName { get; set; }

        public string LogoUri { get; set; }

        public List<DRProduct> SoftwareProducts { get; set; }

        public string Status { get; set; }
    }
}
