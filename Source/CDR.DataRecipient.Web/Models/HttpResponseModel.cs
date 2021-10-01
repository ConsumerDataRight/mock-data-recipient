using System.Collections.Generic;

namespace CDR.DataRecipient.Web.Models
{
    public class HttpResponseModel
    {
        public string StatusCode { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }
}
