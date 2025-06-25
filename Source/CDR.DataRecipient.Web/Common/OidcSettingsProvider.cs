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
            this._configuration = configuration;
            this._logger = logger;
        }

        public string GetSecret()
        {
            var secretVolume = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.SecretVolumePath);
            var secret = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.ClientSecret);
            var mountedSecretName = this._configuration.GetValue<string>(Constants.ConfigurationKeys.OidcAuthentication.MountedSecretName);

            // if volume mount configured the mounted secret is used instead of one configured in the appsettings, enviroment variables etc.
            if (!string.IsNullOrEmpty(secretVolume))
            {
                this._logger.LogInformation("Picking the secret from the volume - {SecretVolume},{MountedSecretName}", secretVolume, mountedSecretName);
                secret = this._configuration[mountedSecretName];
            }

            return secret;
        }
    }
}
