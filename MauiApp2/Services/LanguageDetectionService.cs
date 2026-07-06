using Infrastructure.Factories;
using Infrastructure.Services;
using Lingua;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MauiApp2.Services
{
    /// <summary>
    /// Detects whether a piece of text is written in a learner's target or native
    /// language. Used to distinguish a deliberate native-language message (e.g. asking
    /// a question) from an attempt at the target language that may contain mistakes.
    /// </summary>
    public class LanguageDetectionService
    {
        private readonly ILogger<LanguageDetectionService> _logger;

        // Building a detector loads language models, so one is cached per (target, native)
        // language pair and reused across calls instead of rebuilding it every time.
        private readonly ConcurrentDictionary<(string Target, string Native), LanguageDetector> _detectors = new();

        public LanguageDetectionService(ILogger<LanguageDetectionService> logger)
        {
            _logger = logger;
        }

        public string? DetectLanguageCode(string text, string targetLanguageCode, string nativeLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (string.IsNullOrWhiteSpace(targetLanguageCode) || string.IsNullOrWhiteSpace(nativeLanguageCode))
                return null;

            try
            {
                var detector = GetOrCreateDetector(targetLanguageCode, nativeLanguageCode);
                if (detector is null)
                    return null;

                return ToCode(detector.DetectLanguageOf(text));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect language");
                return null;
            }
        }

        public bool IsTargetLanguage(string text, string targetLanguageCode, string nativeLanguageCode) =>
            DetectLanguageCode(text, targetLanguageCode, nativeLanguageCode) == targetLanguageCode;

        public bool IsNativeLanguage(string text, string targetLanguageCode, string nativeLanguageCode) =>
            DetectLanguageCode(text, targetLanguageCode, nativeLanguageCode) == nativeLanguageCode;

        private LanguageDetector? GetOrCreateDetector(string targetLanguageCode, string nativeLanguageCode)
        {
            var key = (targetLanguageCode, nativeLanguageCode);
            if (_detectors.TryGetValue(key, out var cached))
                return cached;

            if (!LanguageServiceFactory.TryGetLanguageService(targetLanguageCode, out var targetService) ||
                !LanguageServiceFactory.TryGetLanguageService(nativeLanguageCode, out var nativeService))
            {
                return null;
            }

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var detector = LanguageDetectorBuilder
                .FromLanguages(targetService!.GetLinguaLanguage(), nativeService!.GetLinguaLanguage())
                .WithPreloadedLanguageModels()
                .Build();

            return _detectors.GetOrAdd(key, detector);
        }

        private static string? ToCode(Language language)
        {
            return language switch
            {
                Language.Portuguese => AvailableCodes.Portuguese,
                Language.French => AvailableCodes.French,
                Language.Nynorsk => AvailableCodes.Norwegian,
                Language.Italian => AvailableCodes.Italian,
                Language.English => AvailableCodes.English,
                Language.German => AvailableCodes.German,
                Language.Chinese => AvailableCodes.Chinese,
                Language.Vietnamese => AvailableCodes.Vietnamese,
                _ => null
            };
        }
    }
}