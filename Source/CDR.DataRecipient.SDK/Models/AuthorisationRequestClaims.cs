using System.Text.Json.Serialization;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class AuthorisationRequestClaims
    {
        public AuthorisationRequestClaims(int supportedAcr)
        {
            this.Userinfo = new AuthorisationRequestClaimsUserInfo();
            this.Id_token = new IdToken(supportedAcr);
        }

        [JsonPropertyName("sharing_duration")]
        public int? Sharing_duration { get; set; }

        [JsonPropertyName("cdr_arrangement_id")]
        public string Cdr_arrangement_id { get; set; }

        [JsonPropertyName("userinfo")]
        public AuthorisationRequestClaimsUserInfo Userinfo { get; set; }

        [JsonPropertyName("id_token")]
        public IdToken Id_token { get; set; }
    }
}
