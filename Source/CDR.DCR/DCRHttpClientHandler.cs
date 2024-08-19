using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace CDR.DCR
{
    public class DcrHttpClientHandler : HttpClientHandler
    {
        public DcrHttpClientHandler(
            IOptions<DcrOptions> options,
            ILogger<DcrHttpClientHandler> logger)
        {
            var dcrOptions = options.Value;
            logger.LogInformation("Loading the client certificate...");

            byte[] clientCertBytes = Convert.FromBase64String(dcrOptions.Client_Certificate);
            X509Certificate2 clientCertificate = new(clientCertBytes, dcrOptions.Client_Certificate_Password, X509KeyStorageFlags.MachineKeySet);
            logger.LogInformation("Client certificate loaded: {thumbprint}", clientCertificate.Thumbprint);


            ClientCertificates.Add(clientCertificate);

            if (dcrOptions.Ignore_Server_Certificate_Errors)
            {
                ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }
        }
    }
}