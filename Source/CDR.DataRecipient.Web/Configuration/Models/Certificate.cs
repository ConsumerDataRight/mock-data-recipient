using System.Security.Cryptography.X509Certificates;

namespace CDR.DataRecipient.Web.Configuration.Models
{
    public class Certificate
    {
        private X509Certificate2 _certificate;

        public string Path { get; set; }
        public string Password { get; set; }
        public X509Certificate2 X509Certificate
        { 
            get
            {
                if (_certificate != null)
                {
                    return _certificate;
                }

                if (!string.IsNullOrEmpty(Path) && !string.IsNullOrEmpty(Password))
                {
                    _certificate = new X509Certificate2(Path, Password, X509KeyStorageFlags.Exportable);
                }

                return _certificate;
            }
        }
    }
}
