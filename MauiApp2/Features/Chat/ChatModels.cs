using Domain.Shared.Models;
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

    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public ChatMessage? UserMessage { get; set; }
        public ChatMessage? CompanionResponse { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
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
    }
}
