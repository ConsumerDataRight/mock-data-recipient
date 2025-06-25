using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Enumerations;
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
            ILogger<MetadataService> logger,
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
        }

        public async Task<(string, System.Net.HttpStatusCode, string)> GetDataHolderBrands(
            string registerMtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string softwareProductId,
            Industry industry,
            int? page = null,
            int? pageSize = null)
        {
            this.Logger.LogDebug($"Request received to {nameof(MetadataService)}.{nameof(this.GetDataHolderBrands)}.");

            // Setup the request to the get data holder brands endpoint.
            var endpoint = $"{registerMtlsBaseUri.TrimEnd('/')}/cdr-register/v1/{industry.ToPath()}/data-holders/brands";

            // Setup the http client.
            var client = this.GetHttpClient(clientCertificate, accessToken, version);

            this.Logger.LogDebug("Requesting data holder brands from Register: {Endpoint}.  Client Certificate: {Thumbprint}", endpoint, clientCertificate.Thumbprint);

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
            var response = await client.GetAsync(this.EnsureValidEndpoint(endpoint));
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Get Data Holder Brands Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            return (body, response.StatusCode, response.ReasonPhrase.ToString());
        }

        public async Task<(string, System.Net.HttpStatusCode, string)> GetDataRecipients(
            string registerTlsBaseUri,
            string version,
            Industry industry)
        {
            this.Logger.LogDebug($"Request received to {nameof(MetadataService)}.{nameof(this.GetDataRecipients)}.");

            // Setup the request to the get data recipients endpoint.
            var endpoint = $"{registerTlsBaseUri.TrimEnd('/')}/cdr-register/v1/{industry.ToPath()}/data-recipients";

            // Setup the http client.
            var client = this.GetHttpClient(version: version);

            this.Logger.LogDebug("Requesting data recipients from Register: {Endpoint}.", endpoint);

            // Make the request to the get data recipients endpoint.
            var response = await client.GetAsync(this.EnsureValidEndpoint(endpoint));
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Get Data Recipients Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            return (body, response.StatusCode, response.ReasonPhrase.ToString());
        }
    }
}
