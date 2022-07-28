using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class Registration
    {
        [Key]
        public Guid ClientId { get; set; }

        public Guid DataHolderBrandId { get; set; }

        public string JsonDocument { get; set; }
    }
}