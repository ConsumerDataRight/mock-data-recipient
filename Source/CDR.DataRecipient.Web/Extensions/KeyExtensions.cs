namespace CDR.DataRecipient.Web.Extensions
{
    public static class KeyExtensions
    {
        /// <summary>
        /// Apply formatting to the provided private key.
        /// </summary>
        /// <param name="privateKey">Raw private key</param>
        /// <returns></returns>
        public static string FormatPrivateKey(this string privateKey)
        {
            return privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\r\n", "")
                .Trim();
        }
    }
}
