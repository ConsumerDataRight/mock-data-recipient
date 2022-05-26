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
    }
}
