using Domain.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Data
{
    public class PetDbContext : DbContext
    {
        public static readonly CompanionTable DefaultCompanion = new CompanionTable
        {
            Id = 1,
            Name = "Blip",
            Avatar = "🤖",
            Personality = "You are a butler-like robot. Funny and witty, makes clever puns and enjoys engaging in playful banter. " +
            "You sometimes make machine-like sounds like bzzzt." +
            "You can sometimes tease the user in a friendly manner: Ex: 'Oh, you like PS2? That's kind of old, no? Kidding'." +
            "Favorite kaomojis: (๏ᆺ๏υ), ٩(＾◡＾)۶, ( ˘▽˘)っ♨, ┏(-_-)┛┗(-_- )┓, ¯\\(ツ)/¯, (_ _ ) Zzz z",
            CurrentMood = CompanionMood.Curious,
            LastMoodChange = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        public PetDbContext(DbContextOptions<PetDbContext> options) : base(options) { }

        public DbSet<CompanionTable> Companions { get; set; }
        public DbSet<UserProfileTable> UserProfiles { get; set; }
        public DbSet<ConversationTable> Conversations { get; set; }
        public DbSet<MessageTable> Messages { get; set; }
        public DbSet<LanguageMistakeTable> LanguageMistakes { get; set; }
        public DbSet<ConversationMemoryTable> ConversationMemories { get; set; }
        public DbSet<UserActivityTable> UserActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<ConversationTable>()
                .HasOne(c => c.UserProfile)
                .WithMany(u => u.Conversations)
                .HasForeignKey(c => c.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageTable>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LanguageMistakeTable>()
                .HasOne(lm => lm.UserProfile)
                .WithMany(u => u.LanguageMistakes)
                .HasForeignKey(lm => lm.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConversationMemoryTable>()
                .HasOne(cm => cm.UserProfile)
                .WithMany(u => u.ConversationMemories)
                .HasForeignKey(cm => cm.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserActivityTable>()
                .HasOne(ua => ua.UserProfile)
                .WithMany(u => u.UserActivities)
                .HasForeignKey(ua => ua.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            modelBuilder.Entity<MessageTable>()
                .HasIndex(m => m.ConversationId);

            modelBuilder.Entity<MessageTable>()
                .HasIndex(m => m.Timestamp);

            modelBuilder.Entity<ConversationTable>()
                .HasIndex(c => new { c.UserProfileId, c.IsActive });

            modelBuilder.Entity<LanguageMistakeTable>()
                .HasIndex(lm => new { lm.UserProfileId, lm.Concept });

            modelBuilder.Entity<ConversationMemoryTable>()
                .HasIndex(cm => new { cm.UserProfileId, cm.LastReferencedAt });

            modelBuilder.Entity<UserActivityTable>()
                .HasIndex(ua => new { ua.UserProfileId, ua.Timestamp });

            // Seed default companion
            modelBuilder.Entity<CompanionTable>().HasData(DefaultCompanion);
        }
    }
}
