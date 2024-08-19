using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class AuthorisationRequestClaims
    {
        public AuthorisationRequestClaims(int supportedAcr)
        {
            this.userinfo = new UserInfo();
            this.id_token = new IdToken(supportedAcr);
        }

        [JsonProperty(nameof(sharing_duration))]
        public int? sharing_duration { get; set; }

        [JsonProperty(nameof(cdr_arrangement_id))]
        public string cdr_arrangement_id { get; set; }

        [JsonProperty(nameof(userinfo))]
        public UserInfo userinfo { get; set; }

        [JsonProperty(nameof(id_token))]
        public IdToken id_token { get; set; }
    }

    public class UserInfo
    {
        [JsonProperty(nameof(given_name))]
        public string given_name { get; set; }

        [JsonProperty(nameof(family_name))]
        public string family_name { get; set; }
    }

    public class IdToken
    {        
        public IdToken(int supportedAcr)
        {            
            this.acr = new Acr() { essential = true, values = new string[] { $"urn:cds.au:cdr:{supportedAcr}" } };
        }

        [JsonProperty(nameof(acr))]
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
