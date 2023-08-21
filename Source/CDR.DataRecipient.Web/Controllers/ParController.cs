using CDR.DataRecipient.Models;
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
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    [Route("par")]
    public class ParController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;
        private readonly IInfosecService _dhInfoSecService;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
        private readonly IRegistrationsRepository _registrationsRepository;
        private readonly ILogger<ParController> _logger;

        public ParController(
            IConfiguration config,
            IDistributedCache cache,
            IInfosecService dhInfoSecService,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IRegistrationsRepository registrationsRepository,
            ILogger<ParController> logger)
        {
            _config = config;
            _cache = cache;
            _dhInfoSecService = dhInfoSecService;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _registrationsRepository = registrationsRepository;
            _logger=logger;
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            var model = new ParModel() { UsePkce = true, ResponseType = "code id_token", ResponseMode = "fragment" };
            await PopulatePickers(model);
            return View(model);
        }

        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(ParModel model)
        {
            if (!string.IsNullOrEmpty(model.ClientId))
            {                
                try
                {                   
                    var reg = await _registrationsRepository.GetRegistration(model.ClientId, model.DataHolderBrandId);
                    var dhConfig = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(reg.DataHolderBrandId);
                    var sp = _config.GetSoftwareProductConfig();

                    var infosecBaseUri = await GetInfoSecBaseUri(reg.DataHolderBrandId);
                    if (string.IsNullOrEmpty(infosecBaseUri))
                        throw new CustomException();

                    var stateKey = Guid.NewGuid().ToString();
                    var nonce = Guid.NewGuid().ToString();
                    var redirectUri = reg.RedirectUris.FirstOrDefault();

                    var acrValuesSupported = dhConfig.AcrValuesSupported;
                    int acrValueSupported = ValidateAcrValuesSpported(acrValuesSupported);

                    var authState = new AuthorisationState()
                    {
                        StateKey = stateKey,
                        ClientId = model.ClientId,
                        SharingDuration = model.SharingDuration,
                        Scope = model.Scope,
                        DataHolderBrandId = reg.DataHolderBrandId,
                        DataHolderInfosecBaseUri = infosecBaseUri,
                        RedirectUri = redirectUri,
                        UserId = this.HttpContext.User.GetUserId()
                    };

                    if (model.UsePkce)
                        authState.Pkce = _dhInfoSecService.CreatePkceData();

                    _logger.LogDebug("saving AuthorisationState of {@authState} to cache", authState);

                    await _cache.SetAsync(stateKey, authState, DateTimeOffset.UtcNow.AddMinutes(60));

                    // Build the authentication request JWT.
                    var authorisationRequestJwt = new AuthorisationRequestJwt()
                    {
                        InfosecBaseUri = dhConfig.Issuer,
                        ClientId = model.ClientId,
                        RedirectUri = redirectUri,
                        Scope = model.Scope,
                        State = stateKey,
                        Nonce = nonce,
                        SigningCertificate = sp.SigningCertificate.X509Certificate,
                        SharingDuration = model.SharingDuration,
                        CdrArrangementId = model.CdrArrangementId,
                        ResponseMode = model.ResponseMode,
                        Pkce = authState.Pkce,
                        AcrValueSupported = acrValueSupported,
                        ResponseType = model.ResponseType
                    };

                    var authRequest = _dhInfoSecService.BuildAuthorisationRequestJwt(authorisationRequestJwt);

                    var parResponse = await _dhInfoSecService.PushedAuthorisationRequest(
                        dhConfig.PushedAuthorizationRequestEndpoint,
                        sp.ClientCertificate.X509Certificate,
                        sp.SigningCertificate.X509Certificate,
                        model.ClientId,
                        authRequest);

                    model.StatusCode = parResponse.StatusCode;
                    model.Messages = parResponse.Message;

                    if (parResponse.IsSuccessful)
                    {
                        model.PushedAuthorisation = parResponse.Data;

                        // Build the Authorisation URL for the Data Holder passing in the request uri returned from the PAR response.
                        model.AuthorisationUri = await _dhInfoSecService.BuildAuthorisationRequestUri(
                            infosecBaseUri,
                            model.ClientId,
                            sp.SigningCertificate.X509Certificate,
                            model.PushedAuthorisation.RequestUri,
                            model.Scope,
                            model.ResponseType);
                    }
                    else
                    {
                        model.ErrorList = parResponse.Errors;
                    }
                }
                catch (CustomException)
                {
                    var msg = $"The Data Holder details do not exist in the repository for ClientId: {model.ClientId}";
                    return View("Error", new ErrorViewModel { Message = msg });
                }
                catch (Exception ex)
                {
                    var msg = $"Unable to create the Pushed Authorisation Request (PAR) with ClientId: {model.ClientId} - {ex.Message}";
                    return View("Error", new ErrorViewModel { Message = msg });
                }
            }

            await PopulatePickers(model);
            return View(model);
        }

        /// <summary>
        /// With multiple supported acr values
        /// Should use the the max. available Acr value spported only
        /// </summary>
        /// <param name="acrValuesSupported"></param>
        /// <returns>max available ACR value supported</returns>
        private static int ValidateAcrValuesSpported(string[] acrValuesSupported)
        {
            var maxAcrSupported = 0;
            foreach (var acrValueSupported in acrValuesSupported)
            {
                // Should use the max. available supported acr
                // "urn:cds.au:cdr:3"
                var acrSupported = acrValueSupported.Split(':')[3];
                int acrValue;
                if (int.TryParse(acrSupported, out acrValue) && acrValue > maxAcrSupported)
                {
                    maxAcrSupported = acrValue;
                }
            }

            return maxAcrSupported;
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
            List<SelectListItem> arrangements = new();

            // Return the RedirectUris for the picked item
            var registrationInfo = Registration.SplitRegistrationId(registrationId);
            Registration registration = await _registrationsRepository.GetRegistration(registrationInfo.ClientId, registrationInfo.DataHolderBrandId);
            if (registration == null)
            {
                message = "Registration not found";
                return new JsonResult(new { message, arrangements, redirectUris, scope });
            }

            // Return the Consents(CdrArrangements) for the picked item
            IEnumerable<ConsentArrangement> cdrArrangements = await _consentsRepository.GetConsents(registrationInfo.ClientId, registrationInfo.DataHolderBrandId, HttpContext.User.GetUserId());
            if (cdrArrangements != null && cdrArrangements.Any())
            {
                arrangements = cdrArrangements.Select(c => new SelectListItem(c.CdrArrangementId, c.CdrArrangementId)).ToList();
            }

            return new JsonResult(new 
            { 
                message, 
                arrangements, 
                redirectUris = string.Join(' ', registration.RedirectUris), 
                scope = registration.Scope
            });
        }

        private async Task PopulatePickers(ParModel model)
        {
            model.Registrations = await _registrationsRepository.GetRegistrations();

            model.RegistrationListItems = new List<SelectListItem>();

            if (model.Registrations != null && model.Registrations.Any())
            {
                model.RegistrationListItems = model.Registrations
                    .Select(r => new SelectListItem($"DH Brand: {r.BrandName} ({r.DataHolderBrandId}) - ({r.ClientId})", r.GetRegistrationId()))
                    .ToList();
            }

            model.ConsentArrangementListItems = new List<SelectListItem>();
        }

        private async Task<string> GetInfoSecBaseUri(string dataHolderBrandId)
        {
            var dh = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
            if (dh == null)
                return null;

            return dh.EndpointDetail.InfoSecBaseUri;
        }
    }
}
