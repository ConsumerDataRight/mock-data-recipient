namespace CDR.DataRecipient.Web.Extensions
{
    public static class KeyExtensions
    {
        /// <summary>
        /// Apply formatting to the provided private key.
        /// </summary>
        /// <param name="privateKey">Raw private key.</param>
        /// <returns>string.</returns>
        public static string FormatPrivateKey(this string privateKey)
        {
            return privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", string.Empty)
                .Replace("-----END PRIVATE KEY-----", string.Empty)
                .Replace("\r\n", string.Empty)
                .Trim();
        }
    }
}
