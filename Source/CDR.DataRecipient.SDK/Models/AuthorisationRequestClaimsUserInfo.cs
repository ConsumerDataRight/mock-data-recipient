using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class AuthorisationRequestClaimsUserInfo
    {
        [JsonProperty("given_name")]
        public string Given_name { get; set; }

        [JsonProperty("family_name")]
        public string Family_name { get; set; }
    }
}
