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

    public class DRBrand
    {
        public string DataRecipientBrandId { get; set; }
        public string BrandName { get; set; }
        public string LogoUri { get; set; }
        public List<DRProduct> SoftwareProducts { get; set; }
        public string Status { get; set; }
    }

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
