using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace CDR.DiscoverDataHolders
{
    public class DHHttpClientHandler : HttpClientHandler
    {
        public DHHttpClientHandler(
            IOptions<DHOptions> options,
            ILogger<DHHttpClientHandler> logger)
        {
            var dhOptions = options.Value;
            logger.LogInformation("Loading the client certificate...");

            byte[] clientCertBytes = Convert.FromBase64String(dhOptions.Client_Certificate);
            X509Certificate2 clientCertificate = new(clientCertBytes, dhOptions.Client_Certificate_Password, X509KeyStorageFlags.MachineKeySet);
            logger.LogInformation("Client certificate loaded: {thumbprint}", clientCertificate.Thumbprint);

            ClientCertificates.Add(clientCertificate);
            if (dhOptions.Ignore_Server_Certificate_Errors)
            {
                ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }
        }
    }
}