using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Services.DataHolder
{
    public class DynamicClientRegistrationService : BaseService, IDynamicClientRegistrationService
    {
        public DynamicClientRegistrationService(
            IConfiguration config,
            ILogger<DynamicClientRegistrationService> logger,
            IServiceConfiguration serviceConfiguration)
            : base(config, logger, serviceConfiguration)
        {
        }

        public async Task<DcrResponse> DeleteRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId)
        {
            this.Logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(this.DeleteRegistration)}.");

            // Setup the http client.
            var client = this.GetHttpClient(clientCertificate, accessToken);

            this.Logger.LogDebug("Deleting registration from Data Holder: {RegistrationEndpoint}.  Client ID: {ClientId}.  Client Certificate: {Thumbprint}", registrationEndpoint, clientId, clientCertificate.Thumbprint);

            // Make the request to the data holder's registration endpoint.
            var uri = string.Concat(registrationEndpoint.TrimEnd('/'), "/", clientId);
            var response = await client.DeleteAsync(this.EnsureValidEndpoint(uri));
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            return new DcrResponse
            {
                StatusCode = response.StatusCode,
                Payload = body,
                Message = response.IsSuccessStatusCode ? "Registration deleted." : $"Failed to delete registration: {body}",
            };
        }

        public async Task<DcrResponse> GetRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId)
        {
            this.Logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(this.GetRegistration)}.");

            // Setup the http client.
            var client = this.GetHttpClient(clientCertificate, accessToken);

            // Make the request to the data holder's registration endpoint.
            var uri = string.Concat(registrationEndpoint.TrimEnd('/'), "/", clientId);
            var response = await client.GetAsync(this.EnsureValidEndpoint(uri));
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            return new DcrResponse()
            {
                StatusCode = response.StatusCode,
                Payload = body,
                Message = response.IsSuccessStatusCode ? "Registration retrieved successfully." : $"Failed to retrieve registration: {body}",
            };
        }

        public async Task<DcrResponse> Register(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string payload)
        {
            this.Logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(this.Register)}.");

            // Setup the http client.
            var client = this.GetHttpClient(clientCertificate);

            this.Logger.LogDebug("Registering with Data Holder: {RegistrationEndpoint}.  Client Certificate: {Thumbprint}", registrationEndpoint, clientCertificate.Thumbprint);

            // Create the post content.
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/jwt");

            // Make the request to the data holder's registration endpoint.
            var response = await client.PostAsync(this.EnsureValidEndpoint(registrationEndpoint), content);
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            return new DcrResponse()
            {
                Data = JsonConvert.DeserializeObject<Registration>(body),
                StatusCode = response.StatusCode,
                Message = response.IsSuccessStatusCode ? "Registration successful." : $"Failed to register: {body}",
                Payload = body,
            };
        }

        public async Task<DcrResponse> UpdateRegistration(
            string registrationEndpoint,
            X509Certificate2 clientCertificate,
            string accessToken,
            string clientId,
            string payload)
        {
            this.Logger.LogDebug($"Request received to {nameof(DynamicClientRegistrationService)}.{nameof(this.UpdateRegistration)}.");

            // Setup the http client.
            var client = this.GetHttpClient(clientCertificate, accessToken);

            this.Logger.LogDebug("Updating registration with Data Holder: {RegistrationEndpoint}.  Client ID: {ClientId}.  Client Certificate: {Thumbprint}", registrationEndpoint, clientId, clientCertificate.Thumbprint);

            // Create the put content.
            var content = new StringContent(payload);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/jwt");

            // Make the request to the data holder's registration endpoint.
            var uri = string.Concat(registrationEndpoint.TrimEnd('/'), "/", clientId);
            var response = await client.PutAsync(this.EnsureValidEndpoint(uri), content);
            var body = await response.Content.ReadAsStringAsync();

            this.Logger.LogDebug("Response: {StatusCode}.  Body: {Body}", response.StatusCode, body);

            return new DcrResponse()
            {
                StatusCode = response.StatusCode,
                Message = response.IsSuccessStatusCode ? "Registration update successful." : $"Failed to update registration: {body}",
                Payload = body,
            };
        }
    }
}
