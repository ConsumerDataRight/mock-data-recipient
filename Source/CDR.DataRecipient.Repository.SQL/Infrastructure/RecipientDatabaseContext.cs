using CDR.DataRecipient.Repository.SQL.Entities;
using Microsoft.EntityFrameworkCore;

namespace CDR.DataRecipient.Infrastructure
{
    public class RecipientDatabaseContext : DbContext
    {
        public RecipientDatabaseContext()
        {

        }

        public RecipientDatabaseContext(DbContextOptions<RecipientDatabaseContext> options) : base(options)
        {

        }

        public DbSet<CdrArrangement> CdrArrangements { get; set; }
        public DbSet<DataHolderBrand> DataHolderBrands { get; set; }
        public DbSet<SoftwareProduct> SoftwareProducts { get; set; }
        public DbSet<Registration> Registrations { get; set; }

        public DbSet<DcrMessage> DcrMessage { get; set; }
        public DbSet<LogEventsDcrService> LogEvents_DCRService { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CdrArrangement>().ToTable("CdrArrangement");
            modelBuilder.Entity<DataHolderBrand>().ToTable("DataHolderBrand");
            modelBuilder.Entity<SoftwareProduct>().ToTable("SoftwareProduct");
            modelBuilder.Entity<Registration>().ToTable("Registration")
                .HasKey(nameof(Registration.ClientId), nameof(Registration.DataHolderBrandId));
        }
    }
}