using System;
using System.Collections.Generic;

namespace MauiApp2.Features.Memory
{
    public class MemoryExtractionRequest
    {
        public int ConversationId { get; set; }
        public int UserProfileId { get; set; }
        public List<ConversationMessage> Messages { get; set; } = new();
    }

    public class ConversationMessage
    {
        public string Sender { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class MemoryExtractionResult
    {
        public bool Success { get; set; }
        public List<ExtractedMemory> Memories { get; set; } = new();
        public int MemoriesCreated { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ExtractedMemory
    {
        public string FactType { get; set; } = string.Empty; // Interest, Event, Preference, Goal
        public string Content { get; set; } = string.Empty;
        public int Importance { get; set; } // 1-5
    }

    public class MemoryRetrievalRequest
    {
        public int UserProfileId { get; set; }
        public int Limit { get; set; } = 5;
        public int MinImportance { get; set; } = 1;
        public string? FactType { get; set; }
    }

    public class MemoryRetrievalResult
    {
        public List<ConversationMemoryData> Memories { get; set; } = new();
        public int TotalMemories { get; set; }
    }

    public class ConversationMemoryData
    {
        public int Id { get; set; }
        public string FactType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Importance { get; set; }
        public DateTime LastReferencedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DaysSinceLastReference => (DateTime.UtcNow - LastReferencedAt).Days;
    }

    public class MemoryUpdateRequest
    {
        public int MemoryId { get; set; }
        public int? NewImportance { get; set; }
        public bool MarkAsReferenced { get; set; }
    }
}
