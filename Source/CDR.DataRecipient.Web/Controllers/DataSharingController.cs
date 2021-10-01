using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CDR.DataRecipient.Models;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("data-sharing")]
    public class DataSharingController : Controller
    {
        private const string HEADER_INJECT_CDR_ARRANGEMENT_ID = "x-inject-cdr-arrangement-id";

        private readonly ILogger<DataSharingController> _logger;
        private readonly IConfiguration _config;
        private readonly IConsentsRepository _consentsRepository;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly IInfosecService _infosecService;

		public DataSharingController(
           IConfiguration config,
           ILogger<DataSharingController> logger,
           IConsentsRepository consentsRepository,
           IDataHoldersRepository dhRepository,
           IInfosecService infosecService)
        {
            _logger = logger;
            _config = config;
            _consentsRepository = consentsRepository;
            _dhRepository = dhRepository;
            _infosecService = infosecService;
		}

        [HttpGet]
        public IActionResult Index()
        {
            var model = new DataSharingModel();
            PopulateModel(model);
            return View(model);
        }

        /// <summary>
        /// Endpoint that outputs a customised version of the CDS swagger in order to utilise the SwaggerUI.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("swagger")]
        public async Task Swagger()
        {
            // Get the CDS swagger file.
            var cdsSwagger = _config["ConsumerDataStandardsSwagger"];
            var sp = _config.GetSoftwareProductConfig();
            var client = new HttpClient();
            var cdsSwaggerResponse = await client.GetAsync(cdsSwagger);
            var cdsSwaggerJson = await cdsSwaggerResponse.Content.ReadAsStringAsync();

            // Replace the host and base path in the CDS swagger file to point to the mock data recipient proxy endpoint
            // in order to proxy all swagger requests via the mock data recipient.
            var json = JObject.Parse(cdsSwaggerJson);
            json["host"] = new Uri(sp.RecipientBaseUri).Host;
            json["basePath"] = "/data-sharing/proxy/cds-au/v1";

            // Return the updated swagger file.
            Response.ContentType = "application/json";
            await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(json.ToString()));
        }

        /// <summary>
        /// The Proxy action captures requests from the SwaggerUI and forwards them on to the appropriate data holder.
        /// </summary>
        /// <returns></returns>
        [Route("proxy/{**path}")]
        public async Task Proxy()
        {
            var sp = _config.GetSoftwareProductConfig();

            var cdrArrangement = await GetCdrArrangement(this.Request);
            var dh = await GetDataHolder(cdrArrangement);
            var isPublic = IsPublic(this.Request.Path);
            var baseUri = isPublic ? dh.EndpointDetail.PublicBaseUri : dh.EndpointDetail.ResourceBaseUri;

            // Build the Http Request to the data holder.
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // Provide the data recipient's client certificate for a non-public endpoint.
            if (!isPublic)
            {
                clientHandler.ClientCertificates.Add(sp.ClientCertificate.X509Certificate);
            }

            var client = new HttpClient(clientHandler);
            var requestUri = String.Concat(baseUri, this.Request.Path.ToString().Replace("/data-sharing/proxy", ""), this.Request.QueryString);
            var request = new HttpRequestMessage()
            {
                 Method = this.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Get : HttpMethod.Post,
                 RequestUri = new Uri(requestUri)
            };

            // Don't add the host header or there will be CORS errors. This has to be added to the where.
            foreach (var header in this.Request.Headers.Keys.Where(
                h => !h.StartsWith(":") && !h.StartsWith("x-inject-") && h != "Host"))
            {
                request.Headers.Add(header, this.Request.Headers[header].ToString());
            }

            // Provide an access token for a non-public endpoint.
            if (!isPublic)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cdrArrangement.AccessToken);
            }

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Return the raw JSON response.
            Response.ContentType = "application/json";
            Response.StatusCode = response.StatusCode.ToInt();
            await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(body));
        }

        private async Task<DataHolderBrand> GetDataHolder(ConsentArrangement cdrArrangement)
        {
            return await _dhRepository.GetDataHolderBrand(cdrArrangement.DataHolderBrandId);
        }

        private async Task<ConsentArrangement> GetCdrArrangement(HttpRequest request)
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

            return await _consentsRepository.GetConsent(cdrArrangementId);
        }

        /// <summary>
        /// Return the list of cdr arrangements so that a select element can be built.
        /// </summary>
        [HttpGet]
        [Route("cdr-arrangements")]
        public async Task<IList<KeyValuePair<string, string>>> GetCdrArrangements()
        {
            var consents = await _consentsRepository.GetConsents();
            return consents
                .Select(c => new KeyValuePair<string, string>(c.CdrArrangementId, $"{c.CdrArrangementId} (DH Brand: {c.DataHolderBrandId})"))
                .ToList();
        }

        /// <summary>
        /// Determine if the target request is for a public endpoint, or a resource endpoint.
        /// </summary>
        /// <param name="requestPath">Request Path</param>
        /// <returns>True if the request is for the /discovery or /banking/products endpoints, otherwise false</returns>
        public bool IsPublic(string requestPath)
        {
            return requestPath.Contains("/cds-au/v1/discovery", StringComparison.OrdinalIgnoreCase)
                || requestPath.Contains("/cds-au/v1/banking/products", StringComparison.OrdinalIgnoreCase);
        }

        private void PopulateModel(DataSharingModel model)
        {
            model.CdsSwaggerLocation = _config["ConsumerDataStandardsSwagger"];
        }
    }
}
