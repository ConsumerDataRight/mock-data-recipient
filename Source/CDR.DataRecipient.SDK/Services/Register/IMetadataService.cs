using CDR.DataRecipient.SDK.Enumerations;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public interface IMetadataService
    {
        Task<(string, System.Net.HttpStatusCode, string)> GetDataHolderBrands(
            string registerMtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string softwareProductId,
            Industry industry,
            int? page = null,
            int? pageSize = null);

        Task<(string, System.Net.HttpStatusCode, string)> GetDataRecipients(
            string registerTlsBaseUri,
            string version,
            Industry industry);
    }
}
