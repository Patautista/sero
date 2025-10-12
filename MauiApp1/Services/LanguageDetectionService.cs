using Business.Interfaces;
using Infrastructure.Services;
using Lingua;
using static Lingua.Language;

namespace MauiApp1.Services
{
    public class LanguageDetectionService
    {
        private readonly ISettingsService _settingsService;

        public LanguageDetectionService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public string? DetectLanguageCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var studyConfig = _settingsService.StudyConfig.Value;
            if (studyConfig?.SelectedLanguage == null)
                return null;

            var targetCode = studyConfig.SelectedLanguage.Target?.TwoLetterISOLanguageName;
            var nativeCode = studyConfig.SelectedLanguage.Source?.TwoLetterISOLanguageName;

            if (string.IsNullOrWhiteSpace(targetCode) || string.IsNullOrWhiteSpace(nativeCode))
                return null;

            try
            {
                var targetLanguage = FromCode(targetCode);
                var nativeLanguage = FromCode(nativeCode);

                Directory.SetCurrentDirectory(AppContext.BaseDirectory);

                var detector = LanguageDetectorBuilder
                    .FromLanguages(targetLanguage, nativeLanguage)
                    .WithPreloadedLanguageModels()
                    .Build();

                var detectedLanguage = detector.DetectLanguageOf(text);

                return ToCode(detectedLanguage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to detect language: {ex.Message}");
                return null;
            }
        }

        public bool IsTargetLanguage(string text, string targetLanguageCode)
        {
            var detected = DetectLanguageCode(text);
            return detected == targetLanguageCode;
        }

        public bool IsNativeLanguage(string text, string nativeLanguageCode)
        {
            var detected = DetectLanguageCode(text);
            return detected == nativeLanguageCode;
        }

        private Language FromCode(string code)
        {
            return code.ToLower() switch
            {
                AvailableCodes.Portuguese or "por" or "portuguese" => Portuguese,
                AvailableCodes.Norwegian or "nob" or "norwegian" => Nynorsk,
                AvailableCodes.Italian or "ita" or "italian" => Italian,
                _ => throw new ArgumentException($"Unsupported language code: {code}")
            };
        }

        private string ToCode(Language language)
        {
            return language switch
            {
                Language.Portuguese => AvailableCodes.Portuguese,
                Language.Nynorsk => AvailableCodes.Norwegian,
                Language.Italian => AvailableCodes.Italian,
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }
    }
}