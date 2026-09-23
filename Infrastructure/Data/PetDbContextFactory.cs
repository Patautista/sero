using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data
{
    /// <summary>
    /// Design-time factory for EF Core migrations.
    /// This is only used by dotnet ef commands, not at runtime.
    /// </summary>
    /*
     public class PetDbContextFactory : IDesignTimeDbContextFactory<PetDbContext>
    {
        public PetDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PetDbContext>();

            // Use a temporary SQLite database for migrations
            // The actual connection string is set at runtime in MauiProgram.cs
            optionsBuilder.UseSqlite("Data Source=languagepet_design.db");

            return new PetDbContext(optionsBuilder.Options);
        }
    }
     */
}
