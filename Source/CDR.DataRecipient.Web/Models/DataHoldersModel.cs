using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Web.Models
{
    public class DataHoldersModel
    {
        public IEnumerable<DataHolderBrand> DataHolders { get; set; }

        [Display(Name = "Version")]
        public string Version { get; set; }

        public HttpRequestModel RefreshRequest { get; set; }

        public string Messages { get; set; }

        public DataHoldersModel()
        {
            this.DataHolders = new List<DataHolderBrand>();
        }
    }
}
