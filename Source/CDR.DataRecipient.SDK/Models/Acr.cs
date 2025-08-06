using System.Text.Json.Serialization;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class Acr
    {
        [JsonPropertyName("essential")]
        public bool Essential { get; set; }

        [JsonPropertyName("values")]
        public string[] Values { get; set; }
    }
}
