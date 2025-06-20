using System.Collections.Generic;
using Newtonsoft.Json;

namespace CDR.DataRecipient.Web.Models
{
    public class HttpRequestModel
    {
        public HttpRequestModel()
        {
            this.Headers = new Dictionary<string, string>();
            this.QueryParameters = new Dictionary<string, string>();
            this.FormParameters = new Dictionary<string, string>();
        }

        public string Method { get; set; }

        public string Url { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public IDictionary<string, string> QueryParameters { get; set; }

        public IDictionary<string, string> FormParameters { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool RequiresClientCertificate { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool RequiresAccessToken { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool SupportsVersion { get; set; }
    }
}
