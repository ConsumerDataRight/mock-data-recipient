using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class PushedAuthorisation
    {
        [JsonProperty("request_uri")]
        public string RequestUri { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
