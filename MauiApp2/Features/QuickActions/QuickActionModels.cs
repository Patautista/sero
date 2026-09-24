using System.Collections.Generic;

namespace MauiApp2.Features.QuickActions
{
    public class QuickActionRequest
    {
        public string Text { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
    }

    public class PronunciationResult
    {
        public string Text { get; set; } = string.Empty;
        public byte[] AudioData { get; set; } = System.Array.Empty<byte>();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class TranslationResult
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public byte[] AudioData { get; set; } = System.Array.Empty<byte>();
        public List<TextChunk> Chunks { get; set; } = new();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class MeaningResult
    {
        public string Word { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class TextAnalysisResult
    {
        public string OriginalText { get; set; } = string.Empty;
        public List<TextChunk> Chunks { get; set; } = new();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class TextChunk
    {
        public string Word { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class QuickActionHistoryItem
    {
        public string OriginalText { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime UsedAt { get; set; }
    }
}
