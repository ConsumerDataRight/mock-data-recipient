using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class JsonWebKeySet
    {
        [JsonProperty("keys")]
        public JsonWebKey[] Keys { get; set; }
    }
}
