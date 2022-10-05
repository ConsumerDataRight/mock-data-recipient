using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Exceptions;
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    public abstract class DataSharingControllerBase : Controller
    {
        protected const string HEADER_INJECT_CDR_ARRANGEMENT_ID = "x-inject-cdr-arrangement-id";
        protected readonly IConfiguration _config;
        protected readonly IDistributedCache _cache;
        protected readonly IConsentsRepository _consentsRepository;
        protected readonly IDataHoldersRepository _dhRepository;
        protected readonly IInfosecService _infosecService;
        protected readonly ILogger<DataSharingControllerBase> _logger;

        protected abstract string BasePath { get; }
        protected abstract string IndustryName { get; }
        protected abstract string CdsSwaggerLocation { get; }

        protected List<string> _allowedHeaders;

        protected DataSharingControllerBase(
            IConfiguration config,
            IDistributedCache cache,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IInfosecService infosecService,
            ILogger<DataSharingControllerBase> logger)
        {
            _config = config;
            _cache = cache;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _infosecService = infosecService;
            _logger = logger;
            _allowedHeaders = _config.GetValue<string>(Constants.ConfigurationKeys.AllowSpecificHeaders).Split(',').ToList();
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult Index()
        {
            var model = new DataSharingModel();
            PopulateModel(model);
            return View("DataSharing", model);
        }

        /// <summary>
        /// Endpoint that outputs a customised version of the CDS swagger in order to utilise the SwaggerUI.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("swagger")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task Swagger()
        {
            var sp = _config.GetSoftwareProductConfig();
            var client = new HttpClient();
            Uri uri = new Uri(sp.RecipientBaseUri);

            // Get the CDS swagger file.
            var cdsSwaggerResponse = await client.GetAsync(this.CdsSwaggerLocation);
            var cdsSwaggerJson = await cdsSwaggerResponse.Content.ReadAsStringAsync();

            // Replace the host and base path in the CDS Banking swagger file to point to the mock data recipient proxy endpoint
            // in order to proxy all swagger requests via the mock data recipient.
            var jsonObj = JObject.Parse(cdsSwaggerJson);
            jsonObj = PrepareSwaggerJson(jsonObj, uri);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonObj.ToString());

            // Return the updated swagger file.
            Response.ContentType = "application/json";
            await Response.BodyWriter.WriteAsync(jsonBytes);
        }

        /// <summary>
        /// The Proxy action captures requests from the SwaggerUI and forwards them on to the appropriate data holder.
        /// </summary>
        /// <returns></returns>
        [Route("proxy/{**path}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task Proxy()
        {
            var sp = _config.GetSoftwareProductConfig();

            var cdrArrangement = await GetCdrArrangement(this.Request);
            if (cdrArrangement == null)
            {
                Response.ContentType = "application/json";
                Response.StatusCode = StatusCodes.Status400BadRequest;
                string rtnMsg = @"{""Error"":""Please select an Agreement""}";
                await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(rtnMsg));
                return;
            }

            var requestPath = this.Request.Path.ToString().Replace($"/{this.BasePath}/proxy", "");
            if (!IsValidRequestPath(requestPath))
            {
                Response.ContentType = "application/json";
                Response.StatusCode = StatusCodes.Status400BadRequest;
                string rtnMsg = @"{""Error"":""Invalid request path""}";
                await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(rtnMsg));
                return;
            }

            var dh = await GetDataHolder(cdrArrangement);
            var isPublic = IsPublic(this.Request.Path);
            var baseUri = isPublic ? dh.EndpointDetail.PublicBaseUri : dh.EndpointDetail.ResourceBaseUri;

            _logger.LogDebug("Proxying call to Data Holder: {DataHolderBrandId}.  Is Public: {isPublic}.  Base Uri: {baseUri}.  Path: {path}.", dh.DataHolderBrandId, isPublic, baseUri, this.Request.Path);

            // Build the Http Request to the data holder.
            var clientHandler = new HttpClientHandler();
            var acceptAnyServerCertificate = _config.IsAcceptingAnyServerCertificate();
            if (acceptAnyServerCertificate)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            // Provide the data recipient's client certificate for a non-public endpoint.
            if (!isPublic)
            {
                clientHandler.ClientCertificates.Add(sp.ClientCertificate.X509Certificate);
            }

            var client = new HttpClient(clientHandler);
            var requestUri = String.Concat(baseUri, requestPath, this.Request.QueryString);
            var request = new HttpRequestMessage()
            {
                Method = this.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Get : HttpMethod.Post,
                RequestUri = new Uri(requestUri)
            };

            // Don't add the host header or there will be CORS errors. This has to be added to the where.
            foreach (var header in this.Request.Headers.Keys.Where(h => _allowedHeaders.Contains(h, StringComparer.OrdinalIgnoreCase)))
            {
                request.Headers.Add(header, this.Request.Headers[header].ToString());
            }

            // Provide an access token for a non-public endpoint.
            if (!isPublic)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cdrArrangement.AccessToken);
            }

            if (_config.IsEnforcingHttpsEndpoints() && !request.RequestUri.IsHttps())
            {
                throw new NoHttpsException();
            }

            _logger.LogDebug("Making request to: {RequestUri}.", request.RequestUri);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Return the raw JSON response.
            Response.ContentType = "application/json";
            Response.StatusCode = response.StatusCode.ToInt();
            await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Return the list of cdr arrangements so that a select element can be built.
        /// </summary>
        [HttpGet]
        [Route("cdr-arrangements")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IList<KeyValuePair<string, string>>> GetCdrArrangements()
        {
            var consents = await GetConsents(HttpContext.User.GetUserId(), industry: this.IndustryName);
            return consents
                .Select(c => new KeyValuePair<string, string>(c.CdrArrangementId, $"{c.CdrArrangementId} (DH Brand: {c.BrandName} {c.DataHolderBrandId})"))
                .ToList();
        }

        protected virtual async Task<IEnumerable<ConsentArrangement>> GetConsents(string userId, string industry = null)
        {
            return await _consentsRepository.GetConsents("", "", userId, industry);
        }

        protected virtual void PopulateModel(DataSharingModel model)
        {
            model.BasePath = this.BasePath;
            model.IndustryName = this.IndustryName;
            model.CdsSwaggerLocation = this.CdsSwaggerLocation;
        }

        protected virtual async Task<DataHolderBrand> GetDataHolder(ConsentArrangement cdrArrangement)
        {
            return await _dhRepository.GetDataHolderBrand(cdrArrangement.DataHolderBrandId);
        }

        protected virtual async Task<ConsentArrangement> GetCdrArrangement(HttpRequest request)
        {
            // Get the cdr arrangement id from the http header.
            if (!request.Headers.ContainsKey(HEADER_INJECT_CDR_ARRANGEMENT_ID))
            {
                return null;
            }

            var cdrArrangementId = request.Headers[HEADER_INJECT_CDR_ARRANGEMENT_ID];

            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                return null;
            }

            return await _consentsRepository.GetConsentByArrangement(cdrArrangementId);
        }

        protected abstract JObject PrepareSwaggerJson(JObject json, Uri uri);
        protected abstract bool IsPublic(string requestPath);

        protected virtual bool IsValidRequestPath(string requestPath)
        {
            // Validate the request path prefix.
            var validPath = $"/cds-au/v1/{this.IndustryName.ToLower()}/";
            if (requestPath.StartsWith(validPath))
            {
                return true;
            }

            return false;
        }
    }
}
