using Mosaik.Core;
using Lingua;

namespace Infrastructure.Interfaces;

public interface ILanguageService
{
    /// <summary>
    /// Registers the Catalyst language model for NLP processing
    /// </summary>
    void RegisterLanguageModel();

    /// <summary>
    /// Gets the Catalyst Language enum for this language
    /// </summary>
    Mosaik.Core.Language GetCatalystLanguage();

    /// <summary>
    /// Gets the Lingua Language enum for language detection
    /// </summary>
    Lingua.Language GetLinguaLanguage();

    /// <summary>
    /// Indicates whether conjugation tables are available for this language
    /// </summary>
    bool HasConjugationTable { get; }

    /// <summary>
    /// Gets the two-letter ISO language code
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Gets the default RSS feed URL for this language
    /// </summary>
    string GetDefaultRssFeedUrl();
}