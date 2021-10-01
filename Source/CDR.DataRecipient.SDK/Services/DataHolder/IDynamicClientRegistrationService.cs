using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.SDK.Services.DataHolder
{
    public interface IDynamicClientRegistrationService
    {
        Task<DcrResponse> Register(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string payload);

        Task<DcrResponse> GetRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId);

        Task<DcrResponse> UpdateRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId,
            string payload);

        Task<DcrResponse> DeleteRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId);
    }
}
