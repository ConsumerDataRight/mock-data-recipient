namespace CDR.DataRecipient.Web.Common
{
    public static class Constants
    {
        public static class ConfigurationKeys
        {
            public static class MockDataRecipient
            {
                public const string Hostname = "MockDataRecipient:Hostname";
                public const string DefaultPageSize = "MockDataRecipient:Paging:DefaultPageSize";
                public const string AttemptValidateCdrArrangementJwtFromDate = "MockDataRecipient:Arrangement:AttemptValidateJwtFromDate";
                

                public static class SoftwareProduct
                {
                    public const string Root = "MockDataRecipient:SoftwareProduct";
                    public const string SoftwareProductId = "MockDataRecipient:SoftwareProduct:softwareProductId";
                    public const string BrandId = "MockDataRecipient:SoftwareProduct:brandId";
                    public const string JwksUri = "MockDataRecipient:SoftwareProduct:jwksUri";
                    public const string RedirectUris = "MockDataRecipient:SoftwareProduct:redirectUris";
                }

                public static class DefaultDataHolder
                {
                    public const string Root = "MockDataRecipient:DataHolder";
                    public const string RegistrationEndpoint = "MockDataRecipient:DataHolder:registrationEndpoint";
                    public const string OidcDiscoveryUri = "MockDataRecipient:DataHolder:oidcDiscoveryUri";
                    public const string JwksUri = "MockDataRecipient:DataHolder:jwksUri";
                }
            }

            public static class OidcAuthentication
            {
                public const string Issuer = "oidc:issuer";
                public const string ClientId = "oidc:client_id";
                public const string ClientSecret = "oidc:client_secret";
                public const string CallbackPath = "oidc:callback_path";
                public const string ResponseType = "oidc:response_type";
                public const string ResponseMode = "oidc:response_mode";
                public const string Scope = "oidc:scope";
            }

            public static class Register
            {
                public const string Root = "MockDataRecipient:Register";
                public const string TlsBareUri = "MockDataRecipient:Register:tlsBaseUri";
                public const string MtlsBareUri = "MockDataRecipient:Register:mtlsBaseUri";
                public const string OidcDiscoveryUri = "MockDataRecipient:Register:oidcDiscoveryUri";
            }

            public const string AllowSpecificOrigins = "AllowSpecificOrigins";
            public const string AllowSpecificHeaders = "AllowSpecificHeaders";
            public const string AcceptAnyServerCertificate = "AcceptAnyServerCertificate";
            public const string EnforceHttpsEndpoints = "EnforceHttpsEndpoints";
            public const string ContentSecurityPolicy = "ContentSecurityPolicy";
        }

        public static class Urls
        {
            public const string ClientArrangementRevokeUrl = "arrangements/revoke";
            public const string ConsentUrl = "consent";
        }

        public static class Content
        {
            public const string ApplicationName = "ApplicationName";
            public const string HomepageContentUrl = "HomepageOverrideContentUrl";
            public const string FooterContentUrl = "FooterOverrideContentUrl";
        }

        public static class Claims
        {
            public const string ClientId = "client_id";
            public const string UserId = "userId";
            public const string Name = "name";
        }

        public static class LocalAuthentication
        {
            public const string AuthenticationType = "local";
            public const string UserId = "mdr-user";
            public const string GivenName = "MDR";
            public const string Surname = "User";
        }

        public static class ErrorCodes
        {
            public const string MissingField = "urn:au-cds:error:cds-all:Field/Missing";
            public const string InvalidField = "urn:au-cds:error:cds-all:Field/Invalid";
            public const string InvalidHeader = "urn:au-cds:error:cds-all:Header/Invalid";
            public const string InvalidConsent = "urn:au-cds:error:cds-all:Authorisation/InvalidArrangement";
        }

        public static class ErrorTitles
        {
            public const string MissingField = "Missing Required Field";
            public const string InvalidField = "Invalid Field";
            public const string InvalidArrangement = "Invalid Consent Arrangement";
            public const string InvalidHeader = "Invalid Header";
        }

        public static class CdrArrangementRevocationRequest
        {
            public const string CdrArrangementId = "cdr_arrangement_id";
            public const string CdrArrangementJwt = "cdr_arrangement_jwt";
        }

        public static class Defaults
        {
            public const string DefaultUserName = "unknown";
        }
        public const string DEFAULT_KEY_ID = "7EFA85C18FDE857949BC2EAA21C25E49627D4865";

        public const string DEFAULT_PRIVATE_KEY =
            @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDCx9oZn9tVOAXy
2XyjLv2JWYtn+cJ0CLPlym3A/r2pFAH4rQnoM2IAHcM5n6uWA9ke1bBUZM2+9UTj
y5jrNa+CGPYzsWnGOOpUcKgv3yaHlksBOi5xBPcx9UnraVpLiLLqcucCH1F1Soit
1Pg+OLwFqMW9rVNZYFFikc/KQO6/ZkKVGCVJyua4iVU5fANY0Je8gYTiobARfabd
QZgU8S/d2QGKcnFyxuYR81xaeOd6jJZ6ZzD3Hnl37cPHCksp+NQS7S7EKFhMNWT0
iLe7NmWz2odUHA62tPpz0Aoma0L+yWoy1qrG+AERMjy0fwmXf7mM2hCDT3jEt3Nd
+0dzTp2zAgMBAAECggEACx5XX9EVNxccl9E8YSBEjruSzpueMvtwMXTNsQ+ZifY/
ao+OGjgcpv8L7tUjeUu88Bqolxit+fGMPiiYEQ0eeKGuJCNDc3I6RhmsMBdf3quA
mpBUqFTtO2fSEWMRKXCjLejjMObSwow/oxSeGwcoDHam2v3y3Q43dxX1s4jjV/+H
uwgwghOqd9gTWeu7tOQefYkJ4Tsj6UMrf42LbSazjQmz+4sABmYv1TiuGW9Uj+vB
iV7Jozc9rVx4ZOhKrGxnM8kMsG6RWCfP5Nm3PUM/9tCzqNcPJCN3FyEcj2tfU/0I
CBuUqXaX4V/usg+TOf2tO2n/5TIZQozUatOAhWeJcQKBgQD3UJ9F6kIpkcO9qZLp
sLN0eVpt3hk2A//XV+QrKg8sYFxlYuX93TnF3q+V56h+UqXgCLnj0Q7MeAWLap2e
ApbumYRz8/qpthUqi0wyR8e8JEMDi3L5cOXNRYcVY2F3XKmxFXjfnNQ6gkms0Gfe
3Pni+q/xceQ7nhc0h/HAdNm3SwKBgQDJnvF6sW8bwamFoi0CWPOBcmb52QJ6tZ7h
Duh6fNZVE+FoRFv2q6PUNsm9rnTZtR7uevYcfnt2NrXNpsMQmnpaEU73nYWhiPn4
zxaavAEFQPzUFaocYEAj5uXSKv1KNK2lvT9bjQFDT1lU1eginXJAbWa63zDnkV89
AxH9LjeqOQKBgDtg8QzBROdkJwIHj81p7nw9krekRptQdIHIiXDPpVr7O9Pf3eaI
0hEu+Stdtne18juK/M605/+xpWsmyvcgGgrpcwLABmPu4sAXN9EuqMcEUc6tEYrQ
T2xskBVTihg1eEybIi1WIyJ1G6lRVE8O8TRNCidHOAwUVe/339RcedVnAoGAGcr7
mXaZgDOGPFJC78nxXN4FznC0oH4blS8TDphp0vh4HZ6hJS1QCBX6OQnYaQGCs3+H
fJ2xra3SFD0BN16LyHnuYD8GmWOslufnPGRQvRtTPM6ItJibm/wt6nUVcijLDijn
sg6X2sSL6Q50Y/lAZH2aZs2ms/kk9ekuo/UFqgECgYEAlGWBKSsYH4OijwOSA54W
tHeKOMkv5DySAPQUhMPSE13y56XaoHdtl+wGElYQ2/lt1h8dhj0L04QZjhqkAJn3
79xiYVOkKJvDjHCnjLIHkrDOKWBVApX31sD2uKl4BXRzLO+9iV+W67T89z71Ftdx
dQMjPQnK/1R3jqD9OWNr818=
-----END PRIVATE KEY-----
";
    }
}