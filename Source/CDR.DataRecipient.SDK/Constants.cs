namespace CDR.DataRecipient.SDK
{
    public static class Constants
    {
        public static class Scopes
        {
            public const string CDR_DYNAMIC_CLIENT_REGISTRATION = "cdr:registration";
            public const string CDR_REGISTER = "cdr-register:read";
            public const string CDR_REGISTER_BANKING = "cdr-register:bank:read";
        }

        public static class GrantTypes
        {
            public const string CLIENT_CREDENTIALS = "client_credentials";
            public const string AUTH_CODE = "authorization_code";
        }

        public static class TokenTypes
        {
            public const string ID_TOKEN = "id_token";
            public const string ACCESS_TOKEN = "access_token";
            public const string REFRESH_TOKEN = "refresh_token";
        }

        public static class Infosec
        {
            public const string CODE_CHALLENGE_METHOD = "S256";
        }

        public static class Urls
        {
            public const string ClientArrangementRevokeUrl = "arrangements/revoke";
        }

        public static class ErrorCodes
        {
            public const string MissingField = "urn:au-cds:error:cds-all:Field/Missing";
            public const string InvalidHeader = "urn:au-cds:error:cds-all:Header/Invalid";
            public const string InvalidArrangement = "urn:au-cds:error:cds-all:Authorisation/InvalidArrangement";
        }

        public static class ErrorTitles
        {
            public const string MissingField = "Missing Required Field";
            public const string InvalidArrangement = "Invalid Consent Arrangement";
            public const string InvalidHeader = "Invalid Header";
        }
    }
}