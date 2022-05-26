using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Enumerations;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public interface ISsaService
    {
        Task<Response<string>> GetSoftwareStatementAssertion(
            string mtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string brandId,
            string softwareProductId,
            Industry industry);
    }
}
