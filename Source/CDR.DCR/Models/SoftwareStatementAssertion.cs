using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace CDR.DCR.Models
{
    public class SoftwareStatementAssertion
    {        
        public string SsaEndpoint { get; set; }
        public string Version { get; set; }
        public string AccessToken { get; set; }
        public X509Certificate2 ClientCertificate { get; set; }
        public string BrandId { get; set; }
        public string SoftwareProductId { get; set; }
        public ILogger Log { get; set; }
        public bool IgnoreServerCertificateErrors { get; set; } = false;   
    }
}
