using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Common
{
    public class OidcSettingsProvider : IOidcSettingsProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OidcSettingsProvider> _logger;

        public OidcSettingsProvider(IConfiguration configuration, ILogger<OidcSettingsProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GetSecret()
        {
            var secretVolume = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.SecretVolumePath);
            var secret = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ClientSecret);
            var mountedSecretName = _configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.MountedSecretName);

            // if volume mount configured the mounted secret is used instead of one configured in the appsettings, enviroment variables etc.
            if (!string.IsNullOrEmpty(secretVolume))
            {
                _logger.LogInformation("Picking the secret from the volume - {SecretVolume},{MountedSecretName}", secretVolume, mountedSecretName);
                secret = _configuration[mountedSecretName];
            }

            return secret;
        }
    }
}
