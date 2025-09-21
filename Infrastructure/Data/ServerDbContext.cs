using Infrastructure.Data.Model;
using Infrastructure.Data.Model.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;

namespace Infrastructure.Data
{
    public class ServerDbContext : DbContext
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options) { }
        public DbSet<ApiAccess> ApiAccesses { get; set; } = null!;
    }

    public class ServerDbContextFactory : IDesignTimeDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext(string[] args)
        {
            // Determine environment
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            Console.WriteLine($"Environment detected: {environment}");

            // Build configuration
            var basePath = Directory.GetCurrentDirectory();
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();

            if (environment == "Development")
            {
                var sqliteConn = config.GetConnectionString("Sqlite") ?? "Data Source=localdb.db";
                optionsBuilder.UseSqlite(sqliteConn);
            }
            else
            {
                var pgConn = config.GetConnectionString("Postgres") ?? "Host=localhost;Database=prod;Username=postgres;Password=postgres";
                optionsBuilder.UseNpgsql(pgConn);
            }

            return new ServerDbContext(optionsBuilder.Options);
        }
    }
}
