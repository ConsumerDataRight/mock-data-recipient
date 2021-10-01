namespace CDR.DataRecipient.IntegrationTests.Models
{
    /// <summary>
    /// Access token
    /// </summary>
    public class AccessToken
    {
#pragma warning disable IDE1006                        
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
#pragma warning restore IDE1006                
    }
}
