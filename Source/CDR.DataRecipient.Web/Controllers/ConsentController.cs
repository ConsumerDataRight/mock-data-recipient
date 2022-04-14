using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataRecipient.SDK.Constants;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("consent")]
    public class ConsentController : Controller
    {
        private readonly ILogger<ConsentController> _logger;
        private readonly IConfiguration _config;
        private readonly IInfosecService _dhInfosecService;
        private readonly IRegistrationsRepository _registrationsRepository;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly IMemoryCache _cache;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;

        public ConsentController(
            IConfiguration config,
            ILogger<ConsentController> logger,
            IMemoryCache cache,
            IInfosecService dhInfosecService,
            IRegistrationsRepository registrationsRepository,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache)
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            _dhInfosecService = dhInfosecService;
            _registrationsRepository = registrationsRepository;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation($"GET request: {nameof(ConsentController)}.{nameof(Index)}");

            var model = new ConsentModel();
            SetDefaults(model);
            await EnsureModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ConsentModel model)
        {
            _logger.LogInformation($"POST request: {nameof(ConsentController)}.{nameof(Index)}");

            await EnsureModel(model);

            if (!ModelState.IsValid)
            {
                model.Messages = ModelState.GetErrorMessage();
                return View(model);
            }

            // Build the authorisation uri based on the selected client id.
            if (!string.IsNullOrEmpty(model.ClientId))
            {
                var sp = _config.GetSoftwareProductConfig();
                var client = model.Registrations.FirstOrDefault(c => c.ClientId == model.ClientId);
                var infosecBaseUri = await GetInfoSecBaseUri(client.DataHolderBrandId);

                var stateKey = Guid.NewGuid().ToString();
                var nonce = Guid.NewGuid().ToString();
                var redirectUri = sp.RedirectUri;

                _cache.Set(stateKey, new AuthorisationState()
                {
                    StateKey = stateKey,
                    ClientId = model.ClientId,
                    SharingDuration = model.SharingDuration,
                    Scope = model.Scope,
                    DataHolderBrandId = client.DataHolderBrandId,
                    DataHolderInfosecBaseUri = infosecBaseUri,
                    RedirectUri = redirectUri
                });

                model.AuthorisationUri = await _dhInfosecService.BuildAuthorisationRequestUri(
                    infosecBaseUri,
                    client.ClientId,
                    redirectUri,
                    model.Scope,
                    stateKey,
                    nonce,
                    sp.SigningCertificate.X509Certificate,
                    model.SharingDuration);
            }

            return View(model);
        }

        [HttpGet]
        [HttpPost]
        [Route("callback")]
        public async Task<IActionResult> Callback()
        {
            var model = new TokenModel();
            var isSuccessful = this.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase) && this.Request.Form != null && this.Request.Form.ContainsKey("id_token");
            
            if (isSuccessful)
            {
                var sp = _config.GetSoftwareProductConfig();
                var idToken = this.Request.Form["id_token"].ToString();
                var authCode = this.Request.Form["code"].ToString();
                var state = this.Request.Form["state"].ToString();
                var nonce = this.Request.Form["nonce"].ToString();

                var authState = _cache.Get<AuthorisationState>(state);

                // Request a token from the data holder.
                var tokenEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByInfoSecBaseUri(authState.DataHolderInfosecBaseUri)).MtlsEndpointAliases.TokenEndpoint;
                model.TokenResponse = await _dhInfosecService.GetAccessToken(
                    tokenEndpoint,
                    authState.ClientId,
                    sp.ClientCertificate.X509Certificate,
                    sp.SigningCertificate.X509Certificate,
                    "",
                    authState.RedirectUri,
                    authCode,
                    "authorization_code");

                if (model.TokenResponse.IsSuccessful)
                {
                    // Save the consent arrangement.
                    var consentArrangement = new ConsentArrangement()
                    {
                        DataHolderBrandId = authState.DataHolderBrandId,
                        ClientId = authState.ClientId,
                        SharingDuration = authState.SharingDuration,
                        CdrArrangementId = model.TokenResponse.Data.CdrArrangementId,
                        IdToken = model.TokenResponse.Data.IdToken,
                        AccessToken = model.TokenResponse.Data.AccessToken,
                        RefreshToken = model.TokenResponse.Data.RefreshToken,
                        ExpiresIn = model.TokenResponse.Data.ExpiresIn,
                        Scope = model.TokenResponse.Data.Scope,
                        TokenType = model.TokenResponse.Data.TokenType,
                        CreatedOn = DateTime.UtcNow
                    };

                    await _consentsRepository.PersistConsent(consentArrangement);
                }
            }
            else
            {
                // Error state.
                var qs = HttpUtility.ParseQueryString(this.Request.QueryString.Value);
                model.Messages = "An error has occurred.";
                model.ErrorList.Errors.Add(new SDK.Models.Error(qs["code"], qs["title"], qs["detail"]));
            }

            return View(model);
        }

        [HttpGet]
        [Route("consents")]
        public async Task<IActionResult> Consents()
        {
            var model = new ConsentsModel();
            model.ConsentArrangements = await _consentsRepository.GetConsents();
            return View(model);
        }

        [HttpGet]
        [Route("userinfo/{cdrArrangementId}")]
        public async Task<IActionResult> UserInfo(string cdrArrangementId)
        {
            _logger.LogInformation($"GET request: {nameof(ConsentController)}.{nameof(UserInfo)} - {cdrArrangementId}");

            var reg = await GetUserInfo(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload
            };

            return new JsonResult(response)
            {
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("introspection/{cdrArrangementId}")]
        public async Task<IActionResult> Introspection(string cdrArrangementId)
        {
            _logger.LogInformation($"GET request: {nameof(ConsentController)}.{nameof(Introspection)} - {cdrArrangementId}");

            var reg = await GetIntrospection(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload
            };

            return new JsonResult(response)
            {
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("revoke/{cdrArrangementId}")]
        public async Task<IActionResult> Revoke(string cdrArrangementId)
        {
            _logger.LogInformation($"GET request: {nameof(ConsentController)}.{nameof(Revoke)} - {cdrArrangementId}");

            var reg = await RevokeArrangement(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload
            };

            return new JsonResult(response)
            {
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("revoke-token/{cdrArrangementId}")]
        public async Task<IActionResult> Revoke(string cdrArrangementId, [FromQuery] string tokenType)
        {
            _logger.LogInformation($"GET request: {nameof(ConsentController)}.{nameof(Revoke)} - {cdrArrangementId}, {tokenType}");

            var reg = await RevokeToken(cdrArrangementId, tokenType);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload
            };

            return new JsonResult(response)
            {
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("refresh/{cdrArrangementId}")]
        public async Task<IActionResult> Refresh(string cdrArrangementId)
        {
            _logger.LogInformation($"GET request: {nameof(ConsentController)}.{nameof(RefreshAccessToken)} - {cdrArrangementId}");

            var reg = await RefreshAccessToken(cdrArrangementId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.Payload
            };

            return new JsonResult(response)
            {
                StatusCode = 200
            };
        }

        [HttpDelete]
        [Route("consents/{cdrArrangementId}")]
        public async Task<IActionResult> Delete(string cdrArrangementId)
        {
            _logger.LogInformation($"DELETE request: {nameof(ConsentController)}.{nameof(Delete)} - {cdrArrangementId}");

            await _consentsRepository.DeleteConsent(cdrArrangementId);

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

        private async Task<ResponseModel> RefreshAccessToken(string cdrArrangementId)
        {
            var sp = _config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await _consentsRepository.GetConsent(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to refresh the access token.
            var tokenEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).MtlsEndpointAliases.TokenEndpoint;
            var tokenResponse = await _dhInfosecService.RefreshAccessToken(
                tokenEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                arrangement.Scope,
                arrangement.RefreshToken,
                sp.RedirectUri);

            if (tokenResponse.IsSuccessful)
            {
                await _consentsRepository.UpdateTokens(arrangement.CdrArrangementId, tokenResponse.Data.IdToken, tokenResponse.Data.AccessToken, tokenResponse.Data.RefreshToken);
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
            var sp = _config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await _consentsRepository.GetConsent(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to revoke the arrangement.
            var revocationEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).CdrArrangementRevocationEndpoint;
            var revocation = await _dhInfosecService.RevokeCdrArrangement(
                revocationEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                arrangement.CdrArrangementId,
                arrangement.AccessToken);

            // The consent has been revoked, so remove from the local repository.
            if (revocation.IsSuccessful)
            {
                await _consentsRepository.DeleteConsent(arrangement.CdrArrangementId);
            }

            return new ResponseModel()
            {
                StatusCode = revocation.StatusCode,
                Messages = revocation.Message,
            };
        }

        private async Task<ResponseModel> GetIntrospection(string cdrArrangementId)
        {
            var sp = _config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await _consentsRepository.GetConsent(cdrArrangementId);
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
            var introspectEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).IntrospectionEndpoint;
            var introspection = await _dhInfosecService.Introspect(
                introspectEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                arrangement.RefreshToken,
                arrangement.AccessToken);

            return new ResponseModel()
            {
                StatusCode = introspection == null ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK,
                Messages = introspection == null ? "Failed to retrieve introspection details." : "Introspection details found.",
                Payload = introspection == null ? null : JsonConvert.SerializeObject(introspection)
            };
        }

        private async Task<ResponseModel> GetUserInfo(string cdrArrangementId)
        {
            var sp = _config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await _consentsRepository.GetConsent(cdrArrangementId);
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
            var userInfoEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).UserInfoEndpoint;
            var userInfo = await _dhInfosecService.UserInfo(
                userInfoEndpoint,
                sp.ClientCertificate.X509Certificate,
                arrangement.AccessToken);

            return new ResponseModel()
            {
                StatusCode = userInfo == null ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK,
                Messages = userInfo == null ? "Failed to retrieve userinfo." : "userinfo details found.",
                Payload = userInfo == null ? null : JsonConvert.SerializeObject(userInfo)
            };
        }

        private async Task EnsureModel(ConsentModel model)
        {
            model.Registrations = await _registrationsRepository.GetRegistrations();

            if (model.Registrations != null && model.Registrations.Any())
            {
                model.RegistrationListItems = model.Registrations.Select(r => new SelectListItem($"DH Brand: {r.DataHolderBrandId} ({r.ClientId})", r.ClientId)).ToList();
            }
            else
            {
                model.RegistrationListItems = new List<SelectListItem>();
            }
        }

        private void SetDefaults(ConsentModel model)
        {
            var sp = _config.GetSoftwareProductConfig();

            model.Scope = sp.Scope;
        }

        private async Task<string> GetInfoSecBaseUri(string dataHolderBrandId)
        {
            var dh = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
            return dh.EndpointDetail.InfoSecBaseUri;
        }

        private async Task<ResponseModel> RevokeToken(string cdrArrangementId, string tokenType)
        {
            var sp = _config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await _consentsRepository.GetConsent(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to revoke the token.
            var tokenRevocationEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).RevocationEndpoint;
            var revocation = await _dhInfosecService.RevokeToken(
                tokenRevocationEndpoint,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                arrangement.ClientId,
                tokenType,
                tokenType.Equals(TokenTypes.ACCESS_TOKEN, StringComparison.OrdinalIgnoreCase) ? arrangement.AccessToken : arrangement.RefreshToken,
                arrangement.AccessToken);

            return new ResponseModel()
            {
                StatusCode = revocation.StatusCode,
                Messages = revocation.Message,
            };
        }
    }
}
