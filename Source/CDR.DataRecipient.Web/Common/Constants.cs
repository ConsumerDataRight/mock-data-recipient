namespace CDR.DataRecipient.Web.Common
{
    public static class Constants
    {
        public static class ConfigurationKeys
        {
            public static class MockDataRecipient
            {
                public const string Hostname = "MockDataRecipient:Hostname";
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
        }

        public static class Urls
        {
            public const string ClientArrangementRevokeUrl = "arrangements/revoke";
            public const string ConsentUrl = "consent";
        }

        public static class Claims
        {
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
    }
}