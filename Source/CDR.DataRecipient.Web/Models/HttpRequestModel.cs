using System.Collections.Generic;

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

        public bool RequiresClientCertificate { get; set; }

        public bool RequiresAccessToken { get; set; }

        public bool SupportsVersion { get; set; }
    }
}
