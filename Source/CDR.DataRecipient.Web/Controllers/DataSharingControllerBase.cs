using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    public abstract class DataSharingControllerBase : Controller
    {
        protected const string HEADER_INJECT_CDR_ARRANGEMENT_ID = "x-inject-cdr-arrangement-id";

        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly IInfosecService _infosecService;
        private readonly ILogger<DataSharingControllerBase> _logger;
        private readonly List<string> _allowedHeaders;

        protected DataSharingControllerBase(
            IConfiguration config,
            IDistributedCache cache,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IInfosecService infosecService,
            ILogger<DataSharingControllerBase> logger,
            IHttpClientFactory clientFactory)
        {
            this._config = config;
            this._cache = cache;
            this._consentsRepository = consentsRepository;
            this._dhRepository = dhRepository;
            this._infosecService = infosecService;
            this._logger = logger;
            this._allowedHeaders = [.. this._config.GetValue<string>(Constants.ConfigurationKeys.AllowSpecificHeaders).Split(',')];
            this._clientFactory = clientFactory;
        }

        protected abstract string BasePath { get; }

        protected abstract string IndustryName { get; }

        /// <summary>
        /// This is the group name of the APIs according to the standards. e.g. "Banking" API, "Common" API.
        /// </summary>
        protected abstract string ApiGroupName { get; }

        protected abstract string CdsSwaggerLocation { get; }

        protected IDistributedCache Cache => this._cache;

        protected IConfiguration Config => this._config;

        protected IInfosecService InfosecService => this._infosecService;

        protected List<string> AllowedHeaders => this._allowedHeaders;

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult Index()
        {
            var model = new DataSharingModel();
            this.PopulateModel(model);
            return this.View("DataSharing", model);
        }

        /// <summary>
        /// Endpoint that outputs a customised version of the CDS swagger in order to utilise the SwaggerUI.
        /// </summary>
        /// <returns>Task.</returns>
        [HttpGet]
        [Route("swagger")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task Swagger()
        {
            var sp = this._config.GetSoftwareProductConfig();
            var client = this._clientFactory.CreateClient();
            Uri uri = new Uri(sp.RecipientBaseUri);

            // Get the CDS swagger file.
            var cdsSwaggerResponse = await client.GetAsync(this.CdsSwaggerLocation);
            var cdsSwaggerJson = await cdsSwaggerResponse.Content.ReadAsStringAsync();

            // Replace the host and base path in the CDS Banking swagger file to point to the mock data recipient proxy endpoint
            // in order to proxy all swagger requests via the mock data recipient.
            var jsonObj = JObject.Parse(cdsSwaggerJson);
            jsonObj = this.PrepareSwaggerJson(jsonObj, uri);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonObj.ToString());

            // Return the updated swagger file.
            this.Response.ContentType = "application/json";
            await this.Response.BodyWriter.WriteAsync(jsonBytes);
        }

        /// <summary>
        /// The Proxy action captures requests from the SwaggerUI and forwards them on to the appropriate data holder.
        /// </summary>
        /// <returns>Task.</returns>
        [Route("proxy/{**path}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task Proxy()
        {
            var cdrArrangement = await this.GetCdrArrangement(this.Request);
            if (cdrArrangement == null)
            {
                this.Response.ContentType = "application/json";
                this.Response.StatusCode = StatusCodes.Status400BadRequest;
                string rtnMsg = @"{""Error"":""Please select an Agreement""}";
                await this.Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(rtnMsg));
                return;
            }

            var requestPath = this.Request.Path.ToString().Replace($"/{this.BasePath}/proxy", string.Empty);
            if (!this.IsValidRequestPath(requestPath))
            {
                this.Response.ContentType = "application/json";
                this.Response.StatusCode = StatusCodes.Status400BadRequest;
                string rtnMsg = @"{""Error"":""Invalid request path""}";
                await this.Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(rtnMsg));
                return;
            }

            var dh = await this.GetDataHolder(cdrArrangement);
            var isPublic = this.IsPublic(this.Request.Path);
            var baseUri = isPublic ? dh.EndpointDetail.PublicBaseUri : dh.EndpointDetail.ResourceBaseUri;

            this._logger.LogDebug("Proxying call to Data Holder: {DataHolderBrandId}.  Is Public: {IsPublic}.  Base Uri: {BaseUri}.  Path: {Path}.", dh.DataHolderBrandId, isPublic, baseUri, this.Request.Path);

            // Build the Http Request to the data holder. Use HttpClientFactory to get the name client instead of re-creating the HttpClient everytime.
            var client = isPublic ? this._clientFactory.CreateClient("PublicDataHolderClient") : this._clientFactory.CreateClient("PrivateDataHolderClient");

            var requestUri = string.Concat(baseUri, requestPath, this.Request.QueryString);
            var request = new HttpRequestMessage()
            {
                Method = this.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Get : HttpMethod.Post,
                RequestUri = new Uri(requestUri),
            };

            // Add the body to the request for POST requests.
            if (request.Method.Equals(HttpMethod.Post))
            {
                using var reader = new StreamReader(this.Request.Body);

                // You now have the body string raw
                var requestBody = await reader.ReadToEndAsync();
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            // Don't add the host header or there will be CORS errors. This has to be added to the where.
            foreach (var header in this.Request.Headers.Keys.Where(h => this._allowedHeaders.Contains(h, StringComparer.OrdinalIgnoreCase)))
            {
                request.Headers.Add(header, this.Request.Headers[header].ToString());
            }

            // Provide an access token for a non-public endpoint.
            if (!isPublic)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cdrArrangement.AccessToken);
            }

            if (this._config.IsEnforcingHttpsEndpoints() && !request.RequestUri.IsHttps())
            {
                throw new NoHttpsException();
            }

            this._logger.LogDebug("Making request to: {RequestUri}.", request.RequestUri);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Return the raw JSON response.
            this.Response.ContentType = "application/json";
            this.Response.StatusCode = response.StatusCode.ToInt();
            await this.Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Return the list of cdr arrangements so that a select element can be built.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [HttpGet]
        [Route("cdr-arrangements")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IList<KeyValuePair<string, string>>> GetCdrArrangements()
        {
            var consents = await this.GetConsents(this.HttpContext.User.GetUserId(), industry: this.IndustryName);
            return consents
                .Select(c => new KeyValuePair<string, string>(c.CdrArrangementId, $"{c.CdrArrangementId} (DH Brand: {c.BrandName} {c.DataHolderBrandId})"))
                .ToList();
        }

        protected virtual async Task<IEnumerable<ConsentArrangement>> GetConsents(string userId, string industry = null)
        {
            return await this._consentsRepository.GetConsents(string.Empty, string.Empty, userId, industry);
        }

        protected virtual void PopulateModel(DataSharingModel model)
        {
            model.BasePath = this.BasePath;
            model.ApiGroupName = this.ApiGroupName;
            model.CdsSwaggerLocation = this.CdsSwaggerLocation;
        }

        protected virtual async Task<DataHolderBrand> GetDataHolder(ConsentArrangement cdrArrangement)
        {
            return await this._dhRepository.GetDataHolderBrand(cdrArrangement.DataHolderBrandId);
        }

        protected virtual async Task<ConsentArrangement> GetCdrArrangement(HttpRequest request)
        {
            // Get the cdr arrangement id from the http header.
#pragma warning disable S6932 // Use model binding instead of reading raw request data
            if (!request.Headers.TryGetValue(HEADER_INJECT_CDR_ARRANGEMENT_ID, out var cdrArrangementId))
            {
                return null;
            }
#pragma warning restore S6932 // Use model binding instead of reading raw request data

            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                return null;
            }

            return await this._consentsRepository.GetConsentByArrangement(cdrArrangementId);
        }

        protected abstract JObject PrepareSwaggerJson(JObject json, Uri uri);

        protected abstract bool IsPublic(string requestPath);

        protected virtual bool IsValidRequestPath(string requestPath)
        {
            // Validate the request path prefix.
            var validPath = $"/cds-au/v1/{this.IndustryName.ToLower()}";
            if (requestPath.StartsWith(validPath))
            {
                return true;
            }

            return false;
        }
    }
}
