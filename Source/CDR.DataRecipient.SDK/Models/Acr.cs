using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class Acr
    {
        [JsonProperty(PropertyName = "essential")]
        public bool Essential { get; set; }

        [JsonProperty(PropertyName = "values")]
        public string[] Values { get; set; }
    }
}
