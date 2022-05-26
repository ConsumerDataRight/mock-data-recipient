using System.IO;
using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class JsonExtensions
    {
        public static string ToPrettyJson(this string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
    }
}
