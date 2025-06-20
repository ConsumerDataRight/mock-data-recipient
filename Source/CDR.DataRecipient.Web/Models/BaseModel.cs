using System.Net;
using Newtonsoft.Json;

namespace CDR.DataRecipient.Web.Models
{
    public abstract class BaseModel
    {
        protected BaseModel()
        {
            this.ErrorList = new SDK.Models.ErrorList();
        }

        [JsonProperty(Required = Required.Always)]
        public HttpStatusCode StatusCode { get; set; }

        public string Messages { get; set; }

        public SDK.Models.ErrorList ErrorList { get; set; }
    }
}
