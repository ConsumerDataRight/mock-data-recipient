using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("dcr")]
    public class DynamicClientRegistrationController : Controller
    {
        private readonly ILogger<DynamicClientRegistrationController> _logger;
        private readonly IConfiguration _config;
        private readonly ISsaService _ssaService;
        private readonly IDynamicClientRegistrationService _dcrService;
        private readonly IRegistrationsRepository _regRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly SDK.Services.Register.IInfosecService _regInfosecService;
        private readonly SDK.Services.DataHolder.IInfosecService _dhInfosecService;
        private readonly Common.IDataHolderDiscoveryCache _dataHolderDiscoveryCache;

        public DynamicClientRegistrationController(
            IConfiguration config,
            ILogger<DynamicClientRegistrationController> logger,
            ISsaService ssaService,
            IRegistrationsRepository regRepository,
            IDataHoldersRepository dhRepository,
            IDynamicClientRegistrationService dcrService,
            SDK.Services.Register.IInfosecService regInfosecService,
            SDK.Services.DataHolder.IInfosecService dhInfosecService,
            Common.IDataHolderDiscoveryCache dataHolderDiscoveryCache)
        {
            _logger = logger;
            _config = config;
            _regRepository = regRepository;
            _dhRepository = dhRepository;
            _ssaService = ssaService;
            _dcrService = dcrService;
            _regInfosecService = regInfosecService;
            _dhInfosecService = dhInfosecService;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string clientId = null)
        {
            _logger.LogInformation($"GET request: {nameof(DynamicClientRegistrationController)}.{nameof(Index)}");

            var model = new DynamicClientRegistrationModel();

            if (string.IsNullOrEmpty(clientId))
            {
                SetDefaults(model);
            }
            else
            {
                var client = await _regRepository.GetRegistration(clientId);
                SetModel(model, client);
            }

            await EnsureModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DynamicClientRegistrationModel model)
        {
            _logger.LogInformation($"POST request: {nameof(DynamicClientRegistrationController)}.{nameof(Index)}");

            if (string.IsNullOrEmpty(model.ClientId))
            {
                await Register(model);
            }
            else
            {
                await UpdateRegistration(model);
            }

            await EnsureModel(model);
            return View(model);
        }

        [HttpDelete]
        [Route("registrations/{clientId}")]
        public async Task<IActionResult> Delete(string clientId)
        {
            _logger.LogInformation($"DELETE request: {nameof(DynamicClientRegistrationController)}.{nameof(Delete)}");

            // Delete the registration from the data holder.
            var deleteResponse = await DeleteRegistration(clientId);

            if (deleteResponse.StatusCode.IsSuccessful())
            {
                return Ok();
            }

            var response = new
            {
                StatusCode = deleteResponse.StatusCode,
                Messages = deleteResponse.Messages,
                Payload = deleteResponse.ResponsePayload
            };

            return new JsonResult(response)
            {
                StatusCode = Convert.ToInt32(deleteResponse.StatusCode)
            };
        }

        [HttpGet]
        [Route("registrations/{clientId}")]
        public async Task<IActionResult> Get(string clientId)
        {
            _logger.LogInformation($"GET request: {nameof(DynamicClientRegistrationController)}.{nameof(GetRegistration)} - {clientId}");

            var reg = await GetRegistration(clientId);
            var response = new
            {
                StatusCode = reg.StatusCode,
                Messages = reg.Messages,
                Payload = reg.ResponsePayload
            };
            return new JsonResult(response)
            {
                StatusCode = Convert.ToInt32(reg.StatusCode)
            };
        }

        private async Task EnsureModel(DynamicClientRegistrationModel model)
        {
            // Populate any existing registrations from the repository.
            model.Registrations = await _regRepository.GetRegistrations();
            model.DataHolderBrands = (await _dhRepository.GetDataHolderBrands()).Select(d => new SelectListItem(d.BrandName, d.DataHolderBrandId)).ToList();

            if (!string.IsNullOrEmpty(model.DataHolderBrandId))
            {
                var selected = model.DataHolderBrands.FirstOrDefault(d => d.Value.Equals(model.DataHolderBrandId, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                {
                    selected.Selected = true;
                }
            }
        }

        private void SetDefaults(DynamicClientRegistrationModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var dh = _config.GetDefaultDataHolderConfig();
            model.DataHolderBrandId = dh.BrandId;
            model.SoftwareProductId = sp.SoftwareProductId;
            model.RedirectUris = sp.RedirectUris;
            model.TokenEndpointAuthSigningAlg = sp.DefaultSigningAlgorithm;
            model.TokenEndpointAuthMethod = "private_key_jwt";
            model.GrantTypes = "client_credentials,authorization_code,refresh_token";
            model.ResponseTypes = "code,token";
            model.ApplicationType = "web";
            model.IdTokenSignedResponseAlg = sp.DefaultSigningAlgorithm;
            model.IdTokenEncryptedResponseAlg = "RSA-OAEP";
            model.IdTokenEncryptedResponseEnc = "A256GCM";
            model.RequestObjectSigningAlg = sp.DefaultSigningAlgorithm;
            model.Messages = "Waiting...";
        }

        private void SetModel(DynamicClientRegistrationModel model, Registration client)
        {
            model.ClientId = client.ClientId;
            model.DataHolderBrandId = client.DataHolderBrandId;
            model.SoftwareProductId = client.SoftwareId;
            model.RedirectUris = string.Join(',', client.RedirectUris);
            model.TokenEndpointAuthSigningAlg = client.TokenEndpointAuthSigningAlg;
            model.TokenEndpointAuthMethod = client.TokenEndpointAuthMethod;
            model.GrantTypes = string.Join(',', client.GrantTypes);
            model.ResponseTypes = string.Join(',', client.ResponseTypes);
            model.ApplicationType = client.ApplicationType;
            model.IdTokenSignedResponseAlg = client.IdTokenSignedResponseAlg;
            model.IdTokenEncryptedResponseAlg = client.IdTokenEncryptedResponseAlg;
            model.IdTokenEncryptedResponseEnc = client.IdTokenEncryptedResponseEnc;
            model.RequestObjectSigningAlg = client.RequestObjectSigningAlg;
            model.Messages = "Waiting...";
        }

        private async Task<DynamicClientRegistrationModel> GetRegistration(string clientId)
        {
            var model = new DynamicClientRegistrationModel();
            var sp = _config.GetSoftwareProductConfig();
            var client = await _regRepository.GetRegistration(clientId);
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(client.DataHolderBrandId);

            var tokenResponse = await _dhInfosecService.GetAccessToken(
                dataHolderDiscovery.TokenEndpoint,
                clientId,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                scope: Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                redirectUri: sp.RedirectUri,
                grantType: Constants.GrantTypes.CLIENT_CREDENTIALS);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return model;
            }

            // Request DCR to the Data Holder.
            var dcrResponse = await _dcrService.GetRegistration(dataHolderDiscovery.RegistrationEndpoint, sp.ClientCertificate.X509Certificate, tokenResponse.Data.AccessToken, clientId);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = dcrResponse.Message;
            model.ResponsePayload = dcrResponse.Payload;

            return model;
        }

        private async Task Register(DynamicClientRegistrationModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var ssa = await GetSSA(model);

            // Construct the DCR request.
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(model.DataHolderBrandId);
            var registrationRequestJwt = PopulateRegistrationRequestJwt(model, sp, ssa, dataHolderDiscovery.Issuer);

            // Request DCR to the Data Holder.
            var dcrResponse = await _dcrService.Register(dataHolderDiscovery.RegistrationEndpoint, sp.ClientCertificate.X509Certificate, registrationRequestJwt);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = $"{dcrResponse.StatusCode} - {(dcrResponse.IsSuccessful ? "Registered" : dcrResponse.Message)}";
            model.ResponsePayload = dcrResponse.Payload;

            if (dcrResponse.IsSuccessful)
            {
                var registration = dcrResponse.Data;
                registration.DataHolderBrandId = model.DataHolderBrandId;

                await _regRepository.PersistRegistration(dcrResponse.Data);
            }
        }

        private async Task UpdateRegistration(DynamicClientRegistrationModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var ssa = await GetSSA(model);
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(model.DataHolderBrandId);

            var tokenResponse = await _dhInfosecService.GetAccessToken(
                dataHolderDiscovery.TokenEndpoint,
                model.ClientId,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                scope: Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                redirectUri: sp.RedirectUri,
                grantType: Constants.GrantTypes.CLIENT_CREDENTIALS);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return;
            }

            // Construct the DCR request.
            var registrationRequestJwt = PopulateRegistrationRequestJwt(model, sp, ssa, dataHolderDiscovery.Issuer);

            // Request DCR to the Data Holder.
            var dcrResponse = await _dcrService.UpdateRegistration(dataHolderDiscovery.RegistrationEndpoint, sp.ClientCertificate.X509Certificate, tokenResponse.Data.AccessToken, model.ClientId, registrationRequestJwt);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = dcrResponse.Message;
            model.ResponsePayload = dcrResponse.Payload;

            if (dcrResponse.IsSuccessful)
            {
                var registration = dcrResponse.Data;
                registration.DataHolderBrandId = model.DataHolderBrandId;

                await _regRepository.UpdateRegistration(dcrResponse.Data);
            }
        }

        private async Task<DynamicClientRegistrationModel> DeleteRegistration(string clientId)
        {
            var model = new DynamicClientRegistrationModel();
            var client = await _regRepository.GetRegistration(clientId);

            var sp = _config.GetSoftwareProductConfig();
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(client.DataHolderBrandId);
            var tokenResponse = await _dhInfosecService.GetAccessToken(
                dataHolderDiscovery.TokenEndpoint,
                clientId,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate,
                scope: Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                redirectUri: sp.RedirectUri,
                grantType: Constants.GrantTypes.CLIENT_CREDENTIALS);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return model;
            }

            // Delete client from the Data Holder.
            var dcrResponse = await _dcrService.DeleteRegistration(dataHolderDiscovery.RegistrationEndpoint, sp.ClientCertificate.X509Certificate, tokenResponse.Data.AccessToken, clientId);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = dcrResponse.Message;
            model.ResponsePayload = dcrResponse.Payload;

            if (dcrResponse.IsSuccessful)
            {
                // Delete the client from the internal repository.
                await _regRepository.DeleteRegistration(clientId);
            }

            return model;
        }

        private async Task<string> GetSSA(DynamicClientRegistrationModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();

            // Get an access token from the Register.
            // var tokenResponse = await _regInfosecService.GetAccessToken(reg.MtlsBaseUri, sp.SoftwareProductId, sp.ClientCertificate.X509Certificate, sp.SigningCertificate.X509Certificate);
            var tokenResponse = await _regInfosecService.GetAccessToken(reg.TokenEndpoint, sp.SoftwareProductId, sp.ClientCertificate.X509Certificate, sp.SigningCertificate.X509Certificate);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return null;
            }

            // Get an SSA from the Register.
            var ssaResponse = await _ssaService.GetSoftwareStatementAssertion(reg.MtlsBaseUri, "2", tokenResponse.Data.AccessToken, sp.ClientCertificate.X509Certificate, sp.BrandId, sp.SoftwareProductId);

            if (!ssaResponse.IsSuccessful)
            {
                model.StatusCode = ssaResponse.StatusCode;
                model.Messages = $"{ssaResponse.StatusCode} - {ssaResponse.Message}";
                return null;
            }

            return ssaResponse.Data;
        }

        private string PopulateRegistrationRequestJwt(DynamicClientRegistrationModel model, Configuration.Models.SoftwareProduct sp, string ssa, string audience)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim("jti", Guid.NewGuid().ToString()));
            claims.Add(new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer));
            claims.Add(new Claim("token_endpoint_auth_signing_alg", model.TokenEndpointAuthSigningAlg ?? ""));
            claims.Add(new Claim("token_endpoint_auth_method", model.TokenEndpointAuthMethod ?? ""));
            claims.Add(new Claim("application_type", model.ApplicationType ?? ""));
            claims.Add(new Claim("id_token_signed_response_alg", model.IdTokenSignedResponseAlg ?? ""));
            claims.Add(new Claim("id_token_encrypted_response_alg", model.IdTokenEncryptedResponseAlg ?? ""));
            claims.Add(new Claim("id_token_encrypted_response_enc", model.IdTokenEncryptedResponseEnc ?? ""));
            claims.Add(new Claim("request_object_signing_alg", model.RequestObjectSigningAlg ?? ""));
            claims.Add(new Claim("software_statement", ssa ?? ""));

            foreach (var redirectUri in model.RedirectUris.Split(','))
            {
                claims.Add(new Claim("redirect_uris", redirectUri));
            }

            foreach (var grantType in model.GrantTypes.Split(','))
            {
                claims.Add(new Claim("grant_types", grantType));
            }

            foreach (var responseType in model.ResponseTypes.Split(','))
            {
                claims.Add(new Claim("response_types", responseType));
            }

            // TODO: algorithm to be adaptable.
            var jwt = new JwtSecurityToken(
                issuer: sp.SoftwareProductId,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new X509SigningCredentials(sp.SigningCertificate.X509Certificate, SecurityAlgorithms.RsaSsaPssSha256));


            var tokenHandler = new JwtSecurityTokenHandler();

            return tokenHandler.WriteToken(jwt);
        }
    }
}
