using System;
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

        public string KVSecretsStorePath { get; set; }

        public X509Certificate2 X509Certificate
        {
            get
            {
                // if we already have a cert return it. But for KVSecretsStorePath we always fetch a new one incase cert was updated.
                if (this._certificate != null && string.IsNullOrEmpty(this.KVSecretsStorePath))
                {
                    return this._certificate;
                }

                if (!string.IsNullOrEmpty(this.Path) && !string.IsNullOrEmpty(this.Password))
                {
                    this._certificate = new X509Certificate2(this.Path, this.Password, X509KeyStorageFlags.Exportable);
                }
                else if (!string.IsNullOrEmpty(this.Url) && !string.IsNullOrEmpty(this.Password))
                {
                    // Retrieve the raw bytes from the URL value.
                    this._certificate = new X509Certificate2(DownloadData(this.Url), this.Password, X509KeyStorageFlags.Exportable);
                }
                else if (!string.IsNullOrEmpty(this.KVSecretsStorePath))
                {
                    var certString = System.IO.File.ReadAllText(this.KVSecretsStorePath);

                    // The cert loaded into the volume by secrets-store-csi-driver-provider-azure is in base64
                    var certBytes = Convert.FromBase64String(certString);

                    this._certificate = new X509Certificate2(certBytes, string.Empty, X509KeyStorageFlags.Exportable);
                }

                return this._certificate;
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
