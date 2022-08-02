using CDR.DataRecipient.SDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class UrlExtensions
    {
        public static string ConcatUrl(this string uri, string path)
        {
            return string.Concat(uri.TrimEnd('/'), "/", path.TrimStart('/'));
        }

        public static string AppendQueryString(this string uri, string name, string value)
        {
            if (!uri.Contains('?'))
            {
                uri += $"?{name}={value}";
            }
            else
            {
                uri += $"&{name}={value}";
            }

            return uri;
        }

        public static bool IsHttps(this Uri uri)
        {
            return uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        }

        public static string ValidateEndpoint(this string endpoint, bool enforceHttpsEndpoint)
        {
            if (!enforceHttpsEndpoint)
            {
                return endpoint;
            }

            var uri = new Uri(endpoint);
            uri.ValidateEndpoint(enforceHttpsEndpoint);
                return endpoint;
        }

        public static Uri ValidateEndpoint(this Uri uri, bool enforceHttpsEndpoint)
        {
            if (!enforceHttpsEndpoint)
            {
                return uri;
            }

            if (!uri.IsHttps())
            {
                throw new NoHttpsException();
            }

            return uri;
        }
    }
}
