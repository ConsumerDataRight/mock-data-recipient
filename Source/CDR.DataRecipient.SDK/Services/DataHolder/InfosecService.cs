using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Models.AuthorisationRequest;
using CDR.DataRecipient.SDK.Services.Tokens;
using Jose;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static CDR.DataRecipient.SDK.Constants;

namespace CDR.DataRecipient.SDK.Services.DataHolder
{
    public class InfosecService : BaseService, IInfosecService
    {
        private readonly IAccessTokenService _accessTokenService;

        public InfosecService(
            IConfiguration config,
            ILogger<InfosecService> logger,
            IAccessTokenService accessTokenService,
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
            _accessTokenService = accessTokenService;
        }

        public async Task<Response<OidcDiscovery>> GetOidcDiscovery(
            string infosecBaseUri)
        {
            var oidcResponse = new Response<OidcDiscovery>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetOidcDiscovery)}.");

            var client = GetHttpClient();
            var configUrl = string.Concat(infosecBaseUri.TrimEnd('/'), "/.well-known/openid-configuration");
            var configResponse = await client.GetAsync(EnsureValidEndpoint(configUrl));

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
                additionalFormFields: formFields,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

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

        public string BuildAuthorisationRequestJwt(AuthorisationRequestJwt authorisationRequestJwt)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(BuildAuthorisationRequestJwt)}.");

            // Build the list of claims to include in the authorisation request jwt.
            var authorisationRequestClaims = new Dictionary<string, object>
            {
                { "response_type", authorisationRequestJwt.ResponseType },
                { "client_id", authorisationRequestJwt.ClientId },
                { "redirect_uri", authorisationRequestJwt.RedirectUri },
                { "response_mode", authorisationRequestJwt.ResponseMode },
                { "scope", authorisationRequestJwt.Scope },
                { "state", authorisationRequestJwt.State },
                { "nonce", authorisationRequestJwt.Nonce },
                {
                    "claims", JsonSerializer.SerializeToElement(new AuthorisationRequestClaims(authorisationRequestJwt.AcrValueSupported)
                                { sharing_duration = authorisationRequestJwt.SharingDuration, cdr_arrangement_id = authorisationRequestJwt.CdrArrangementId })
                },
            };

            if (authorisationRequestJwt.Pkce != null)
            {
                authorisationRequestClaims.Add("code_challenge", authorisationRequestJwt.Pkce.CodeChallenge);
                authorisationRequestClaims.Add("code_challenge_method", authorisationRequestJwt.Pkce.CodeChallengeMethod);
            }

            return authorisationRequestClaims.GenerateJwt(authorisationRequestJwt.ClientId, authorisationRequestJwt.InfosecBaseUri, authorisationRequestJwt.SigningCertificate);
        }

        public async Task<string> BuildAuthorisationRequestUri(
            string infosecBaseUri,
            string clientId,
            X509Certificate2 signingCertificate,
            string requestUri,
            string scope,
            string responseType = "code")
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(BuildAuthorisationRequestUri)}.");

            var config = (await GetOidcDiscovery(infosecBaseUri)).Data;

            string authRequestUri = config.AuthorizationEndpoint
                .AppendQueryString("client_id", clientId)
                .AppendQueryString("scope", scope)
                .AppendQueryString("response_type", responseType)
                .AppendQueryString("request_uri", requestUri);

            return authRequestUri;
        }

        public async Task<Response<Token>> GetAccessToken(AccessToken accessToken)
        {
            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(GetAccessToken)}.");

            return await _accessTokenService.GetAccessToken(accessToken);
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
                grantType: TokenTypes.REFRESH_TOKEN,
                additionalFormFields: formFields,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

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
                scope: string.Empty,
                additionalFormFields: formFields,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

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
            formFields.Add("token_type_hint", TokenTypes.REFRESH_TOKEN);

            var response = await client.SendPrivateKeyJwtRequest(
                introspectionEndpoint,
                signingCertificate,
                issuer: clientId,
                clientId: clientId,
                scope: string.Empty,
                additionalFormFields: formFields,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

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

        public async Task<Response<Models.UserInfo>> UserInfo(
            string userInfoEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken)
        {
            var userInfoResponse = new Response<Models.UserInfo>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(UserInfo)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            var response = await client.GetAsync(EnsureValidEndpoint(userInfoEndpoint));
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
                scope: string.Empty,
                additionalFormFields: formFields,
                enforceHttpsEndpoint: _serviceConfiguration.EnforceHttpsEndpoints);

            var body = await response.Content.ReadAsStringAsync();

            revocationResponse.StatusCode = response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                revocationResponse.Message = body;
            }

            return revocationResponse;
        }

        public async Task<Response<Models.UserInfo>> PushedAuthorizationRequest(
            string parEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken)
        {
            var parResponse = new Response<Models.UserInfo>();

            _logger.LogDebug($"Request received to {nameof(InfosecService)}.{nameof(PushedAuthorizationRequest)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            var response = await client.GetAsync(EnsureValidEndpoint(parEndpoint));
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
                CodeVerifier = string.Concat(System.Guid.NewGuid().ToString(), '-', System.Guid.NewGuid().ToString()),
            };

            var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(pkce.CodeVerifier));
            pkce.CodeChallenge = Base64Url.Encode(challengeBytes);

            return pkce;
        }
    }
}
