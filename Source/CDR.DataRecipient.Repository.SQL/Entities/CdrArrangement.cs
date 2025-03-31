using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class CdrArrangement
    {
        [Key]
        [MaxLength(100)]
        public string CdrArrangementId { get; set; }

        [MaxLength(100)]
        public string ClientId { get; set; }

        public string UserId { get; set; }

        public string JsonDocument { get; set; }
    }
}
