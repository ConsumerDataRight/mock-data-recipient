using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class AuthorisationRequestClaims
    {
        public AuthorisationRequestClaims(int supportedAcr)
        {
            this.Userinfo = new AuthorisationRequestClaimsUserInfo();
            this.Id_token = new IdToken(supportedAcr);
        }

        [JsonProperty("sharing_duration")]
        public int? Sharing_duration { get; set; }

        [JsonProperty("cdr_arrangement_id")]
        public string Cdr_arrangement_id { get; set; }

        [JsonProperty("userinfo")]
        public AuthorisationRequestClaimsUserInfo Userinfo { get; set; }

        [JsonProperty("id_token")]
        public IdToken Id_token { get; set; }
    }
}
