namespace CDR.DataRecipient.SDK.Models
{
    public class DataRecipientViewModel
    {
        public string SoftwareProductId { get; set; }
        public string SoftwareProductName { get; set; }
    }

    public class SoftwareProductViewModel
    {
        public string BrandId { get; set; }
        public string RecipientBaseUri { get; set; }
        public string RedirectUri { get; set; }
        public string JwksUri { get; set; }
        public string Scope { get; set; }
    }
}