using System.Text.Json.Serialization;

namespace CDR.DataRecipient.SDK.Models.AuthorisationRequest
{
    public class IdToken
    {
        public IdToken(int supportedAcr)
        {
            this.Acr = new Acr() { Essential = true, Values = new string[] { $"urn:cds.au:cdr:{supportedAcr}" } };
        }

        [JsonPropertyName("acr")]
        public Acr Acr { get; set; }
    }
}
