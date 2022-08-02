using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Services.DataHolder
{
    public class DynamicClientRegistrationService : BaseService, IDynamicClientRegistrationService
    {

        public DynamicClientRegistrationService(
            IConfiguration config,
            ILogger<DynamicClientRegistrationService> logger,
            IServiceConfiguration serviceConfiguration) : base(config, logger, serviceConfiguration)
        {
        }

        public async Task<DcrResponse> DeleteRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId)
        {
            _logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(DeleteRegistration)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            _logger.LogDebug("Deleting registration from Data Holder: {registrationEndpoint}.  Client ID: {clientId}.  Client Certificate: {thumbprint}", registrationEndpoint, clientId, clientCertificate.Thumbprint);

            // Make the request to the data holder's registration endpoint.
            var uri = string.Concat(registrationEndpoint.TrimEnd('/'), "/", clientId);
            var response = await client.DeleteAsync(EnsureValidEndpoint(uri));
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            return new DcrResponse
            {
                StatusCode = response.StatusCode,
                Payload = body,
                Message = response.IsSuccessStatusCode ? "Registration deleted." : $"Failed to delete registration: {body}"
            };
        }

        public async Task<DcrResponse> GetRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId)
        {
            _logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(GetRegistration)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            // Make the request to the data holder's registration endpoint.
            var uri = string.Concat(registrationEndpoint.TrimEnd('/'), "/", clientId);
            var response = await client.GetAsync(EnsureValidEndpoint(uri));
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            return new DcrResponse()
            {
                StatusCode = response.StatusCode,
                Payload = body,
                Message = response.IsSuccessStatusCode ? "Registration retrieved successfully." : $"Failed to retrieve registration: {body}"
            };
        }

        public async Task<DcrResponse> Register(
            string registrationEndpoint, 
            X509Certificate2 clientCertificate, 
            string payload)
        {
            _logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(Register)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate);

            _logger.LogDebug("Registering with Data Holder: {registrationEndpoint}.  Client Certificate: {thumbprint}", registrationEndpoint, clientCertificate.Thumbprint);

            // Create the post content.
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/jwt");

            // Make the request to the data holder's registration endpoint.
            var response = await client.PostAsync(EnsureValidEndpoint(registrationEndpoint), content);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            return new DcrResponse()
            {
                Data = JsonConvert.DeserializeObject<Registration>(body),
                StatusCode = response.StatusCode,
                Message = response.IsSuccessStatusCode ? "Registration successful." : $"Failed to register: {body}",
                Payload = body
            };
        }

        public async Task<DcrResponse> UpdateRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId,
            string payload)
        {
            _logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(UpdateRegistration)}.");

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken);

            _logger.LogDebug("Updating registration with Data Holder: {registrationEndpoint}.  Client ID: {clientId}.  Client Certificate: {thumbprint}", registrationEndpoint, clientId, clientCertificate.Thumbprint);

            // Create the put content.
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/jwt");

            // Make the request to the data holder's registration endpoint.
            var uri = string.Concat(registrationEndpoint.TrimEnd('/'), "/", clientId);
            var response = await client.PutAsync(EnsureValidEndpoint(uri), content);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            return new DcrResponse()
            {
                StatusCode = response.StatusCode,
                Message = response.IsSuccessStatusCode ? "Registration update successful." : $"Failed to update registration: {body}",
                Payload = body,
            };
        }
    }
}
