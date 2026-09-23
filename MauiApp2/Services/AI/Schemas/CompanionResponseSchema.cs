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

        /// <summary>
        /// Optional signal that this turn revealed a genuinely new, durable fact about the
        /// user (an interest, preference or goal not already known). When set, the
        /// conversation engine persists it to memory and shows a "{companion} has learned a
        /// new thing about you!" notice. Left empty when nothing new was learned.
        /// </summary>
        [JsonPropertyName("learnedAboutUser")]
        public string? LearnedAboutUser { get; set; }

        /// <summary>
        /// Optional signal that the companion shared a genuinely new, durable fact about
        /// itself (its likes, dreams, opinions, history) that the user did not know before.
        /// When set, the conversation engine shows a "You've learned a new thing about
        /// {companion}" notice. Left empty when nothing new was shared.
        /// </summary>
        [JsonPropertyName("sharedAboutSelf")]
        public string? SharedAboutSelf { get; set; }
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
