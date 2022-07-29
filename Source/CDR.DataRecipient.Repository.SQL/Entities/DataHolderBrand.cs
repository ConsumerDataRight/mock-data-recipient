using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class DataHolderBrand
    {
        [Key]
        public Guid DataHolderBrandId { get; set; }
        public string JsonDocument { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}