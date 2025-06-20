using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataRecipient.Web.Common.Constants;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    [Route(Urls.ConsentUrl)]
    public class ConsentController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;
        private readonly IInfosecService _dhInfosecService;
        private readonly IRegistrationsRepository _registrationsRepository;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
        private readonly ILogger<ConsentController> _logger;

        public ConsentController(
            IConfiguration config,
            IDistributedCache cache,
            IInfosecService dhInfosecService,
            IRegistrationsRepository registrationsRepository,
            IConsentsRepository consentsRepository,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            ILogger<ConsentController> logger)
        {
            this._config = config;
            this._cache = cache;
            this._dhInfosecService = dhInfosecService;
            this._registrationsRepository = registrationsRepository;
            this._consentsRepository = consentsRepository;
            this._dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            this._logger = logger;
        }

        [HttpPost]
        [Route("registration/detail")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> RegistrationDetail(string registrationId)
        {
            // Return the software product detail.
            string message = string.Empty;
            string redirectUris = string.Empty;
            string scope = string.Empty;

            var registrationInfo = Registration.SplitRegistrationId(registrationId);
            Registration myResponse = await this._registrationsRepository.GetRegistration(registrationInfo.ClientId, registrationInfo.DataHolderBrandId);
            if (myResponse == null)
            {
                message = "Registration not found";
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var item in myResponse.RedirectUris)
                {
                    sb.Append(item);
                    sb.Append(' ');
                }

                redirectUris = sb.ToString().Trim();
                scope = myResponse.Scope;
            }

            return new JsonResult(new { message, redirectUris, scope }) { };
        }

        [HttpGet]
        [HttpPost]
        [Route("callback")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] ConsentCallbackGetModel getModel, [FromForm] ConsentCallbackPostModel postModel)
        {
            if (this.HttpContext.Request.Method.Equals("get", StringComparison.OrdinalIgnoreCase))
            {
                postModel = null;
            }
            else
            {
                getModel = null;
            }

            var model = new TokenModel();
            var sp = this._config.GetSoftwareProductConfig();

            (bool isvalid, string authCode, AuthorisationState authState, ErrorList errorList) = await this.ValidateCallback(getModel, postModel);

            if (errorList != null && errorList.Errors.Count > 0)
            {
                model.Messages = "An error has occurred.";
                model.ErrorList.Errors.AddRange(errorList.Errors);
            }

            if (isvalid)
            {
                // Request a token from the data holder.
                var tokenEndpoint = (await this._dataHolderDiscoveryCache.GetOidcDiscoveryByInfoSecBaseUri(authState.DataHolderInfosecBaseUri)).TokenEndpoint;

                var accessToken = new AccessToken()
                {
                    TokenEndpoint = tokenEndpoint,
                    ClientId = authState.ClientId,
                    ClientCertificate = sp.ClientCertificate.X509Certificate,
                    SigningCertificate = sp.SigningCertificate.X509Certificate,
                    Scope = string.Empty,
                    RedirectUri = authState.RedirectUri,
                    Code = authCode,
                    GrantType = "authorization_code",
                    Pkce = authState.Pkce,
                };

                model.TokenResponse = await this._dhInfosecService.GetAccessToken(accessToken);

                if (model.TokenResponse.IsSuccessful)
                {
                    // Save the consent arrangement.
                    var consentArrangement = new ConsentArrangement()
                    {
                        UserId = authState.UserId,
                        DataHolderBrandId = authState.DataHolderBrandId,
                        ClientId = authState.ClientId,
                        SharingDuration = authState.SharingDuration,
                        CdrArrangementId = model.TokenResponse.Data.CdrArrangementId ?? string.Empty,
                        IdToken = model.TokenResponse.Data.IdToken,
                        AccessToken = model.TokenResponse.Data.AccessToken ?? string.Empty,
                        RefreshToken = model.TokenResponse.Data.RefreshToken ?? string.Empty,
                        ExpiresIn = model.TokenResponse.Data.ExpiresIn,
                        Scope = model.TokenResponse.Data.Scope,
                        TokenType = model.TokenResponse.Data.TokenType,
                        CreatedOn = DateTime.UtcNow,
                    };

                    await this._consentsRepository.PersistConsent(consentArrangement);
                }
            }
            else
            {
                // Error state.
                var qs = HttpUtility.ParseQueryString(this.Request.QueryString.Value);
                model.Messages = "An error has occurred.";

                if (!string.IsNullOrEmpty(qs["title"]))
                {
                    model.ErrorList.Errors.Add(new SDK.Models.Error(qs["code"], qs["title"], qs["detail"]));
                }
            }

            return this.View(model);
        }

        [HttpGet]
        [Route("consents")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Consents()
        {
            var model = new ConsentsModel();
            model.ConsentArrangements = await this._consentsRepository.GetConsents(string.Empty, string.Empty, this.HttpContext.User.GetUserId(), string.Empty);
            return this.View(model);
        }

        [HttpGet]
        [Route("userinfo/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> UserInfo(string cdrArrangementId)
        {
            var reg = await this.GetUserInfo(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload,
            };

            return new JsonResult(response)
            {
                StatusCode = 200,
            };
        }

        [HttpGet]
        [Route("introspection/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Introspection(string cdrArrangementId)
        {
            var reg = await this.GetIntrospection(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload,
            };

            return new JsonResult(response)
            {
                StatusCode = 200,
            };
        }

        [HttpGet]
        [Route("revoke/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Revoke(string cdrArrangementId)
        {
            var reg = await this.RevokeArrangement(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload,
            };

            return new JsonResult(response)
            {
                StatusCode = 200,
            };
        }

        [HttpGet]
        [Route("revoke-token/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Revoke(string cdrArrangementId, [FromQuery] string tokenType)
        {
            var reg = await this.RevokeToken(cdrArrangementId, tokenType);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload,
            };

            return new JsonResult(response)
            {
                StatusCode = 200,
            };
        }

        [HttpGet]
        [Route("refresh/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Refresh(string cdrArrangementId)
        {
            var reg = await this.RefreshAccessToken(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload,
            };

            return new JsonResult(response)
            {
                StatusCode = 200,
            };
        }

        [HttpDelete]
        [Route("consents/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Delete(string cdrArrangementId)
        {
            await this._consentsRepository.DeleteConsent(cdrArrangementId);

            var response = new
            {
                StatusCode = 204,
                Messages = "CDR Arrangement deleted",
            };

            return new JsonResult(response)
            {
                StatusCode = 200,
            };
        }

        // Validating JARM token
        private async Task<(bool Isvalid, string AuthCode, AuthorisationState AuthState, ErrorList ErrorList)> ValidateJARMToken(string responseToken)
        {
            bool isValid = false;
            string authCode = string.Empty;
            string state = string.Empty;
            AuthorisationState authState = null;
            ErrorList errorList = new ErrorList();

            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(responseToken))
            {
                try
                {
                    var token = handler.ReadJwtToken(responseToken);

                    // Check if the token is encrypted
                    if (!string.IsNullOrEmpty(token.Header.Enc))
                    {
                        var failedDecryptionError = new Error(code: string.Empty, title: ErrorTitles.InvalidResponse, detail: ErrorDescription.FailedDecryption);

                        // Load the signing certificate and make sure the keys match
                        var sp = this._config.GetSoftwareProductConfig();
                        var encryptionKeys = sp.SigningCertificate.X509Certificate.GetEncryptionCredentials();
                        if (!encryptionKeys.TryGetValue(token.Header.Kid, out var encryptionKey) || token.Header.Alg != encryptionKey.Enc)
                        {
                            isValid = false;
                            errorList.Errors.Add(failedDecryptionError);
                            return (isValid, authCode, authState, errorList);
                        }

                        // Decrypt the token
                        var decryptedToken = responseToken.DecryptToken(sp.SigningCertificate.X509Certificate);
                        if (!handler.CanReadToken(decryptedToken))
                        {
                            isValid = false;
                            errorList.Errors.Add(failedDecryptionError);
                            return (isValid, authCode, authState, errorList);
                        }

                        responseToken = decryptedToken;
                        token = handler.ReadJwtToken(responseToken);
                    }

                    state = token.Claims.FirstOrDefault(x => x.Type == "state")?.Value ?? string.Empty;
                    if (string.IsNullOrEmpty(state))
                    {
                        isValid = false;
                        errorList.Errors.Add(new Error(code: string.Empty, title: ErrorTitles.MissingField, detail: ErrorDescription.MissingState));
                        return (isValid, authCode, authState, errorList);
                    }

                    authState = await this._cache.GetAsync<AuthorisationState>(state);

                    if (authState == null)
                    {
                        isValid = false;
                        errorList.Errors.Add(new Error(code: string.Empty, title: ErrorTitles.MissingField, detail: ErrorDescription.MissingAuthState));
                        return (isValid, authCode, authState, errorList);
                    }

                    // Validate token against JWKS of the Data holder
                    var dataholderDiscoveryDocument = await this._dataHolderDiscoveryCache.GetOidcDiscoveryByInfoSecBaseUri(authState.DataHolderInfosecBaseUri);
                    if (dataholderDiscoveryDocument == null)
                    {
                        isValid = false;
                        errorList.Errors.Add(new Error(code: string.Empty, title: ErrorTitles.MissingField, detail: ErrorDescription.MissingDiscoveryDocument));
                        return (isValid, authCode, authState, errorList);
                    }

                    this._logger.LogDebug("Validating token against {JwksUri}.", dataholderDiscoveryDocument.JwksUri);

                    // Validate the token
                    var validated = await responseToken.ValidateToken(
                        dataholderDiscoveryDocument.JwksUri,
                        this._logger,
                        dataholderDiscoveryDocument.Issuer,
                        new[] { authState.ClientId },
                        validateLifetime: true,
                        acceptAnyServerCertificate: this._config.IsAcceptingAnyServerCertificate());

                    this._logger.LogDebug("Validated token: {IsValid}.", validated.IsValid);
                    isValid = validated.IsValid;

                    var errorTitle = token.Claims.FirstOrDefault(x => x.Type == "error")?.Value ?? validated.ValidationError?.Title ?? string.Empty;
                    var errorDescription = token.Claims.FirstOrDefault(x => x.Type == "error_description")?.Value ?? validated.ValidationError?.Detail ?? string.Empty;
                    var errorCode = validated.ValidationError?.Code;

                    // Error description
                    if (!string.IsNullOrEmpty(errorTitle))
                    {
                        errorList.Errors.Add(new Error(code: errorCode, title: errorTitle, detail: errorDescription));
                        isValid = false;
                        return (isValid, authCode, authState, errorList);
                    }

                    authCode = token.Claims.FirstOrDefault(x => x.Type == "code")?.Value ?? string.Empty;

                    if (string.IsNullOrEmpty(authCode))
                    {
                        isValid = false;
                        errorList.Errors.Add(new Error(code: string.Empty, title: ErrorTitles.MissingField, detail: ErrorDescription.MissingAuthCode));
                        return (isValid, authCode, authState, errorList);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "An error occurred validating the JARM token");
                    var (errorCode, errorTitle, errorDescription) = ex.Message.ParseErrorString("Token Validation Error", "error", ex.Message);
                    errorList.Errors.Add(new Error(code: errorCode, title: errorTitle, detail: errorDescription));

                    return (false, authCode, authState, errorList);
                }
            }

            return (isValid, authCode, authState, errorList);
        }

        private async Task<(bool Isvalid, string AuthCode, AuthorisationState AuthState, ErrorList ErrorList)> ValidateCallback(ConsentCallbackGetModel getModel, ConsentCallbackPostModel postModel)
        {
            bool isValid = false;
            string authCode = string.Empty;
            AuthorisationState authState = null;
            ErrorList errorList = new ErrorList();

            // Handle GET request
            if (getModel?.Response != null)
            {
                // code, jwt
                if (string.IsNullOrEmpty(getModel.Response))
                {
                    errorList.Errors.Add(new Error(string.Empty, ErrorTitles.MissingField, ErrorDescription.MissingResponse));
                    return (false, authCode, authState, errorList);
                }

                (isValid, authCode, authState, errorList) = await this.ValidateJARMToken(getModel.Response);
            }

            // Handle POST request
            if (postModel != null)
            {
                // Handle form_post with code/state
                if (!string.IsNullOrEmpty(postModel.Code))
                {
                    authCode = postModel.Code;

                    if (string.IsNullOrEmpty(postModel.State))
                    {
                        errorList.Errors.Add(new Error(string.Empty, ErrorTitles.MissingField, ErrorDescription.MissingState));
                        return (false, authCode, authState, errorList);
                    }

                    authState = await this._cache.GetAsync<AuthorisationState>(postModel.State);

                    if (authState == null)
                    {
                        errorList.Errors.Add(new Error(string.Empty, ErrorTitles.MissingField, ErrorDescription.MissingAuthState));
                        return (false, authCode, authState, errorList);
                    }

                    return (true, authCode, authState, errorList);
                }

                // Handle JARM response
                if (string.IsNullOrEmpty(postModel.Response))
                {
                    isValid = false;
                    errorList.Errors.Add(new Error(code: string.Empty, title: ErrorTitles.MissingField, detail: ErrorDescription.MissingResponse));
                    return (isValid, authCode, authState, errorList);
                }

                if (!string.IsNullOrEmpty(postModel.Response))
                {
                    return await this.ValidateJARMToken(postModel.Response);
                }

                // Handle error response
                if (!string.IsNullOrEmpty(postModel.Error))
                {
                    errorList.Errors.Add(new Error(postModel.ErrorCode, postModel.Error, postModel.ErrorDescription));
                    return (false, authCode, authState, errorList);
                }
            }

            return (isValid, authCode, authState, errorList);
        }

        private async Task<ResponseModel> RefreshAccessToken(string cdrArrangementId)
        {
            var sp = this._config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await this._consentsRepository.GetConsentByArrangement(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to refresh the access token.
            var tokenEndpoint = (await this._dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).TokenEndpoint;
            var tokenResponse = await this._dhInfosecService.RefreshAccessToken(
                tokenEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                arrangement.Scope,
                arrangement.RefreshToken,
                sp.RedirectUri);

            if (tokenResponse.IsSuccessful)
            {
                await this._consentsRepository.UpdateTokens(arrangement.CdrArrangementId, tokenResponse.Data.IdToken, tokenResponse.Data.AccessToken, tokenResponse.Data.RefreshToken);
            }

            return new ResponseModel()
            {
                StatusCode = tokenResponse.StatusCode,
                Messages = tokenResponse.Message,
                Payload = tokenResponse.Data == null ? null : JsonConvert.SerializeObject(tokenResponse.Data),
            };
        }

        private async Task<ResponseModel> RevokeArrangement(string cdrArrangementId)
        {
            var sp = this._config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await this._consentsRepository.GetConsentByArrangement(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to revoke the arrangement.
            var revocationEndpoint = (await this._dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).CdrArrangementRevocationEndpoint;
            var revocation = await this._dhInfosecService.RevokeCdrArrangement(
                revocationEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                arrangement.CdrArrangementId);

            // The consent has been revoked, so remove from the local repository.
            if (revocation.IsSuccessful)
            {
                await this._consentsRepository.DeleteConsent(arrangement.CdrArrangementId);
            }

            return new ResponseModel()
            {
                StatusCode = revocation.StatusCode,
                Messages = revocation.Message,
            };
        }

        private async Task<ResponseModel> GetIntrospection(string cdrArrangementId)
        {
            var sp = this._config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await this._consentsRepository.GetConsentByArrangement(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            if (string.IsNullOrEmpty(arrangement.RefreshToken))
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "No refresh_token found for the CDR Arrangement ID.",
                };
            }

            // Call the DH to introspect the refresh token.
            var introspectEndpoint = (await this._dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).IntrospectionEndpoint;
            var introspection = await this._dhInfosecService.Introspect(
                introspectEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                arrangement.RefreshToken);

            return new ResponseModel()
            {
                StatusCode = introspection == null ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK,
                Messages = introspection == null ? "Failed to retrieve introspection details." : "Introspection details found.",
                Payload = introspection == null ? null : JsonConvert.SerializeObject(introspection),
            };
        }

        private async Task<ResponseModel> GetUserInfo(string cdrArrangementId)
        {
            var sp = this._config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await this._consentsRepository.GetConsentByArrangement(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            if (string.IsNullOrEmpty(arrangement.AccessToken))
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "No access_token found for the CDR Arrangement ID.",
                };
            }

            // Call the DH to get the userinfo for the access token.
            var userInfoEndpoint = (await this._dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).UserInfoEndpoint;
            var userInfo = await this._dhInfosecService.UserInfo(
                userInfoEndpoint,
                sp.ClientCertificate.X509Certificate,
                arrangement.AccessToken);

            return new ResponseModel()
            {
                StatusCode = userInfo == null ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK,
                Messages = userInfo == null ? "Failed to retrieve userinfo." : "userinfo details found.",
                Payload = userInfo == null ? null : JsonConvert.SerializeObject(userInfo),
            };
        }

        private async Task<ResponseModel> RevokeToken(string cdrArrangementId, string tokenType)
        {
            var sp = this._config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await this._consentsRepository.GetConsentByArrangement(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to revoke the token.
            var tokenRevocationEndpoint = (await this._dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).RevocationEndpoint;
            var revocation = await this._dhInfosecService.RevokeToken(
                tokenRevocationEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                tokenType,
                tokenType.Equals(SDK.Constants.TokenTypes.ACCESS_TOKEN, StringComparison.OrdinalIgnoreCase) ? arrangement.AccessToken : arrangement.RefreshToken);

            return new ResponseModel()
            {
                StatusCode = revocation.StatusCode,
                Messages = revocation.Message,
            };
        }
    }
}
