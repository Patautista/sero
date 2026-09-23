using Domain.Shared.Models;
using MauiApp2.Features.MentalModels;
using System;
using System.Collections.Generic;

namespace MauiApp2.Features.Chat
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public SenderType SenderType { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<CorrectionData>? Corrections { get; set; }
        public MessageType MessageType { get; set; }

        // UI-only state (not persisted)
        public string? Translation { get; set; }
        public bool ShowTranslation { get; set; }
        public bool IsTranslating { get; set; }
    }

    public class CorrectionCheckResult
    {
        public List<CorrectionData>? Corrections { get; set; }
        public List<string> NewlyMasteredConcepts { get; set; } = new();
    }

    public class ConversationState
    {
        public int ConversationId { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class CompanionResponseContext
    {
        public string UserName { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string NativeLanguage { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();
        public List<ConversationMemory> RecentMemories { get; set; } = new();
        public CompanionMood CurrentMood { get; set; }
        public string Personality { get; set; } = string.Empty;
        public string CompanionName { get; set; } = string.Empty;
        public List<ChatMessage> ConversationHistory { get; set; } = new();
        public List<LearningActivity> EligibleActivities { get; set; } = new();
        public SkillProfile UserSkillProfile { get; set; } = new();
        public AreaProgress UserAreaProgress { get; set; } = new();
        public SkillAreaCatalog? SkillAreaCatalog { get; set; }

        /// <summary>
        /// Tracks whether the user has completed a learning activity in the last 15 minutes.
        /// If true, activity suggestions should be withheld from the prompt.
        /// </summary>
        public bool HasRecentActivity { get; set; }

        /// <summary>
        /// The composed output of the mental models and the conversation strategist for
        /// this turn (per-model insights plus the next objective). Populated by the
        /// Conversation Engine and rendered into the prompt; may be null if reasoning
        /// could not be produced, in which case the prompt builder falls back to its
        /// legacy inline sections.
        /// </summary>
        public MentalModelReasoning? Reasoning { get; set; }
    }
}
