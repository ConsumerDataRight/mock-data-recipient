using System.Security.Cryptography.X509Certificates;

namespace CDR.DataRecipient.SDK.Models
{
    public class AuthorisationRequestJwt
    {
        public string InfosecBaseUri { get; set; }

        public string ClientId { get; set; }

        public string RedirectUri { get; set; }

        public string Scope { get; set; }

        public string State { get; set; }

        public string Nonce { get; set; }

        public X509Certificate2 SigningCertificate { get; set; }

        public int? SharingDuration { get; set; } = 0;

        public string CdrArrangementId { get; set; } = null;

        public string ResponseMode { get; set; }

        public Pkce Pkce { get; set; } = null;

        public int AcrValueSupported { get; set; } = 0;

        public string ResponseType { get; set; } = "code";
    }
}
