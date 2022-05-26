using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
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

        public ParController(
            IConfiguration config,
            IDistributedCache cache,
            IInfosecService dhInfoSecService,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IRegistrationsRepository registrationsRepository)
        {
            _config = config;
            _cache = cache;
            _dhInfoSecService = dhInfoSecService;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _registrationsRepository = registrationsRepository;
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            var model = new ParModel() { UsePkce = true };
            await PopulatePickers(model);
            return View(model);
        }

        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(ParModel model)
        {
            if (!string.IsNullOrEmpty(model.ClientId))
            {
                var reg = await _registrationsRepository.GetRegistration(model.ClientId);
                var dhConfig = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(reg.DataHolderBrandId);
                var sp = _config.GetSoftwareProductConfig();
                var infosecBaseUri = await GetInfoSecBaseUri(reg.DataHolderBrandId);
                var stateKey = Guid.NewGuid().ToString();
                var nonce = Guid.NewGuid().ToString();
                var redirectUri = model.RedirectUris;

                var authState = new AuthorisationState()
                {
                    StateKey = stateKey,
                    ClientId = model.ClientId,
                    SharingDuration = model.SharingDuration,
                    Scope = model.Scope,
                    DataHolderBrandId = reg.DataHolderBrandId,
                    DataHolderInfosecBaseUri = infosecBaseUri,
                    RedirectUri = redirectUri
                };

                if (model.UsePkce)
                {
                    authState.Pkce = _dhInfoSecService.CreatePkceData();
                }

                await _cache.SetAsync(stateKey, authState, DateTimeOffset.UtcNow.AddMinutes(60));

                // Build the authentication request JWT.
                var authRequest = _dhInfoSecService.BuildAuthorisationRequestJwt(
                    dhConfig.Issuer,
                    model.ClientId,
                    redirectUri,
                    model.Scope,
                    stateKey,
                    nonce,
                    sp.SigningCertificate.X509Certificate,
                    model.SharingDuration,
                    model.CdrArrangementId,
                    "form_post",
                    authState.Pkce);

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
                        model.PushedAuthorisation.RequestUri);
                }
                else
                {
                    model.ErrorList = parResponse.Errors;
                }
            }

            await PopulatePickers(model);
            return View(model);
        }

        [HttpPost]
        [Route("registration/detail")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> RegistrationDetail(string clientId)
        {
            // Return the software product detail.
            string message = "";
            string redirectUris = "";
            string scope = "";
            List<SelectListItem> arrangements = new();

            // Return the RedirectUris for the picked item
            Registration registration = await _registrationsRepository.GetRegistration(clientId);
            if (registration == null)
            {
                message = "Registration not found";
                return new JsonResult(new { message, arrangements, redirectUris, scope });
            }

            // Return the Consents(CdrArrangements) for the picked item
            IEnumerable<ConsentArrangement> cdrArrangements = await _consentsRepository.GetConsents(clientId, "", HttpContext.User.GetUserId());
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
                    .Select(r => new SelectListItem($"DH Brand: {r.DataHolderBrandId} ({r.ClientId})", r.ClientId))
                    .ToList();
            }

            model.ConsentArrangementListItems = new List<SelectListItem>();
        }

        private async Task<string> GetInfoSecBaseUri(string dataHolderBrandId)
        {
            var dh = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
            return dh.EndpointDetail.InfoSecBaseUri;
        }
    }
}
