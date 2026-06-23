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
        ProactivePrompt
    }

    // Domain Models
    public class Companion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public CompanionMood CurrentMood { get; set; }
        public DateTime LastMoodChange { get; set; }
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

        // Navigation property
        public UserProfile? UserProfile { get; set; }
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
