using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Register;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class HttpExtensions
    {

        public static void SetClientCertificate(this HttpClientHandler clientHandler, string certificateFileName, string certificatePassword)
        {
            clientHandler.ClientCertificates.Add(new X509Certificate2(certificateFileName, certificatePassword, X509KeyStorageFlags.Exportable));
        }

        public static bool IsSuccessful(this HttpStatusCode statusCode)
        {
            return ((int)statusCode) < 400;
        }

        public static int ToInt(this HttpStatusCode statusCode)
        {
            return ((int)statusCode);
        }

        public static async Task<HttpResponseMessage> SendPrivateKeyJwtRequest(
            this HttpClient client, 
            string url, 
            string clientId, 
            X509Certificate2 signingCertificate,
            string scope,
            string redirectUri = null,
            string code = null, 
            string grantType = Constants.GrantTypes.CLIENT_CREDENTIALS,
            IDictionary<string, string> additionalFormFields = null)
        {
            var privateKeyJwt = new PrivateKeyJwt(signingCertificate);

            var formFields = new List<KeyValuePair<string, string>>();
            formFields.Add(new KeyValuePair<string, string>("grant_type", grantType));
            formFields.Add(new KeyValuePair<string, string>("client_id", clientId));
            formFields.Add(new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"));
            formFields.Add(new KeyValuePair<string, string>("client_assertion", privateKeyJwt.Generate(clientId, url)));

            if (!string.IsNullOrEmpty(scope))
            {
                formFields.Add(new KeyValuePair<string, string>("scope", scope));
            }

            if (!string.IsNullOrEmpty(redirectUri))
            {
                formFields.Add(new KeyValuePair<string, string>("redirect_uri", redirectUri));
            }

            if (!string.IsNullOrEmpty(code))
            {
                formFields.Add(new KeyValuePair<string, string>("code", code));
            }

            if (additionalFormFields != null)
            {
                foreach (var field in additionalFormFields)
                {
                    formFields.Add(new KeyValuePair<string, string>(field.Key, field.Value));
                }
            }

            var clientAssertionContent = new FormUrlEncodedContent(formFields);

            return await client.PostAsync(url, clientAssertionContent);
        }
    }
}
