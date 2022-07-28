using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public interface IInfosecService
    {
        Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope = Constants.Scopes.CDR_REGISTER);

        Task<Response<OidcDiscovery>> GetOidcDiscovery(string registerOidcConfigEndpoint);

        Task<string> GetTokenEndpoint(string registerOidcConfigEndpoint);

    }
}
