using Domain.Shared.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data
{
    [Table("Companions")]
    public class CompanionTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Avatar { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Personality { get; set; } = string.Empty;

        [Required]
        public CompanionMood CurrentMood { get; set; }

        [Required]
        public DateTime LastMoodChange { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }

    [Table("UserProfiles")]
    public class UserProfileTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string TargetLanguage { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string NativeLanguage { get; set; } = string.Empty;

        [Column(TypeName = "TEXT")]
        public string InterestsJson { get; set; } = "[]";

        [Column(TypeName = "TEXT")]
        public string ActivityPatternsJson { get; set; } = "{}";

        [Column(TypeName = "TEXT")]
        public string SkillsJson { get; set; } = "{}";

        [Required]
        public DateTime OnboardedAt { get; set; }

        [Required]
        public DateTime LastActiveAt { get; set; }

        // Navigation properties
        public ICollection<ConversationTable> Conversations { get; set; } = new List<ConversationTable>();
        public ICollection<LanguageMistakeTable> LanguageMistakes { get; set; } = new List<LanguageMistakeTable>();
        public ICollection<ConversationMemoryTable> ConversationMemories { get; set; } = new List<ConversationMemoryTable>();
        public ICollection<UserActivityTable> UserActivities { get; set; } = new List<UserActivityTable>();
    }

    [Table("Conversations")]
    public class ConversationTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserProfileId { get; set; }

        [Required]
        public DateTime StartedAt { get; set; }

        [Required]
        public DateTime LastMessageAt { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserProfileId))]
        public UserProfileTable? UserProfile { get; set; }

        public ICollection<MessageTable> Messages { get; set; } = new List<MessageTable>();
    }

    [Table("Messages")]
    public class MessageTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public SenderType SenderType { get; set; }

        [Required]
        [Column(TypeName = "TEXT")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public MessageType MessageType { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;

        [Column(TypeName = "TEXT")]
        public string? CorrectionDataJson { get; set; }

        // Navigation property
        [ForeignKey(nameof(ConversationId))]
        public ConversationTable? Conversation { get; set; }
    }

    [Table("LanguageMistakes")]
    public class LanguageMistakeTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserProfileId { get; set; }

        [Required]
        [MaxLength(500)]
        public string OriginalText { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string CorrectedText { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MistakeType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Concept { get; set; } = string.Empty;

        [Required]
        public int OccurrenceCount { get; set; }

        [Required]
        public DateTime FirstSeenAt { get; set; }

        [Required]
        public DateTime LastSeenAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserProfileId))]
        public UserProfileTable? UserProfile { get; set; }
    }

    [Table("ConversationMemories")]
    public class ConversationMemoryTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserProfileId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FactType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "TEXT")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime LastReferencedAt { get; set; }

        [Required]
        [Range(1, 5)]
        public int Importance { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserProfileId))]
        public UserProfileTable? UserProfile { get; set; }
    }

    [Table("UserActivities")]
    public class UserActivityTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserProfileId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserProfileId))]
        public UserProfileTable? UserProfile { get; set; }
    }
}
