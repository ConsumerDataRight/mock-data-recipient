using System.Security.Cryptography.X509Certificates;

namespace CDR.DataRecipient.SDK.Models
{
    public class AccessToken
    {        
       public string TokenEndpoint { get; set; }
       public string ClientId { get; set; }
       public X509Certificate2 ClientCertificate { get; set; }
       public X509Certificate2 SigningCertificate { get; set; }
       public string Scope { get; set; } = "cdr:registration";
       public string RedirectUri { get; set; } = null;
       public string Code { get; set; } = null;
       public string GrantType { get; set; } = Constants.GrantTypes.CLIENT_CREDENTIALS;
       public Pkce Pkce { get; set; } = null;
   }
}