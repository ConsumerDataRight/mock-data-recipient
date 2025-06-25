using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class JsonWebKey
    {
        [JsonProperty("alg")]
        public string Alg { get; set; }

        [JsonProperty("e")]
        public string E { get; set; }

        [JsonProperty("use")]
        public string Use { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("kty")]
        public string Kty { get; set; }

        [JsonProperty("n")]
        public string N { get; set; }

        [JsonProperty("d")]
        public string D { get; set; }
    }
}
