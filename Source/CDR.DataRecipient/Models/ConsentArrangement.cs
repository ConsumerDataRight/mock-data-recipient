using System;

namespace CDR.DataRecipient.Models
{
    public class ConsentArrangement
    {
        public string UserId { get; set; }
        public string DataHolderBrandId { get; set; }
        public string BrandName { get; set; }
        public string ClientId { get; set; }
        public int? SharingDuration { get; set; }
        public string CdrArrangementId { get; set; }
        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string Scope { get; set; }
        public string TokenType { get; set; }
        public DateTime CreatedOn { get; set; }
        
    }
}
