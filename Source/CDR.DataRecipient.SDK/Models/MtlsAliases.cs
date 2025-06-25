using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class MtlsAliases
    {
        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set; }

        [JsonProperty("revocation_endpoint")]
        public string RevocationEndpoint { get; set; }

        [JsonProperty("introspection_endpoint")]
        public string IntrospectionEndpoint { get; set; }

        [JsonProperty("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; }

        [JsonProperty("registration_endpoint")]
        public string RegistrationEndpoint { get; set; }

        [JsonProperty("pushed_authorization_request_endpoint")]
        public string PushedAuthorizationRequestEndpoint { get; set; }
    }
}
