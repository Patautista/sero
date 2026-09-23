using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Domain.Shared.Models
{
    // Enums
    public enum CompanionMood
    {
        Tired,
        Curious,
        Nostalgic,
        Excited
    }

    public enum SenderType
    {
        User,
        Companion
    }

    public enum MessageType
    {
        Normal,
        Correction,
        ProactivePrompt,
        SkillUpdate,
        ActivityPrompt,
        UserInsight,
        CompanionInsight,
        /// <summary>
        /// An audio-only companion message (e.g. the spoken line of a listening
        /// activity). The message's Content holds the transcript used to generate/
        /// cache the speech, but the UI must render it as a playable audio bubble
        /// instead of showing the text.
        /// </summary>
        AudioMessage
    }

    public enum MistakeChallengeStatus
    {
        Active,
        Resolved
    }

    // Domain Models
    public class Companion
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

    public class UserProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string NativeLanguage { get; set; } = string.Empty;
        public string InterestsJson { get; set; } = "[]";
        public string ActivityPatternsJson { get; set; } = "{}";
        public string SkillsJson { get; set; } = "{}";
        public string AreaProgressJson { get; set; } = "{}";
        public DateTime OnboardedAt { get; set; }
        public DateTime LastActiveAt { get; set; }

        // Computed properties
        public List<string> Interests
        {
            get => string.IsNullOrWhiteSpace(InterestsJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(InterestsJson) ?? new List<string>();
            set => InterestsJson = JsonSerializer.Serialize(value);
        }

        /// <summary>
        /// Independent skill scores (Reading, Writing, Listening; 0-100) that replace
        /// a single static language level. Persisted as a name-keyed JSON dictionary so
        /// new skills can be added without a schema change.
        /// </summary>
        public SkillProfile Skills
        {
            get => SkillProfile.FromJson(SkillsJson);
            set => SkillsJson = value?.ToJson() ?? "{}";
        }

        /// <summary>
        /// Progress through concept/topic skill areas, stored independently from the
        /// broad Reading/Writing/Listening profile.
        /// </summary>
        public AreaProgress AreaProgress
        {
            get => AreaProgress.FromJson(AreaProgressJson);
            set => AreaProgressJson = value?.ToJson() ?? "{}";
        }

        public Dictionary<int, List<DateTime>> ActivityPatterns
        {
            get => string.IsNullOrWhiteSpace(ActivityPatternsJson) 
                ? new Dictionary<int, List<DateTime>>() 
                : JsonSerializer.Deserialize<Dictionary<int, List<DateTime>>>(ActivityPatternsJson) ?? new Dictionary<int, List<DateTime>>();
            set => ActivityPatternsJson = JsonSerializer.Serialize(value);
        }
    }

    public class Conversation
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation property
        public UserProfile? UserProfile { get; set; }
        public List<Message> Messages { get; set; } = new();
    }

    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public SenderType SenderType { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string? CorrectionDataJson { get; set; }

        // Navigation property
        public Conversation? Conversation { get; set; }

        // Computed property
        public List<CorrectionData>? Corrections
        {
            get => string.IsNullOrWhiteSpace(CorrectionDataJson) 
                ? null 
                : JsonSerializer.Deserialize<List<CorrectionData>>(CorrectionDataJson);
            set => CorrectionDataJson = value == null ? null : JsonSerializer.Serialize(value);
        }
    }

    public class CorrectionData
    {
        public string Original { get; set; } = string.Empty;
        public string Corrected { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    public class LanguageMistake
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string CorrectedText { get; set; } = string.Empty;
        public string MistakeType { get; set; } = string.Empty; // Grammar, Vocabulary, Spelling
        public string Concept { get; set; } = string.Empty; // e.g., "verb conjugation", "article usage"
        public int OccurrenceCount { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public MistakeChallengeStatus Status { get; set; } = MistakeChallengeStatus.Active;
        public int ConsecutiveCorrectCount { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation property
        public UserProfile? UserProfile { get; set; }
    }

    public class PracticeChallenge
    {
        public const int MasteryThreshold = 3;

        public string Concept { get; set; } = string.Empty;
        public string MistakeType { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string CorrectedText { get; set; } = string.Empty;
        public int OccurrenceCount { get; set; }
        public int ConsecutiveCorrectCount { get; set; }
    }

    public class ConversationMemory
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string FactType { get; set; } = string.Empty; // Interest, Event, Preference, Goal
        public string Content { get; set; } = string.Empty;
        public DateTime LastReferencedAt { get; set; }
        public int Importance { get; set; } // 1-5
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public UserProfile? UserProfile { get; set; }
    }

    public class UserActivity
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string ActivityType { get; set; } = string.Empty; // MessageSent, AppOpened
        public DateTime Timestamp { get; set; }

        // Navigation property
        public UserProfile? UserProfile { get; set; }
    }
}
