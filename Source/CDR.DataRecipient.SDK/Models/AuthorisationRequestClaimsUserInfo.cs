using System.Text.Json.Serialization;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class AuthorisationRequestClaimsUserInfo
    {
        [JsonPropertyName("given_name")]
        public string Given_name { get; set; }

        [JsonPropertyName("family_name")]
        public string Family_name { get; set; }
    }
}
