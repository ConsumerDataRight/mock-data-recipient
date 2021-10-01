namespace CDR.DataRecipient.Web.Configuration.Models
{
    public class Register
    {
        public string TlsBaseUri { get; set; }
        public string MtlsBaseUri { get; set; }
        public string OidcDiscoveryUri { get; set; }
        public string TokenEndpoint { get; set; }
        public string GetSsaEndpoint { get; set; }
        public string GetDataHolderBrandsEndpoint { get; set; }
    }
}
