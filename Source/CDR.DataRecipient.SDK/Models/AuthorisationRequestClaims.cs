using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class AuthorisationRequestClaims
    {
        public AuthorisationRequestClaims()
        {
            this.userinfo = new UserInfo();
            this.id_token = new IdToken();
        }

        [JsonProperty("sharing_duration")]
        public int? sharing_duration { get; set; }

        [JsonProperty("cdr_arrangement_id")]
        public string cdr_arrangement_id { get; set; }

        [JsonProperty("userinfo")]
        public UserInfo userinfo { get; set; }

        [JsonProperty("id_token")]
        public IdToken id_token { get; set; }
    }

    public class UserInfo
    {
        [JsonProperty("given_name")]
        public string given_name { get; set; }

        [JsonProperty("family_name")]
        public string family_name { get; set; }
    }

    public class IdToken
    {
        public IdToken()
        {
            this.acr = new Acr() { essential = true, values = new string[] { "urn:cds.au:cdr:3" } };
        }

        [JsonProperty("acr")]
        public Acr acr { get; set; }
    }

    public class Acr
    {
        [JsonProperty(PropertyName = "essential")]
        public bool essential { get; set; }

        [JsonProperty(PropertyName = "values")]
        public string[] values { get; set; }
    }
}
