using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class AnkiAppContext : DbContext
    {
        public AnkiAppContext(DbContextOptions<AnkiAppContext> options) : base(options) { }
        public DbSet<Card> Cards { get; set; }
        public DbSet<UserCardState> UserCardStates { get; set; }
        public DbSet<Sentence> Sentences { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Meaning> Meanings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Card>()
                    .HasOne(c => c.NativeSentence)
                    .WithMany() // sentence doesn’t need to know about cards
                    .HasForeignKey(c => c.NativeSentenceId)
                    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Card>()
                        .HasOne(c => c.TargetSentence)
                        .WithMany()
                        .HasForeignKey(c => c.TargetSentenceId)
                        .OnDelete(DeleteBehavior.Restrict);

            // Tag primary key is Name
            modelBuilder.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Name);
                e.Property(t => t.Name).HasMaxLength(128).IsRequired();
                e.Property(t => t.Type).HasMaxLength(64).IsRequired();
                e.HasIndex(t => t.Name).IsUnique(); // optional but nice
            });

            // Many-to-many Card <-> Tag with explicit join table using Tag.Name as principal key
            modelBuilder.Entity<Meaning>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Meanings)
                .UsingEntity<Dictionary<string, object>>(
                    "MeaningTag",
                    r => r.HasOne<Tag>()
                          .WithMany()
                          .HasForeignKey("TagName")
                          .HasPrincipalKey(nameof(Tag.Name))
                          .OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne<Meaning>()
                          .WithMany()
                          .HasForeignKey("MeaningId")
                          .HasPrincipalKey(nameof(Meaning.Id))
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MeaningId", "TagName");
                        j.ToTable("MeaningTags");
                    });
        }

    }

    public class AnkiAppDbContextFactory : IDesignTimeDbContextFactory<AnkiAppContext>
    {
        public AnkiAppContext CreateDbContext(string[] args)
        {

            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            Console.WriteLine($"Environment detected: {environment}");

            string connectionString = ParseConnectionStringFromCommandLine(args);
            var builder = new DbContextOptionsBuilder<AnkiAppContext>();
            builder.UseSqlite("Data Source = localdb.db");

            return new AnkiAppContext(builder.Options);
        }
        private string ParseConnectionStringFromCommandLine(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--connection="))
                {
                    return arg.Substring("--connection=".Length);
                }
            }
            Console.WriteLine(JsonSerializer.Serialize(args));

            throw new ArgumentException("Connection string not found in command-line arguments. Expected format: dotnet ef database update -- --connection=<CONNECTION_STRING>");
        }
    }
}
