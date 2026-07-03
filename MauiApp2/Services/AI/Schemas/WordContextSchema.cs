using System.Text.Json.Serialization;

namespace MauiApp2.Services.AI.Schemas
{
    public class WordContextSchema
    {
        [JsonPropertyName("context")]
        public string Context { get; set; } = string.Empty;

        [JsonPropertyName("example")]
        public string Example { get; set; } = string.Empty;
    }
}
