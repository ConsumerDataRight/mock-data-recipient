using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class IdToken
    {
        public IdToken(int supportedAcr)
        {
            this.Acr = new Acr() { Essential = true, Values = new string[] { $"urn:cds.au:cdr:{supportedAcr}" } };
        }

        [JsonProperty("acr")]
        public Acr Acr { get; set; }
    }
}
