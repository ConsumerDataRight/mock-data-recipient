using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CDR.DataRecipient.SDK.Enumerations;
using CDR.DataRecipient.SDK.Models;
using Newtonsoft.Json;

namespace CDR.DataRecipient.Web.Models
{
    public class DataHoldersModel
    {
        public DataHoldersModel()
        {
            this.DataHolders = new List<DataHolderBrand>();
        }

        public IEnumerable<DataHolderBrand> DataHolders { get; set; }

        [Display(Name = "Version")]
        public string Version { get; set; }

        public HttpRequestModel RefreshRequest { get; set; }

        public string Messages { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Industry Industry { get; set; }
    }
}
