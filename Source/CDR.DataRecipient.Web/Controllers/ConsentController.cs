﻿using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IDataHoldersRepository _dhRepository;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;

        public ConsentController(
            IConfiguration config,
            IDistributedCache cache,
            IInfosecService dhInfosecService,
            IRegistrationsRepository registrationsRepository,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache)
        {
            _config = config;
            _cache = cache;
            _dhInfosecService = dhInfosecService;
            _registrationsRepository = registrationsRepository;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            var model = new ConsentModel() { UsePkce = true };
            await PopulatePicker(model);
            return View(model);
        }

        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(ConsentModel model)
        {
            try
            {
                await PopulatePicker(model);

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
                    if (string.IsNullOrEmpty(infosecBaseUri))
                        throw new CustomException();

                    var stateKey = Guid.NewGuid().ToString();
                    var nonce = Guid.NewGuid().ToString();
                    var redirectUri = model.RedirectUris;

                    var authState = new AuthorisationState()
                    {
                        StateKey = stateKey,
                        ClientId = model.ClientId,
                        SharingDuration = model.SharingDuration,
                        Scope = model.Scope,
                        DataHolderBrandId = client.DataHolderBrandId,
                        DataHolderInfosecBaseUri = infosecBaseUri,
                        RedirectUri = redirectUri,
                        UserId = this.HttpContext.User.GetUserId()
                    };

                    if (model.UsePkce)
                    {
                        authState.Pkce = _dhInfosecService.CreatePkceData();
                    }

                    await _cache.SetAsync(stateKey, authState, DateTimeOffset.Now.AddMinutes(60));

                    model.AuthorisationUri = await _dhInfosecService.BuildAuthorisationRequestUri(
                        infosecBaseUri,
                        client.ClientId,
                        redirectUri,
                        model.Scope,
                        stateKey,
                        nonce,
                        sp.SigningCertificate.X509Certificate,
                        model.SharingDuration,
                        authState.Pkce);
                }
            }
            catch (CustomException)
            {
                var msg = $"The Data Holder details do not exist in the repository for ClientId: {model.ClientId}";
                return View("Error", new ErrorViewModel { Message = msg });
            }
            catch (Exception ex)
            {
                var msg = $"Unable to create the Consent with ClientId: {model.ClientId} - {ex.Message}";
                return View("Error", new ErrorViewModel { Message = msg });
            }
            return View(model);
        }

        [HttpPost]
        [Route("registration/detail")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> RegistrationDetail(string registrationId)
        {
            // Return the software product detail.
            string message = "";
            string redirectUris = "";
            string scope = "";

            var registrationInfo = Registration.SplitRegistrationId(registrationId);
            Registration myResponse = await _registrationsRepository.GetRegistration(registrationInfo.ClientId, registrationInfo.DataHolderBrandId);
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
        public async Task<IActionResult> Callback()
        {
            var model = new TokenModel();
            var isSuccessful = this.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase) && this.Request.Form != null && this.Request.Form.ContainsKey("id_token");

            if (isSuccessful)
            {
                var sp = _config.GetSoftwareProductConfig();
                var authCode = this.Request.Form["code"].ToString();
                var state = this.Request.Form["state"].ToString();

                var authState = await _cache.GetAsync<AuthorisationState>(state);

                // Request a token from the data holder.
                var tokenEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByInfoSecBaseUri(authState.DataHolderInfosecBaseUri)).TokenEndpoint;
                model.TokenResponse = await _dhInfosecService.GetAccessToken(
                    tokenEndpoint,
                    authState.ClientId,
                    sp.ClientCertificate.X509Certificate,
                    sp.SigningCertificate.X509Certificate,
                    "",
                    authState.RedirectUri,
                    authCode,
                    "authorization_code",
                    authState.Pkce);

                if (model.TokenResponse.IsSuccessful)
                {
                    // Save the consent arrangement.
                    var consentArrangement = new ConsentArrangement()
                    {
                        UserId = authState.UserId,
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Consents()
        {
            var model = new ConsentsModel();
            model.ConsentArrangements = await _consentsRepository.GetConsents("", "", HttpContext.User.GetUserId(), "");
            return View(model);
        }

        [HttpGet]
        [Route("userinfo/{cdrArrangementId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> UserInfo(string cdrArrangementId)
        {
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Introspection(string cdrArrangementId)
        {
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Revoke(string cdrArrangementId)
        {
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Revoke(string cdrArrangementId, [FromQuery] string tokenType)
        {
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Refresh(string cdrArrangementId)
        {
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Delete(string cdrArrangementId)
        {
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
            var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementId);
            if (arrangement == null)
            {
                return new ResponseModel()
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Messages = "CDR Arrangement ID could not be found in local repository.",
                };
            }

            // Call the DH to refresh the access token.
            var tokenEndpoint = (await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(arrangement.DataHolderBrandId)).TokenEndpoint;
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
            var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementId);
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
                arrangement.CdrArrangementId);

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
            var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementId);
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
                arrangement.RefreshToken);

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
            var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementId);
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

        private async Task PopulatePicker(ConsentModel model)
        {
            model.Registrations = await _registrationsRepository.GetRegistrations();

            if (model.Registrations != null && model.Registrations.Any())
                model.RegistrationListItems = model.Registrations.Select(r => new SelectListItem($"DH Brand: {r.BrandName} ({r.DataHolderBrandId}) - ({r.ClientId})", r.GetRegistrationId())).ToList();
            else
                model.RegistrationListItems = new List<SelectListItem>();
        }

        private async Task<string> GetInfoSecBaseUri(string dataHolderBrandId)
        {
            var dh = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
            if (dh == null)
                return null;

            return dh.EndpointDetail.InfoSecBaseUri;
        }

        private async Task<ResponseModel> RevokeToken(string cdrArrangementId, string tokenType)
        {
            var sp = _config.GetSoftwareProductConfig();

            // Retrieve the arrangement details from the local repository.
            var arrangement = await _consentsRepository.GetConsentByArrangement(cdrArrangementId);
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
                tokenType.Equals(SDK.Constants.TokenTypes.ACCESS_TOKEN, StringComparison.OrdinalIgnoreCase) ? arrangement.AccessToken : arrangement.RefreshToken);

            return new ResponseModel()
            {
                StatusCode = revocation.StatusCode,
                Messages = revocation.Message,
            };
        }
    }
}