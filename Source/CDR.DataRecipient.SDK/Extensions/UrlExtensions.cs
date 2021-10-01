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
    }
}
