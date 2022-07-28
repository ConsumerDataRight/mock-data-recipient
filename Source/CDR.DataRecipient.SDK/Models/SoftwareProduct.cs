namespace CDR.DataRecipient.SDK.Models
{
    public class SoftwareProduct
    {
        public string SoftwareProductId { get; set; }

        public string BrandId { get; set; }

        public string JwksUri { get; set; }

        public string RecipientBaseUri { get; set; }

        public string RedirectUris { get; set; }

        public string RedirectUri
        {
            get
            {
                return (string.IsNullOrEmpty(this.RedirectUris) ? null : this.RedirectUris.Split(',')[0]);
            }
        }

        public string RevocationUri => $"{RecipientBaseUri}/{Constants.Urls.ClientArrangementRevokeUrl}";

        public string Scope { get; set; }

        public string DefaultSigningAlgorithm { get; set; }

        public Certificate ClientCertificate { get; set; }

        public Certificate SigningCertificate { get; set; }
    }
}