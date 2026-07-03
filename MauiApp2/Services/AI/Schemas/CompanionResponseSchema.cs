using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MauiApp2.Services.AI.Schemas
{
    public class CompanionResponseSchema
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("corrections")]
        public List<CompanionCorrectionItem> Corrections { get; set; } = new();
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
