using CDR.DataRecipient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace CDR.DataRecipient.Repository.SQL.Infrastructure
{
    public class RecipientDatabaseContextDesignTimeFactory : IDesignTimeDbContextFactory<RecipientDatabaseContext>
    {
        public RecipientDatabaseContextDesignTimeFactory()
        {
            // A parameter-less constructor is required by the EF Core CLI tools.
        }

        public RecipientDatabaseContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<RecipientDatabaseContext>()
                .UseSqlServer("foo") // connection string is only needed if using "dotnet ef database update ..." to actually run migrations from commandline
                .Options;

            return new RecipientDatabaseContext(options);
        }
	}
}