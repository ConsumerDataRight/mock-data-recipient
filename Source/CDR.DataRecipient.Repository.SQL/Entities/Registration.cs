using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class Registration
    {
        [Required]
        [MaxLength(100)]
        public string ClientId { get; set; }
        [Required]
        public Guid DataHolderBrandId { get; set; }
        public string JsonDocument { get; set; }
    }
}