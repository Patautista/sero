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
    public class MobileDbContext : DbContext
    {
        public MobileDbContext(DbContextOptions<MobileDbContext> options) : base(options) { }
        public DbSet<CardTable> Cards { get; set; }
        public DbSet<CurriculumTable> Curricula { get; set; }
        public DbSet<CurriculumSectionTable> CurriculumSections { get; set; }
        public DbSet<UserTable> Users { get; set; }
        public DbSet<ReviewSessionTable> ReviewSessions { get; set; }
        public DbSet<SrsCardStateTable> UserCardStates { get; set; }
        public DbSet<EventTable> Events { get; set; }
        public DbSet<SentenceTable> Sentences { get; set; }
        public DbSet<TagTable> Tags { get; set; }
        public DbSet<MeaningTable> Meanings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                Console.WriteLine($"Entity: {entityType.Name}");
            }

            modelBuilder.Ignore<Domain.Entity.Card>();
            modelBuilder.Ignore<Domain.Entity.CurriculumSection>();
            modelBuilder.Ignore<Domain.Entity.Sentence>();
            modelBuilder.Ignore<Domain.Entity.Tag>();

            // Tag primary key is Name
            modelBuilder.Entity<TagTable>(e =>
            {
                e.HasKey(t => t.Name);
                e.Property(t => t.Name).HasMaxLength(128).IsRequired();
                e.Property(t => t.Type).HasMaxLength(64).IsRequired();
                e.HasIndex(t => t.Name).IsUnique(); // optional but nice
            });

            modelBuilder.Entity<EventTable>()
            .HasOne(e => e.Card)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.CardId);

            // Many-to-many Card <-> Tag with explicit join table using Tag.Name as principal key
            modelBuilder.Entity<MeaningTable>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Meanings)
                .UsingEntity<Dictionary<string, object>>(
                    "MeaningTag",
                    r => r.HasOne<TagTable>()
                          .WithMany()
                          .HasForeignKey("TagName")
                          .HasPrincipalKey(nameof(TagTable.Name))
                          .OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne<MeaningTable>()
                          .WithMany()
                          .HasForeignKey("MeaningId")
                          .HasPrincipalKey(nameof(MeaningTable.Id))
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MeaningId", "TagName");
                        j.ToTable("MeaningTags");
                    });
        }

    }

    public class AnkiAppDbContextFactory : IDesignTimeDbContextFactory<MobileDbContext>
    {
        public MobileDbContext CreateDbContext(string[] args)
        {

            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            Console.WriteLine($"Environment detected: {environment}");

            var builder = new DbContextOptionsBuilder<MobileDbContext>();
            builder.UseSqlite("Data Source = localdb.db");

            return new MobileDbContext(builder.Options);
        }
    }
}
