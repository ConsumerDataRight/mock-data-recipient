using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.SDK.Services.DataHolder
{
    public interface IInfosecService
    {
        Task<Response<OidcDiscovery>> GetOidcDiscovery(string infosecBaseUri);

        Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope = "cdr:registration",
            string redirectUri = null,
            string code = null,
            string grantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            Pkce pkce = null);

        Task<Response<Token>> RefreshAccessToken(
            string tokenEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string scope,
            string refreshToken,
            string redirectUri);

        Task<Response> RevokeToken(
            string tokenRevocationEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string tokenType,
            string token);

        Task<Response<Introspection>> Introspect(
            string introspectionEndpoint, 
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string refreshToken);

        Task<Response<UserInfo>> UserInfo(
            string userInfoEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken);

        Task<Response> RevokeCdrArrangement(
            string cdrArrangementRevocationEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string cdrArrangementId);

        Task<Response<PushedAuthorisation>> PushedAuthorisationRequest(
            string parEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string request);

        Task<string> BuildAuthorisationRequestUri(
            string infosecBaseUri,
            string clientId,
            string redirectUri,
            string scope,
            string state,
            string nonce,
            X509Certificate2 signingCertificate,
            int? sharingDuration = 0,
            Pkce pkce = null);

        Task<string> BuildAuthorisationRequestUri(
            string infosecBaseUri,
            string clientId,
            X509Certificate2 signingCertificate,
            string requestUri,
            string scope);

        string BuildAuthorisationRequestJwt(
            string infosecBaseUri,
            string clientId,
            string redirectUri,
            string scope,
            string state,
            string nonce,
            X509Certificate2 signingCertificate,
            int? sharingDuration = 0,
            string cdrArrangementId = null,
            string responseMode = "form_post",
            Pkce pkce = null);

        Pkce CreatePkceData();

    }
}
