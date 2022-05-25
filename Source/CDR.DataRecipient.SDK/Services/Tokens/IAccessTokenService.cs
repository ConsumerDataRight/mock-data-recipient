using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.SDK.Services.Tokens
{
    public interface IAccessTokenService
    {
        Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope,
            string redirectUri = null,
            string code = null,
            string grantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            Pkce pkce = null);
    }
}
