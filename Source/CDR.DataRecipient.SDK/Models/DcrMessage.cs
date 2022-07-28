using System;

namespace CDR.DataRecipient.SDK.Models
{
    public class DcrMessage
    {
        public int DcrMessageId { get; set; }
        public string ClientId { get; set; }
        public Guid DataHolderBrandId { get; set; }
        public string BrandName { get; set; }
        public string InfosecBaseUri { get; set; }
        public string MessageId { get; set; }
        public string MessageState { get; set; }
        public string MessageError { get; set; }
        public DateTime LastUpdatedByMessageDate { get; set; }
    }
}