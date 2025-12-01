using System.Collections.Generic;
using Mosaik.Core;
using Lingua;
using Infrastructure.Interfaces;

namespace Infrastructure.Languages.Interface;

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

    /// <summary>
    /// Gets available definition providers for this language
    /// </summary>
    /// <returns>List of definition providers configured for this language</returns>
    IEnumerable<IDefinitionProvider> GetDefinitionProviders();

    /// <summary>
    /// Gets available example sentence providers for this language
    /// </summary>
    /// <returns>List of example providers configured for this language</returns>
    IEnumerable<IExampleProvider> GetExampleProviders();

    /// <summary>
    /// Gets available transcription providers for this language
    /// </summary>
    /// <returns>List of transcription providers configured for this language</returns>
    IEnumerable<ITranscriptionProvider> GetTranscriptionProviders();
}