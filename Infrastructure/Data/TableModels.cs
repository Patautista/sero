using Domain.Shared.Models;
using LiteDB;
using System;

namespace Infrastructure.Data
{
    public class CompanionTable
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public string Personality { get; set; } = string.Empty;

        public CompanionMood CurrentMood { get; set; }

        public DateTime LastMoodChange { get; set; }

        /// <summary>
        /// Energy level (0-100) that drives tiredness independently of the flavor mood
        /// rotation. Drains as the companion engages in conversation and only replenishes
        /// while idle (i.e. resting) - talking to the companion never restores it.
        /// </summary>
        public int EnergyLevel { get; set; } = 100;

        public DateTime LastEnergyUpdate { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class UserProfileTable
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string TargetLanguage { get; set; } = string.Empty;

        public string NativeLanguage { get; set; } = string.Empty;

        public string InterestsJson { get; set; } = "[]";

        public string ActivityPatternsJson { get; set; } = "{}";

        public string SkillsJson { get; set; } = "{}";

        public DateTime OnboardedAt { get; set; }

        public DateTime LastActiveAt { get; set; }

        [BsonIgnore]
        public ICollection<ConversationTable> Conversations { get; set; } = new List<ConversationTable>();
        [BsonIgnore]
        public ICollection<LanguageMistakeTable> LanguageMistakes { get; set; } = new List<LanguageMistakeTable>();
        [BsonIgnore]
        public ICollection<ConversationMemoryTable> ConversationMemories { get; set; } = new List<ConversationMemoryTable>();
        [BsonIgnore]
        public ICollection<UserActivityTable> UserActivities { get; set; } = new List<UserActivityTable>();
    }

    public class ConversationTable
    {
        public int Id { get; set; }

        public int UserProfileId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime LastMessageAt { get; set; }

        public bool IsActive { get; set; }

        [BsonIgnore]
        public UserProfileTable? UserProfile { get; set; }

        [BsonIgnore]
        public ICollection<MessageTable> Messages { get; set; } = new List<MessageTable>();
    }

    public class MessageTable
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public SenderType SenderType { get; set; }

        public string Content { get; set; } = string.Empty;

        public MessageType MessageType { get; set; }

        public DateTime Timestamp { get; set; }

        public string LanguageCode { get; set; } = string.Empty;

        public string? CorrectionDataJson { get; set; }

        [BsonIgnore]
        public ConversationTable? Conversation { get; set; }
    }

    public class LanguageMistakeTable
    {
        public int Id { get; set; }

        public int UserProfileId { get; set; }

        public string OriginalText { get; set; } = string.Empty;

        public string CorrectedText { get; set; } = string.Empty;

        public string MistakeType { get; set; } = string.Empty;

        public string Concept { get; set; } = string.Empty;

        public int OccurrenceCount { get; set; }

        public DateTime FirstSeenAt { get; set; }

        public DateTime LastSeenAt { get; set; }

        public MistakeChallengeStatus Status { get; set; } = MistakeChallengeStatus.Active;

        public int ConsecutiveCorrectCount { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [BsonIgnore]
        public UserProfileTable? UserProfile { get; set; }
    }

    public class ConversationMemoryTable
    {
        public int Id { get; set; }

        public int UserProfileId { get; set; }

        public string FactType { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime LastReferencedAt { get; set; }

        public int Importance { get; set; }

        public DateTime CreatedAt { get; set; }

        [BsonIgnore]
        public UserProfileTable? UserProfile { get; set; }
    }

    public class UserActivityTable
    {
        public int Id { get; set; }

        public int UserProfileId { get; set; }

        public string ActivityType { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        [BsonIgnore]
        public UserProfileTable? UserProfile { get; set; }
    }
}
