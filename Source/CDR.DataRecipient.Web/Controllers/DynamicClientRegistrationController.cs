using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Enum;
using CDR.DataRecipient.SDK.Enumerations;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Features;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    [Route("dcr")]
    public class DynamicClientRegistrationController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ISsaService _ssaService;
        private readonly IDynamicClientRegistrationService _dcrService;
        private readonly IRegistrationsRepository _regRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly SDK.Services.Register.IInfosecService _regInfosecService;
        private readonly SDK.Services.DataHolder.IInfosecService _dhInfosecService;
        private readonly Common.IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
        private readonly IFeatureManager _featureManager;
        private readonly ICacheManager _cacheManager;

        public DynamicClientRegistrationController(
            IConfiguration config,
            ISsaService ssaService,
            IRegistrationsRepository regRepository,
            IDataHoldersRepository dhRepository,
            IDynamicClientRegistrationService dcrService,
            SDK.Services.Register.IInfosecService regInfosecService,
            SDK.Services.DataHolder.IInfosecService dhInfosecService,
            Common.IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            ICacheManager cacheManager,
            IFeatureManager featureManager)
        {
            _config = config;
            _regRepository = regRepository;
            _dhRepository = dhRepository;
            _ssaService = ssaService;
            _dcrService = dcrService;
            _regInfosecService = regInfosecService;
            _dhInfosecService = dhInfosecService;
            _dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            _cacheManager = cacheManager;
            _featureManager = featureManager;
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(string clientId = null, string dataHolderBrandId = null)
        {
            var model = new DynamicClientRegistrationModel();
            if (string.IsNullOrEmpty(clientId))
            {
                SetViewModelDefaults(model);
            }
            else
            {
                var client = await _regRepository.GetRegistration(clientId, dataHolderBrandId);
                model.TransactionType = "Update";
                SetViewModel(model, client);
            }

            await PopulateFormDetail(model);
            return View(model);
        }

        [FeatureGate(nameof(Feature.AllowDynamicClientRegistration))]
        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(DynamicClientRegistrationModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.ClientId))
                {
                    await Register(model);
                }
                else
                {
                    await UpdateRegistration(model);
                }

                await PopulateFormDetail(model);
            }
            catch (Exception ex)
            {
                var type = string.Empty;
                if (string.IsNullOrEmpty(model.ClientId))
                {
                    type = $"create";
                }
                else
                {
                    type = "update";
                }

                var msg = $"Unable to {type} the Dynamic Client Registration with DataHolderBrandId: {model.DataHolderBrandId} - {ex.Message}";
                return View("Error", new ErrorViewModel { Message = msg });
            }

            return View(model);
        }

        [FeatureGate(nameof(Feature.AllowDynamicClientRegistration))]
        [HttpDelete]
        [Route("registrations/{clientId}/{dataHolderBrandId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Delete(string clientId, string dataHolderBrandId)
        {
            // Delete the registration from the data holder.
            var regResp = new ResponseModel();

            try
            {
                var deleteResponse = await DeleteRegistration(clientId, dataHolderBrandId);
                if (deleteResponse.StatusCode.IsSuccessful())
                {
                    return Ok();
                }

                regResp.StatusCode = System.Net.HttpStatusCode.OK;
                regResp.Messages = deleteResponse.Messages;
                regResp.Payload = deleteResponse.ResponsePayload;
            }
            catch (Exception ex)
            {
                var msg = $"Unable to delete the DCR details for ClientId: {clientId}, DataHolderBrandId:{dataHolderBrandId} - {ex.Message}";
                regResp.StatusCode = System.Net.HttpStatusCode.BadRequest;
                regResp.Messages = msg;
            }

            return new JsonResult(regResp)
            {
                StatusCode = Convert.ToInt32(regResp.StatusCode),
            };
        }

        [HttpGet]
        [Route("registrations/{clientId}/{dataHolderBrandId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Get(string clientId, string dataHolderBrandId)
        {
            var regResp = new ResponseModel();

            try
            {
                var reg = await GetRegistration(clientId, dataHolderBrandId);
                if (reg.StatusCode.IsSuccessful())
                {
                    regResp.StatusCode = reg.StatusCode;
                    regResp.Messages = reg.Messages;
                    regResp.Payload = reg.ResponsePayload;
                }
            }
            catch (Exception ex)
            {
                var msg = $"Unable to view the DCR details for ClientId: {clientId}, DataHolderBrandId:{dataHolderBrandId} - {ex.Message}";
                regResp.StatusCode = System.Net.HttpStatusCode.BadRequest;
                regResp.Messages = msg;
            }

            return new JsonResult(regResp)
            {
                StatusCode = Convert.ToInt32(regResp.StatusCode),
            };
        }

        [HttpPut]
        [Route("registrations/{clientId}/{dataHolderBrandId}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> UpdateDCRRegistration(string clientId, string dataHolderBrandId)
        {
            var regResp = new ResponseModel();

            try
            {
                regResp = await UpdateCurrentRegistration(clientId, dataHolderBrandId);
            }
            catch (Exception ex)
            {
                var msg = $"Unable to view the DCR details for ClientId: {clientId}, DataHolderBrandId:{dataHolderBrandId} - {ex.Message}";
                regResp.StatusCode = System.Net.HttpStatusCode.BadRequest;
                regResp.Messages = msg;
            }

            return new JsonResult(regResp)
            {
                StatusCode = Convert.ToInt32(regResp.StatusCode),
            };
        }

        [FeatureGate(nameof(Feature.AllowDynamicClientRegistration))]
        [HttpGet]
        [Route("{dataHolderBrandId}/openid-configuration")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetOidcDocument(string dataHolderBrandId)
        {
            var response = new ResponseModel();
            string configUrl = string.Empty;

            try
            {
                DataHolderBrand dataHolder;
                dataHolder = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);

                if (dataHolder == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Messages = $"Could not find data holder {dataHolderBrandId}";
                }
                else
                {
                    string infosecBaseUri = dataHolder.EndpointDetail.InfoSecBaseUri;
                    configUrl = string.Concat(infosecBaseUri.TrimEnd('/'), "/.well-known/openid-configuration");

                    var oidcDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(dataHolderBrandId);
                    var payload = JsonConvert.SerializeObject(oidcDiscovery);
                    response.Payload = payload;
                    response.Messages = $"Discovery Document details loaded from {configUrl}";
                    response.StatusCode = HttpStatusCode.OK;
                }
            }
            catch (Exception)
            {
                response.Messages = $"Unable to load Discovery Document from {configUrl}";
                response.StatusCode = HttpStatusCode.BadRequest;
            }

            return new JsonResult(response)
            {
                StatusCode = Convert.ToInt32(response.StatusCode),
            };
        }

        private async Task Register(DynamicClientRegistrationModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var ssa = await GetSSA(sp, model);

            // Construct the DCR request.
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(model.DataHolderBrandId);
            var registrationRequestJwt = PopulateRegistrationRequestJwt(model, sp, ssa, dataHolderDiscovery.Issuer);

            // Request DCR to the Data Holder.
            var dcrResponse = await _dcrService.Register(
                dataHolderDiscovery.RegistrationEndpoint,
                sp.ClientCertificate.X509Certificate,
                registrationRequestJwt);

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

        private async Task<DynamicClientRegistrationModel> GetRegistration(string clientId, string dataHolderBrandId)
        {
            var model = new DynamicClientRegistrationModel();
            var sp = _config.GetSoftwareProductConfig();

            var client = await _regRepository.GetRegistration(clientId, dataHolderBrandId);
            if (client == null)
            {
                model.StatusCode = System.Net.HttpStatusCode.BadRequest;
                model.Messages = $"The registration details do not exist in the repository";
                return model;
            }

            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(client.DataHolderBrandId);
            if (dataHolderDiscovery == null)
            {
                model.StatusCode = System.Net.HttpStatusCode.BadRequest;
                model.Messages = $"Data Holder discovery failed for {client.BrandName} ({client.DataHolderBrandId})";
                return model;
            }

            var accessToken = new AccessToken()
            {
                TokenEndpoint = dataHolderDiscovery.TokenEndpoint,
                ClientId = clientId,
                ClientCertificate = sp.ClientCertificate.X509Certificate,
                SigningCertificate = sp.SigningCertificate.X509Certificate,
                Scope = Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                RedirectUri = sp.RedirectUri,
                GrantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            };

            var tokenResponse = await _dhInfosecService.GetAccessToken(accessToken);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return model;
            }

            // Request DCR to the Data Holder.
            var dcrResponse = await _dcrService.GetRegistration(
                dataHolderDiscovery.RegistrationEndpoint,
                sp.ClientCertificate.X509Certificate,
                tokenResponse.Data.AccessToken,
                clientId);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = dcrResponse.Message;
            model.ResponsePayload = dcrResponse.Payload;

            return model;
        }

        private async Task<ResponseModel> UpdateCurrentRegistration(string clientId, string dataHolderBrandId)
        {
            var model = new ResponseModel();

            var softwareProductConfig = _config.GetSoftwareProductConfig();
            var registerConfig = _config.GetRegisterConfig();
            var tokenEndpoint = await _cacheManager.GetRegisterTokenEndpoint(registerConfig.OidcDiscoveryUri);

            // Get an access token from the Register.
            var registerTokenResponse = await _regInfosecService.GetAccessToken(
                tokenEndpoint,
                softwareProductConfig.SoftwareProductId,
                softwareProductConfig.ClientCertificate.X509Certificate,
                softwareProductConfig.SigningCertificate.X509Certificate);

            if (!registerTokenResponse.IsSuccessful)
            {
                model.StatusCode = registerTokenResponse.StatusCode;
                model.Messages = $"{registerTokenResponse.StatusCode} - {registerTokenResponse.Message}";
                return model;
            }

            // Get an SSA from the Register
            var ssaResponse = await _ssaService.GetSoftwareStatementAssertion(
                registerConfig.MtlsBaseUri,
                registerConfig.GetSsaMinXv,
                registerTokenResponse.Data.AccessToken,
                softwareProductConfig.ClientCertificate.X509Certificate,
                softwareProductConfig.BrandId,
                softwareProductConfig.SoftwareProductId,
                Industry.ALL);

            if (!ssaResponse.IsSuccessful)
            {
                model.StatusCode = ssaResponse.StatusCode;
                model.Messages = $"{ssaResponse.StatusCode} - {ssaResponse.Message}";
                return model;
            }

            // Get Data Holder discovery
            var ssa = ssaResponse.Data;
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(dataHolderBrandId);

            var accessToken = new AccessToken()
            {
                TokenEndpoint = dataHolderDiscovery.TokenEndpoint,
                ClientId = clientId,
                ClientCertificate = softwareProductConfig.ClientCertificate.X509Certificate,
                SigningCertificate = softwareProductConfig.SigningCertificate.X509Certificate,
                Scope = Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                RedirectUri = softwareProductConfig.RedirectUri,
                GrantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            };

            // Get Access Token from Data Holder
            var dhTokenResponse = await _dhInfosecService.GetAccessToken(accessToken);

            if (!dhTokenResponse.IsSuccessful)
            {
                model.StatusCode = dhTokenResponse.StatusCode;
                model.Messages = $"{dhTokenResponse.StatusCode} - {dhTokenResponse.Message}";
                return model;
            }

            // create DCR Request Jwt
            var (claims, errorMessage) = CreateClaimsForUpdateDCRRequest(dataHolderDiscovery, ssa, softwareProductConfig);
            if (claims == null)
            {
                model.Messages = errorMessage;
                return model;
            }

            var jwt = new JwtSecurityToken(
                issuer: softwareProductConfig.SoftwareProductId,
                audience: dataHolderDiscovery.Issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new X509SigningCredentials(softwareProductConfig.SigningCertificate.X509Certificate, SecurityAlgorithms.RsaSsaPssSha256));

            var tokenHandler = new JwtSecurityTokenHandler();
            var registrationRequestJwt = tokenHandler.WriteToken(jwt);

            // Send Put DCR to the Data Holder.
            var dcrResponse = await _dcrService.UpdateRegistration(
                dataHolderDiscovery.RegistrationEndpoint,
                softwareProductConfig.ClientCertificate.X509Certificate,
                dhTokenResponse.Data.AccessToken,
                clientId,
                registrationRequestJwt);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = dcrResponse.Message;

            if (dcrResponse.IsSuccessful)
            {
                var registration = dcrResponse.Data;
                registration.DataHolderBrandId = dataHolderBrandId;

                await _regRepository.UpdateRegistration(dcrResponse.Data);
            }

            return model;
        }

        private async Task UpdateRegistration(DynamicClientRegistrationModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var ssa = await GetSSA(sp, model);
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(model.DataHolderBrandId);
            model.TransactionType = "Update";

            var accessToken = new AccessToken()
            {
                TokenEndpoint = dataHolderDiscovery.TokenEndpoint,
                ClientId = model.ClientId,
                ClientCertificate = sp.ClientCertificate.X509Certificate,
                SigningCertificate = sp.SigningCertificate.X509Certificate,
                Scope = Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                RedirectUri = sp.RedirectUri,
                GrantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            };

            var tokenResponse = await _dhInfosecService.GetAccessToken(accessToken);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return;
            }

            // Construct the DCR request.
            var registrationRequestJwt = PopulateRegistrationRequestJwt(model, sp, ssa, dataHolderDiscovery.Issuer);

            // Request DCR to the Data Holder.
            var dcrResponse = await _dcrService.UpdateRegistration(
                dataHolderDiscovery.RegistrationEndpoint,
                sp.ClientCertificate.X509Certificate,
                tokenResponse.Data.AccessToken,
                model.ClientId,
                registrationRequestJwt);

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

        private static string PopulateRegistrationRequestJwt(DynamicClientRegistrationModel model, SoftwareProduct sp, string ssa, string audience)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim("jti", Guid.NewGuid().ToString()));
            claims.Add(new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer));
            claims.Add(new Claim("token_endpoint_auth_signing_alg", model.TokenEndpointAuthSigningAlg ?? string.Empty));
            claims.Add(new Claim("token_endpoint_auth_method", model.TokenEndpointAuthMethod ?? string.Empty));
            claims.Add(new Claim("application_type", model.ApplicationType ?? string.Empty));
            claims.Add(new Claim("id_token_signed_response_alg", model.IdTokenSignedResponseAlg ?? string.Empty));
            claims.Add(new Claim("request_object_signing_alg", model.RequestObjectSigningAlg ?? string.Empty));
            claims.Add(new Claim("software_statement", ssa ?? string.Empty));
            claims.Add(new Claim("authorization_signed_response_alg", model.AuthorizationSignedResponseAlg ?? string.Empty));
            claims.Add(new Claim("authorization_encrypted_response_alg", model.AuthorizationEncryptedResponseAlg ?? string.Empty));
            claims.Add(new Claim("authorization_encrypted_response_enc", model.AuthorizationEncryptedResponseEnc ?? string.Empty));

            if (!string.IsNullOrEmpty(model.RedirectUris))
            {
                char[] delimiters = { ',', ' ' };
                var redirectUrisList = model.RedirectUris.Split(delimiters).ToList();
                claims.Add(new Claim("redirect_uris", JsonConvert.SerializeObject(redirectUrisList), JsonClaimValueTypes.JsonArray));
            }

            foreach (var grantType in model.GrantTypes.Split(','))
            {
                claims.Add(new Claim("grant_types", grantType));
            }

            if (!string.IsNullOrEmpty(model.ResponseTypes))
            {
                var responseTypeList = model.ResponseTypes.Split(',');
                claims.Add(new Claim("response_types", JsonConvert.SerializeObject(responseTypeList), JsonClaimValueTypes.JsonArray));
            }

            // algorithm to be adaptable.
            var jwt = new JwtSecurityToken(
                issuer: sp.SoftwareProductId,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new X509SigningCredentials(sp.SigningCertificate.X509Certificate, SecurityAlgorithms.RsaSsaPssSha256));

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(jwt);
        }

        public static (List<Claim> Claims, string ErrorMessages) CreateClaimsForUpdateDCRRequest(OidcDiscovery oidcDiscovery, string ssa, SoftwareProduct softwareProductConfig)
        {
            string errorMessage = string.Empty;

            // Error - Unable to perform DCR as there are no mutually supported values in the mandatory claim [CLAIM_NAME]
            const string ErrorMessage = "Unable to perform DCR as there are no mutually supported values in the mandatory claim";

            var claims = new List<Claim>
            {
                new("jti", Guid.NewGuid().ToString()),
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer),
                new("token_endpoint_auth_signing_alg", "PS256"),
                new("token_endpoint_auth_method", "private_key_jwt"),
                new("application_type", "web"),
                new("id_token_signed_response_alg", "PS256"),
                new("request_object_signing_alg", "PS256"),
                new("software_statement", ssa ?? string.Empty),
                new("grant_types", "client_credentials"),
                new("grant_types", "authorization_code"),
                new("grant_types", "refresh_token"),
            };

            var responseTypesList = oidcDiscovery.ResponseTypesSupported.Where(x => x.ToLower().Equals("code")).ToList();
            claims.Add(new Claim("response_types", JsonConvert.SerializeObject(responseTypesList), JsonClaimValueTypes.JsonArray));

            if (oidcDiscovery.AuthorizationSigningResponseAlgValuesSupported.Length == 0)
            {
                // Log error message to the mandatory claim missing
                errorMessage = ErrorMessage + " authorization_signed_response_alg";
                return (null, errorMessage);
            }

            if (!oidcDiscovery.AuthorizationSigningResponseAlgValuesSupported.Contains("PS256") && !oidcDiscovery.AuthorizationSigningResponseAlgValuesSupported.Contains("ES256"))
            {
                // Return the error
                errorMessage = ErrorMessage + " authorization_signed_response_alg";
                return (null, errorMessage);
            }

            if (oidcDiscovery.AuthorizationSigningResponseAlgValuesSupported.Contains("PS256"))
            {
                claims.Add(new Claim("authorization_signed_response_alg", "PS256"));
            }
            else if (oidcDiscovery.AuthorizationSigningResponseAlgValuesSupported.Contains("ES256"))
            {
                claims.Add(new Claim("authorization_signed_response_alg", "ES256"));
            }

            // Check if the enc is empty but a alg is specified.
            if ((oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported == null || oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported.Length == 0)
                && oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported != null && oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP-256")
                && oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP"))
            {
                errorMessage = ErrorMessage + " authorization_encrypted_response_enc";
                return (null, errorMessage);
            }

            if (oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported != null && oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported.Contains("A128CBC-HS256"))
            {
                claims.Add(new Claim("authorization_encrypted_response_enc", "A128CBC-HS256"));
            }
            else if (oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported != null && oidcDiscovery.AuthorizationEncryptionResponseEncValuesSupported.Contains("A256GCM"))
            {
                claims.Add(new Claim("authorization_encrypted_response_enc", "A256GCM"));
            }

            // Conditional: Optional for response_type "code" if authorization_encryption_enc_values_supported is present
            if (oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported != null && oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported.Length != 0)
            {
                if (oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP-256"))
                {
                    claims.Add(new Claim("authorization_encrypted_response_alg", "RSA-OAEP-256"));
                }
                else if (oidcDiscovery.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP"))
                {
                    claims.Add(new Claim("authorization_encrypted_response_alg", "RSA-OAEP"));
                }
            }

            char[] delimiters = [',', ' '];
            var redirectUrisList = softwareProductConfig.RedirectUris?.Split(delimiters).ToList();
            claims.Add(new Claim("redirect_uris", JsonConvert.SerializeObject(redirectUrisList), JsonClaimValueTypes.JsonArray));

            return (claims, errorMessage);
        }

        private async Task<DynamicClientRegistrationModel> DeleteRegistration(string clientId, string dataHolderBrandId)
        {
            var model = new DynamicClientRegistrationModel();
            var client = await _regRepository.GetRegistration(clientId, dataHolderBrandId);

            var sp = _config.GetSoftwareProductConfig();
            var dataHolderDiscovery = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(client.DataHolderBrandId);

            var accessToken = new AccessToken()
            {
                TokenEndpoint = dataHolderDiscovery.TokenEndpoint,
                ClientId = clientId,
                ClientCertificate = sp.ClientCertificate.X509Certificate,
                SigningCertificate = sp.SigningCertificate.X509Certificate,
                Scope = Constants.Scopes.CDR_DYNAMIC_CLIENT_REGISTRATION,
                RedirectUri = sp.RedirectUri,
                GrantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            };

            var tokenResponse = await _dhInfosecService.GetAccessToken(accessToken);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return model;
            }

            // Delete client from the Data Holder.  This is an optional endpoint and may not be implemented by the Data Holder.
            var dcrResponse = await _dcrService.DeleteRegistration(
                dataHolderDiscovery.RegistrationEndpoint,
                sp.ClientCertificate.X509Certificate,
                tokenResponse.Data.AccessToken,
                clientId);

            model.StatusCode = dcrResponse.StatusCode;
            model.Messages = dcrResponse.Message;
            model.ResponsePayload = dcrResponse.Payload;

            // Delete the client from the internal repository.
            await _regRepository.DeleteRegistration(clientId, dataHolderBrandId);

            return model;
        }

        private async Task<string> GetSSA(SoftwareProduct sp, DynamicClientRegistrationModel model)
        {
            var reg = _config.GetRegisterConfig();
            var tokenEndpoint = await _cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri);

            // Get an access token from the Register.
            var tokenResponse = await _regInfosecService.GetAccessToken(
                tokenEndpoint,
                sp.SoftwareProductId,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate);

            if (!tokenResponse.IsSuccessful)
            {
                model.StatusCode = tokenResponse.StatusCode;
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return null;
            }

            // Get an SSA from the Register.
            var ssaResponse = await _ssaService.GetSoftwareStatementAssertion(
                reg.MtlsBaseUri,
                model.SsaVersion,
                tokenResponse.Data.AccessToken,
                sp.ClientCertificate.X509Certificate,
                sp.BrandId,
                sp.SoftwareProductId,
                model.Industry);

            if (!ssaResponse.IsSuccessful)
            {
                model.StatusCode = ssaResponse.StatusCode;
                model.Messages = $"{ssaResponse.StatusCode} - {ssaResponse.Message}";
                return null;
            }

            return ssaResponse.Data;
        }

        private async Task PopulateFormDetail(DynamicClientRegistrationModel model)
        {
            var allowDynamicClientRegistration = await _featureManager.IsEnabledAsync(nameof(Feature.AllowDynamicClientRegistration));

            // Return any from the Registration table repository.
            var registrations = await _regRepository.GetRegistrations();
            model.DataHolderBrands = (await _dhRepository.GetDataHolderBrands())
                .OrderByMockDataHolders(allowDynamicClientRegistration)
                .Select(d => new SelectListItem(d.BrandName, d.DataHolderBrandId))
                .ToList();

            // Fill the brand name
            if (model.DataHolderBrands != null && model.DataHolderBrands.Count > 0)
            {
                var brandsDictionary = model.DataHolderBrands.ToDictionary(brand => brand.Value);
                foreach (var registration in registrations)
                {
                    registration.BrandName = brandsDictionary.TryGetValue(registration.DataHolderBrandId, out var brandValue) ?
                        brandValue.Text : string.Empty;
                }
            }

            model.Registrations = registrations.OrderBy(reg => reg.BrandName);

            // Set Selected item in picker
            if (model.DataHolderBrands.Count > 0 && !string.IsNullOrEmpty(model.DataHolderBrandId))
                {
                    var selected = model.DataHolderBrands.Find(d => d.Value.Equals(model.DataHolderBrandId, StringComparison.OrdinalIgnoreCase));
                    if (selected != null)
                    {
                        selected.Selected = true;
                    }
                }

            if (!allowDynamicClientRegistration)
            {
                // Return any from the DcrMessage table in the repository.
                var dcrMessages = await _regRepository.GetDcrMessageRegistrations();
                model.FailedDCRMessages = dcrMessages.Where(message => message.MessageState == Message.DCRFailed.ToString()).OrderBy(message => message.BrandName);
            }
        }

        private void SetViewModelDefaults(DynamicClientRegistrationModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            model.SoftwareProductId = sp.SoftwareProductId;
            model.RedirectUris = sp.RedirectUris;
            model.Scope = sp.Scope;
            model.TokenEndpointAuthSigningAlg = sp.DefaultSigningAlgorithm;
            model.TokenEndpointAuthMethod = "private_key_jwt";
            model.GrantTypes = "client_credentials,authorization_code,refresh_token";
            model.ResponseTypes = "code";
            model.ApplicationType = "web";
            model.IdTokenSignedResponseAlg = sp.DefaultSigningAlgorithm;
            model.RequestObjectSigningAlg = sp.DefaultSigningAlgorithm;
            model.AuthorizationSignedResponseAlg = sp.DefaultSigningAlgorithm;
            model.AuthorizationEncryptedResponseAlg = string.Empty;
            model.AuthorizationEncryptedResponseEnc = string.Empty;
            model.SsaVersion = "3";
            model.Messages = string.Empty;
        }

        private void SetViewModel(DynamicClientRegistrationModel model, Registration client)
        {
            var sp = _config.GetSoftwareProductConfig();
            model.DataHolderBrandId = client.DataHolderBrandId;
            model.SoftwareProductId = sp.SoftwareProductId;
            model.RedirectUris = sp.RedirectUris;
            model.Scope = sp.Scope;
            model.TokenEndpointAuthSigningAlg = sp.DefaultSigningAlgorithm;
            model.TokenEndpointAuthMethod = "private_key_jwt";
            model.GrantTypes = "client_credentials,authorization_code,refresh_token";
            model.ResponseTypes = string.Join(",", client.ResponseTypes);
            model.ApplicationType = "web";
            model.IdTokenSignedResponseAlg = sp.DefaultSigningAlgorithm;
            model.RequestObjectSigningAlg = sp.DefaultSigningAlgorithm;
            model.AuthorizationSignedResponseAlg = client.AuthorizationSignedResponseAlg;
            model.AuthorizationEncryptedResponseAlg = client.AuthorizationEncryptedResponseAlg;
            model.AuthorizationEncryptedResponseEnc = client.AuthorizationEncryptedResponseEnc;
            model.SsaVersion = "3";
            model.Messages = "Waiting...";
        }
    }
}
