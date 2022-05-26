using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Repository.SQL.Entities
{
    public class SoftwareProduct
    {
        [Key]
        public Guid SoftwareProductId { get; set; }

        public Guid BrandId { get; set; }

        [MaxLength(100)]
        public string SoftwareProductName { get; set; }

        [MaxLength(1000)]
        public string SoftwareProductDescription { get; set; }

        [MaxLength(1000)]
        public string LogoUri { get; set; }

        [MaxLength(1000)]
        public string RecipientBaseUri { get; set; }

        [MaxLength(2000)]
        public string RedirectUri { get; set; }

        [MaxLength(1000)]
        public string JwksUri { get; set; }

        [MaxLength(1000)]
        public string Scope { get; set; }

        [MaxLength(25)]
        public string Status { get; set; }
    }
}