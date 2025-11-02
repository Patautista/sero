using Infrastructure.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.Languages;

namespace Infrastructure.Factories;

public static class LanguageServiceFactory
{
    private static readonly Dictionary<string, ILanguageService> _languageServices = new()
    {
        { AvailableCodes.Italian, new ItalianLanguageService() },
        { AvailableCodes.English, new EnglishLanguageService() },
        { AvailableCodes.Norwegian, new NorwegianLanguageService() },
        { AvailableCodes.German, new GermanLanguageService() },
        { AvailableCodes.Chinese, new ChineseLanguageService() },
        { AvailableCodes.Vietnamese, new VietnameseLanguageService() },
        { AvailableCodes.Portuguese, new PortugueseLanguageService() }
    };

    /// <summary>
    /// Gets a language service implementation for the specified ISO language code
    /// </summary>
    /// <param name="languageCode">Two-letter ISO language code (e.g., "it", "en")</param>
    /// <returns>Language service implementation</returns>
    /// <exception cref="ArgumentException">Thrown when language code is not supported</exception>
    public static ILanguageService GetLanguageService(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("Language code cannot be null or empty", nameof(languageCode));

        if (_languageServices.TryGetValue(languageCode, out var service))
            return service;

        throw new ArgumentException($"Unsupported language code: {languageCode}", nameof(languageCode));
    }

    /// <summary>
    /// Attempts to get a language service for the specified ISO language code
    /// </summary>
    /// <param name="languageCode">Two-letter ISO language code</param>
    /// <param name="service">The language service if found</param>
    /// <returns>True if the language is supported, false otherwise</returns>
    public static bool TryGetLanguageService(string languageCode, out ILanguageService? service)
    {
        service = null;
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;

        return _languageServices.TryGetValue(languageCode, out service);
    }

    /// <summary>
    /// Gets all supported language codes
    /// </summary>
    public static IEnumerable<string> GetSupportedLanguages()
    {
        return _languageServices.Keys;
    }
}