using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MauiApp2.Services.AI.Schemas
{
    public enum MemoryFactType
    {
        Interest,
        Event,
        Preference,
        Goal
    }

    public class MemoryExtractionSchema
    {
        [JsonPropertyName("memories")]
        public List<MemoryItemSchema> Memories { get; set; } = new();
    }

    public class MemoryItemSchema
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("importance")]
        public int Importance { get; set; }

        [JsonPropertyName("type")]
        public MemoryFactType Type { get; set; }
    }
}
