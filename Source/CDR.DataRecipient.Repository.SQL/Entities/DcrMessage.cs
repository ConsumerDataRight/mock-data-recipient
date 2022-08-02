using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class DcrMessage
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string ClientId { get; set; }

        public Guid DataHolderBrandId { get; set; }

        [MaxLength(1000)]
        public string BrandName { get; set; }

        [MaxLength(500)]
        public string InfosecBaseUri { get; set; }

        [MaxLength(50)]
        public string MessageId { get; set; }

        [MaxLength(25)]
        public string MessageState { get; set; }

        public string MessageError { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}