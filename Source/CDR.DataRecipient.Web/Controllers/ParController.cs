using System;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("par")]
    public class ParController : Controller
    {
        private readonly ILogger<ParController> _logger;
        private readonly IConfiguration _config;
        private readonly IInfosecService _dhInfoSecService;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
        private readonly IRegistrationsRepository _registrationsRepository;
        private readonly IMemoryCache _cache;

        public ParController(
            IConfiguration config,
            ILogger<ParController> logger,
            IInfosecService dhInfoSecService,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IRegistrationsRepository registrationsRepository,
            IMemoryCache cache)
        {
            _logger = logger;
            _config = config;
            _dhInfoSecService = dhInfoSecService;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _registrationsRepository = registrationsRepository;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new ParModel();
            await SetDefaults(model);
            await EnsureModel(model);
            return View(model);
        }

        [HttpPost]
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
                var redirectUri = sp.RedirectUri;

                _cache.Set(stateKey, new AuthorisationState()
                {
                    StateKey = stateKey,
                    ClientId = model.ClientId,
                    SharingDuration = model.SharingDuration,
                    Scope = model.Scope,
                    DataHolderBrandId = reg.DataHolderBrandId,
                    DataHolderInfosecBaseUri = infosecBaseUri,
                    RedirectUri = redirectUri
                });

                // Build the authentication request JWT.
                var authRequest = _dhInfoSecService.BuildAuthorisationRequestJwt(
                    dhConfig.Issuer,
                    model.ClientId,
                    sp.RedirectUri,
                    model.Scope,
                    stateKey,
                    nonce,
                    sp.SigningCertificate.X509Certificate,
                    model.SharingDuration,
                    model.CdrArrangementId,
                    "query");

                var parResponse = await _dhInfoSecService.PushedAuthorisationRequest(
                    dhConfig.PushedAuthorizationRequestEndpoint,
                    sp.ClientCertificate.X509Certificate,
                    sp.SigningCertificate.X509Certificate,
                    model.ClientId,
                    authRequest,
                    model.Scope);

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
                        model.Scope,
                        model.PushedAuthorisation.RequestUri);
                }
                else
                {
                    model.ErrorList = parResponse.Errors;
                }
            }

            await EnsureModel(model);
            return View(model);
        }

        private async Task SetDefaults(ParModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            model.Scope = sp.Scope;
        }

        private async Task EnsureModel(ParModel model)
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

            model.ConsentArrangements = await _consentsRepository.GetConsents();

            if (model.ConsentArrangements != null && model.ConsentArrangements.Any())
            {
                model.ConsentArrangementListItems = model.ConsentArrangements.Select(c => new SelectListItem($"{c.CdrArrangementId}", c.CdrArrangementId)).ToList();
            }
            else
            {
                model.ConsentArrangementListItems = new List<SelectListItem>();
            }
        }

        private async Task<string> GetInfoSecBaseUri(string dataHolderBrandId)
        {
            var dh = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
            return dh.EndpointDetail.InfoSecBaseUri;
        }
    }
}
