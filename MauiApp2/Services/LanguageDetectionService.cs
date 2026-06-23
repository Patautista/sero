using Business.Interfaces;
using Infrastructure.Factories;
using Infrastructure.Services;
using Lingua;

namespace MauiApp1.Services
{
    public class LanguageDetectionService
    {

        public LanguageDetectionService()
        {
        }

        public string? DetectLanguageCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // TODO: Replace placeholders
            var targetCode = "fr";
            var nativeCode = "en";

            if (string.IsNullOrWhiteSpace(targetCode) || string.IsNullOrWhiteSpace(nativeCode))
                return null;

            try
            {
                var targetLanguage = LanguageServiceFactory.GetLanguageService(targetCode).GetLinguaLanguage();
                var nativeLanguage = LanguageServiceFactory.GetLanguageService(nativeCode).GetLinguaLanguage();

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

        private string ToCode(Language language)
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
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }
    }
}