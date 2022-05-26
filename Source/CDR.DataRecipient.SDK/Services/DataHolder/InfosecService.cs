using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Models.AuthorisationRequest;
using CDR.DataRecipient.SDK.Services.Tokens;
using Jose;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services.DataHolder
{
    public class InfosecService : BaseService, IInfosecService
    {

        private readonly IAccessTokenService _accessTokenService;

        public InfosecService(
            IConfiguration config,
            ILogger<InfosecService> logger,
            IAccessTokenService accessTokenService) : base(config, logger)
        {
            _accessTokenService = accessTokenService;
        }

        public async Task<Response<OidcDiscovery>> GetOidcDiscovery(string infosecBaseUri)
        {
            var oidcResponse = new Response<OidcDiscovery>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetOidcDiscovery)}.");

            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            var client = new HttpClient(clientHandler);

            var configUrl = string.Concat(infosecBaseUri.TrimEnd('/'), "/.well-known/openid-configuration");
            var configResponse = await client.GetAsync(configUrl);

            oidcResponse.StatusCode = configResponse.StatusCode;

            if (configResponse.IsSuccessStatusCode)
            {
                var body = await configResponse.Content.ReadAsStringAsync();
                oidcResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<OidcDiscovery>(body);
            }

            return oidcResponse;
        }

        public async Task<Response<PushedAuthorisation>> PushedAuthorisationRequest(
            string parEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string request)
        {
            var parResponse = new Response<PushedAuthorisation>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(PushedAuthorisationRequest)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            var formFields = new Dictionary<string, string>();
            formFields.Add("request", request);

            var response = await client.SendPrivateKeyJwtRequest(
                parEndpoint,
                signingCertificate,
                issuer: clientId,
                additionalFormFields: formFields);

            var body = await response.Content.ReadAsStringAsync();

            parResponse.StatusCode = response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                parResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<PushedAuthorisation>(body);
            }
            else
            {
                parResponse.Message = body;
            }

            return parResponse;
        }

        public string BuildAuthorisationRequestJwt(
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
            Pkce pkce = null)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(BuildAuthorisationRequestJwt)}.");

            // Build the list of claims to include in the authorisation request jwt.
            var authorisationRequestClaims = new Dictionary<string, object>
            {
                { "response_type", "code id_token" },
                { "client_id", clientId },
                { "redirect_uri", redirectUri },
                { "response_mode", responseMode},
                { "scope", scope },
                { "state", state },
                { "nonce", nonce },
                { "claims", new AuthorisationRequestClaims() { sharing_duration = sharingDuration, cdr_arrangement_id = cdrArrangementId } }
            };

            if (pkce != null)
            {
                authorisationRequestClaims.Add("code_challenge", pkce.CodeChallenge);
                authorisationRequestClaims.Add("code_challenge_method", pkce.CodeChallengeMethod);
            }

            return authorisationRequestClaims.GenerateJwt(clientId, infosecBaseUri, signingCertificate);
        }

        public async Task<string> BuildAuthorisationRequestUri(
            string infosecBaseUri,
            string clientId,
            string redirectUri,
            string scope,
            string state,
            string nonce,
            X509Certificate2 signingCertificate,
            int? sharingDuration = 0,
            Pkce pkce = null)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(BuildAuthorisationRequestUri)}.");

            var jwt = BuildAuthorisationRequestJwt(infosecBaseUri, clientId, redirectUri, scope, state, nonce, signingCertificate, sharingDuration, pkce: pkce);
            var config = (await GetOidcDiscovery(infosecBaseUri)).Data;

            var authRequestUri = config.AuthorizationEndpoint
                .AppendQueryString("client_id", clientId)
                //.AppendQueryString("response_type", "code id_token")
                //.AppendQueryString("scope", scope)
                //.AppendQueryString("response_mode", "form_post")
                .AppendQueryString("request", jwt);

            //if (pkce != null)
            //{
            //    authRequestUri = authRequestUri
            //        .AppendQueryString("code_challenge", pkce.CodeChallenge)
            //        .AppendQueryString("code_challenge_method", pkce.CodeChallengeMethod);
            //}

            return authRequestUri;
        }

        public async Task<string> BuildAuthorisationRequestUri(
            string infosecBaseUri,
            string clientId,
            X509Certificate2 signingCertificate,
            string requestUri)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(BuildAuthorisationRequestUri)}.");

            var config = (await GetOidcDiscovery(infosecBaseUri)).Data;

            string authRequestUri = config.AuthorizationEndpoint
                .AppendQueryString("client_id", clientId)
                .AppendQueryString("request_uri", requestUri);

            return authRequestUri;
        }

        public async Task<Response<Token>> GetAccessToken(
            string tokenEndpoint,
            string clientId,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string scope = Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
            string redirectUri = null,
            string code = null,
            string grantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            Pkce pkce = null)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetAccessToken)}.");

            return await _accessTokenService.GetAccessToken(
                tokenEndpoint, 
                clientId, 
                clientCertificate, 
                signingCertificate, 
                scope, 
                redirectUri, 
                code, 
                grantType,
                pkce);
        }

        public async Task<Response<Token>> RefreshAccessToken(
            string tokenEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string scope,
            string refreshToken,
            string redirectUri)
        {
            var tokenResponse = new Response<Token>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(RefreshAccessToken)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            var formFields = new Dictionary<string, string>();
            formFields.Add("refresh_token", refreshToken);
            formFields.Add("redirect_uri", redirectUri);

            var response = await client.SendPrivateKeyJwtRequest(
                tokenEndpoint,
                signingCertificate,
                issuer: clientId,
                clientId: clientId,
                scope: scope,
                grantType: "refresh_token",
                additionalFormFields: formFields);

            var body = await response.Content.ReadAsStringAsync();

            tokenResponse.StatusCode = response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                tokenResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Token>(body);
            }
            else
            {
                tokenResponse.Message = body;
            }

            return tokenResponse;
        }

        public async Task<Response> RevokeToken(
            string tokenRevocationEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string tokenType,
            string token)
        {
            var revocationResponse = new Response();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(RevokeToken)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            var formFields = new Dictionary<string, string>();
            formFields.Add("token", token);
            formFields.Add("token_type_hint", tokenType);

            var response = await client.SendPrivateKeyJwtRequest(
                tokenRevocationEndpoint,
                signingCertificate,
                issuer: clientId,
                clientId: clientId,
                scope: "",
                additionalFormFields: formFields);

            var body = await response.Content.ReadAsStringAsync();

            revocationResponse.StatusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                revocationResponse.Message = body;
            }

            return revocationResponse;
        }

        public async Task<Response<Introspection>> Introspect(
            string introspectionEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string refreshToken)
        {
            var introspectionResponse = new Response<Introspection>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(Introspect)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            var formFields = new Dictionary<string, string>();
            formFields.Add("token", refreshToken);
            formFields.Add("token_type_hint", "refresh_token");

            var response = await client.SendPrivateKeyJwtRequest(
                introspectionEndpoint,
                signingCertificate,
                issuer: clientId,
                clientId: clientId,
                scope: "",
                additionalFormFields: formFields);

            var body = await response.Content.ReadAsStringAsync();

            introspectionResponse.StatusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                introspectionResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Introspection>(body);
            }
            else
            {
                introspectionResponse.Message = body;
            }

            return introspectionResponse;
        }

        public async Task<Response<Models.UserInfo>> UserInfo(string userInfoEndpoint, X509Certificate2 clientCertificate, string accessToken)
        {
            var userInfoResponse = new Response<Models.UserInfo>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(UserInfo)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            var response = await client.GetAsync(userInfoEndpoint);
            var body = await response.Content.ReadAsStringAsync();

            userInfoResponse.StatusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                userInfoResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.UserInfo>(body);
            }
            else
            {
                userInfoResponse.Message = body;
            }

            return userInfoResponse;
        }

        public async Task<Response> RevokeCdrArrangement(
            string cdrArrangementRevocationEndpoint,
            X509Certificate2 clientCertificate,
            X509Certificate2 signingCertificate,
            string clientId,
            string cdrArrangementId)
        {
            var revocationResponse = new Response();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(RevokeCdrArrangement)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            var formFields = new Dictionary<string, string>();
            formFields.Add("cdr_arrangement_id", cdrArrangementId);

            var response = await client.SendPrivateKeyJwtRequest(
                cdrArrangementRevocationEndpoint,
                signingCertificate,
                issuer: clientId,
                clientId: clientId,
                scope: "",
                additionalFormFields: formFields);

            var body = await response.Content.ReadAsStringAsync();

            revocationResponse.StatusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                revocationResponse.Message = body;
            }

            return revocationResponse;
        }

        public async Task<Response<Models.UserInfo>> PushedAuthorizationRequest(string parEndpoint, X509Certificate2 clientCertificate, string accessToken)
        {
            var parResponse = new Response<Models.UserInfo>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(PushedAuthorizationRequest)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            var response = await client.GetAsync(parEndpoint);
            var body = await response.Content.ReadAsStringAsync();

            parResponse.StatusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                parResponse.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.UserInfo>(body);
            }
            else
            {
                parResponse.Message = body;
            }

            return parResponse;
        }

        public Pkce CreatePkceData()
        {
            var pkce = new Pkce
            {
                CodeVerifier = string.Concat(System.Guid.NewGuid().ToString(), '-', System.Guid.NewGuid().ToString())
            };

            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pkce.CodeVerifier));
                pkce.CodeChallenge = Base64Url.Encode(challengeBytes);
            }

            return pkce;
        }
    }
}
