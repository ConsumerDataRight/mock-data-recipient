using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public class MetadataService : BaseService, IMetadataService
    {

        public MetadataService(
            IConfiguration config,
            ILogger<MetadataService> logger) : base(config, logger)
        {
        }

        public async Task<Response<IList<DataHolderBrand>>> GetDataHolderBrands(
            string registerMtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string softwareProductId,
            int? page = null,
            int? pageSize = null)
        {
            var dhBrandsResponse = new Response<IList<DataHolderBrand>>();

            _logger.LogDebug($"Request received to {nameof(MetadataService)}.{nameof(GetDataHolderBrands)}.");

            // Setup the request to the get data holder brands endpoint.
            var endpoint = $"{registerMtlsBaseUri.TrimEnd('/')}/cdr-register/v1/banking/data-holders/brands";

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken, version);

            _logger.LogDebug($"Requesting data holder brands from Register: {endpoint}.  Client Certificate: {clientCertificate.Thumbprint}");

            // Add the query parameters.
            if (page.HasValue)
            {
                endpoint = endpoint.AppendQueryString("page", page.ToString());
            }

            if (pageSize.HasValue)
            {
                endpoint = endpoint.AppendQueryString("page-size", pageSize.ToString());
            }

            // Make the request to the get data holder brands endpoint.
            var response = await client.GetAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug($"Get Data Holder Brands Response: {response.StatusCode}.  Body: {body}");

            if (response.IsSuccessStatusCode)
            {
                dhBrandsResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Response<IList<DataHolderBrand>>>(body);
                dhBrandsResponse.StatusCode = response.StatusCode;
            }
            else
            {
                dhBrandsResponse.StatusCode = response.StatusCode;
                dhBrandsResponse.Message = body;
            }

            return dhBrandsResponse;
        }
    }
}
