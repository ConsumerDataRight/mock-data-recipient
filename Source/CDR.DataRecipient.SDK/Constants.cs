using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK
{
    public static class Constants
    {
        public static class Scopes
        {
            public const string CDR_DYNAMIC_CLIENT_REGISTRATION = "cdr:registration";
            public const string CDR_REGISTER = "cdr-register:bank:read";
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

    }
}
