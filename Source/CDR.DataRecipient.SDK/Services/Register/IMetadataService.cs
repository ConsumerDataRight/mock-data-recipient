using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Enumerations;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public interface IMetadataService
    {
        Task<(string RespBody, System.Net.HttpStatusCode StatusCode, string Reason)> GetDataHolderBrands(
            string registerMtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string softwareProductId,
            Industry industry,
            int? page = null,
            int? pageSize = null);

        Task<(string RespBody, System.Net.HttpStatusCode StatusCode, string Reason)> GetDataRecipients(
            string registerTlsBaseUri,
            string version,
            Industry industry);
    }
}
