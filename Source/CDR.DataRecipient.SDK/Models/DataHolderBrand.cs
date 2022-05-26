using System;
using System.Collections.Generic;

namespace CDR.DataRecipient.SDK.Models
{
    public class DataHolderBrand
    {
        public string DataHolderBrandId { get; set; }
        public string BrandName { get; set; }
        public LegalEntity LegalEntity { get; set; }
        public string Status { get; set; }
        public EndpointDetail EndpointDetail { get; set; }
        public IList<AuthDetail> AuthDetails { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}