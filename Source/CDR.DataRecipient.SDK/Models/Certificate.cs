using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Models
{
    public class Certificate
    {
        private X509Certificate2 _certificate;

        public string Path { get; set; }
        public string Url { get; set; }
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

                if (!string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Password))
                {
                    // Retrieve the raw bytes from the URL value.
                    _certificate = new X509Certificate2(DownloadData(Url), Password, X509KeyStorageFlags.Exportable);
                }

                return _certificate;
            }
        }

        private static byte[] DownloadData(string url)
        {
            using (var http = new HttpClient())
            {
                byte[] result = null;
                Task.Run(async () => result = await http.GetByteArrayAsync(url)).Wait();
                return result;
            }
        }
    }
}