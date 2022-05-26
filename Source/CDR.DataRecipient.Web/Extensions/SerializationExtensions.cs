using System.Text;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class SerializationExtensions
    {
        public static byte[] ToByteArray(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T FromByteArray<T>(this byte[] byteArray) where T : class
        {
            if (byteArray == null)
            {
                return default(T);
            }

            var json = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

    }
}
