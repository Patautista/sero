using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MauiApp2.Services.AI.Schemas
{
    public class CompanionResponseSchema
    {
        [JsonPropertyName("blocks")]
        public List<string> Blocks { get; set; } = new();

        [JsonPropertyName("corrections")]
        public List<CompanionCorrectionItem> Corrections { get; set; } = new();

        /// <summary>
        /// Optional signal that the companion wants to start a learning activity. When
        /// set to a known activity id, the conversation engine hands control to the
        /// Activity Agent for that activity. Left empty during normal conversation, so
        /// the companion only decides *when* to start an activity, never *how* to run it.
        /// </summary>
        [JsonPropertyName("startActivityId")]
        public string? StartActivityId { get; set; }
    }

    public class CompanionCorrectionItem
    {
        [JsonPropertyName("corrected")]
        public string Corrected { get; set; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;

        [JsonPropertyName("original")]
        public string Original { get; set; } = string.Empty;
    }
}
