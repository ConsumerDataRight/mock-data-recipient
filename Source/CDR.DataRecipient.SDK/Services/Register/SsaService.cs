﻿using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Enumerations;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public class SsaService : BaseService, ISsaService
    {
        public SsaService(
            IConfiguration config,
            ILogger<SsaService> logger,
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
        }

        public async Task<Response<string>> GetSoftwareStatementAssertion(
            string mtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string brandId,
            string softwareProductId,
            Industry industry)
        {
            var ssaResponse = new Response<string>();

            this.Logger.LogDebug($"Request received to {nameof(SsaService)}.{nameof(this.GetSoftwareStatementAssertion)}.");

            // Setup the request to the get ssa endpoint.
            var ssaEndpoint = $"{mtlsBaseUri}/cdr-register/v1/{industry.ToPath()}/data-recipients/brands/{brandId}/software-products/{softwareProductId}/ssa";

            // Setup the http client.
            var client = this.GetHttpClient(clientCertificate, accessToken, version);

            this.Logger.LogDebug("Requesting SSA from Register: {SsaEndpoint}.  Client Certificate: {Thumbprint}", ssaEndpoint, clientCertificate.Thumbprint);

            // Make the request to the get data holder brands endpoint.
            var response = await client.GetAsync(this.EnsureValidEndpoint(ssaEndpoint));
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Get SSA Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            ssaResponse.StatusCode = response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                ssaResponse.Data = body;
                ssaResponse.Message = "SSA Generated";
            }
            else
            {
                ssaResponse.Message = $"Failed to generate an SSA: {body}";
            }

            return ssaResponse;
        }
    }
}
