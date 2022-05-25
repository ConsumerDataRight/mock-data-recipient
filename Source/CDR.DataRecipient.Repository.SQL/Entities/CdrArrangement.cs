using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class CdrArrangement
    {
        [Key]
        public Guid CdrArrangementId { get; set; }

        public Guid ClientId { get; set; }

        public string UserId { get; set; }

        public string JsonDocument { get; set; }
    }
}