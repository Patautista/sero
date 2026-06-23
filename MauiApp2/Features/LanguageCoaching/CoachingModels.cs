using Domain.Shared.Models;
using System.Collections.Generic;

namespace MauiApp2.Features.LanguageCoaching
{
    public class MistakeAnalysisRequest
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int UserProfileId { get; set; }
    }

    public class MistakeAnalysisResult
    {
        public bool HasMistakes { get; set; }
        public List<DetectedMistake> Mistakes { get; set; } = new();
        public string OverallFeedback { get; set; } = string.Empty;
    }

    public class DetectedMistake
    {
        public string OriginalSegment { get; set; } = string.Empty;
        public string CorrectedSegment { get; set; } = string.Empty;
        public string MistakeType { get; set; } = string.Empty; // Grammar, Vocabulary, Spelling
        public string Concept { get; set; } = string.Empty;
        public int Severity { get; set; } // 1-5
    }

    public class RecurringMistake
    {
        public int Id { get; set; }
        public string Concept { get; set; } = string.Empty;
        public string MistakeType { get; set; } = string.Empty;
        public int OccurrenceCount { get; set; }
        public string LatestExample { get; set; } = string.Empty;
        public System.DateTime LastSeenAt { get; set; }
    }

    public class StruggleProfile
    {
        public Dictionary<string, int> ConceptFrequency { get; set; } = new();
        public List<string> TopStruggles { get; set; } = new();
        public int TotalMistakes { get; set; }
        public System.DateTime LastAnalyzed { get; set; }
    }

    public class CorrectionGenerationRequest
    {
        public string OriginalText { get; set; } = string.Empty;
        public List<DetectedMistake> Mistakes { get; set; } = new();
        public string TargetLanguage { get; set; } = string.Empty;
    }

    public class CorrectionGenerationResult
    {
        public string NaturalCorrection { get; set; } = string.Empty;
        public List<CorrectionData> Corrections { get; set; } = new();
    }
}
