
using System.Security.Cryptography.X509Certificates;

namespace CDR.DCR.Models
{
    public class DcrRequest
    {        
        public string SoftwareProductId { get; set; }
        public string RedirectUris { get; set; }
        public string Ssa { get; set; }
        public string Audience { get; set; }
        public string[] ResponseTypesSupported { get; set; }
        public string[] AuthorizationSigningResponseAlgValuesSupported { get; set; }
        public string[] AuthorizationEncryptionResponseEncValuesSupported { get; set; }
        public string[] AuthorizationEncryptionResponseAlgValuesSupported { get; set; }
        public X509Certificate2 SignCertificate { get; set; }
    }
}
