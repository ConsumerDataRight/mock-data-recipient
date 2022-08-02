using CDR.DataRecipient.Web.Common;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class ConfigExtensions
    {
        public static bool IsOidcConfigured(this IConfiguration config)
        {
            return !string.IsNullOrEmpty(config.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.Issuer));
        }

        /// <summary>
        /// Turns off server certificate validation when connecting to remote endpoints.
        /// </summary>
        /// <param name="config">IConfiguration</param>
        /// <returns>Value from "AcceptAnyServerCertificate" configuration setting.  Defaults to false.</returns>
        public static bool IsAcceptingAnyServerCertificate(this IConfiguration config)
        {
            return config.GetValue<bool>(Constants.ConfigurationKeys.AcceptAnyServerCertificate, false);
        }

        /// <summary>
        /// When set to true will only connect to remote endpoints using https.
        /// </summary>
        /// <param name="config">IConfiguration</param>
        /// <returns>Value from "EnforceHttpsEndpoints" configuration setting.  Defaults to true.</returns>
        public static bool IsEnforcingHttpsEndpoints(this IConfiguration config)
        {
            return config.GetValue<bool>(Constants.ConfigurationKeys.EnforceHttpsEndpoints, true);
        }
    }
}
