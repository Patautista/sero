namespace Business.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for dictionary/definition providers
    /// </summary>
    public interface IDefinitionProvider
    {
        /// <summary>
        /// Gets the name of the provider (e.g., "Cambridge Dictionary", "Dict.cc")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the language pair this provider serves (e.g., "english-vietnamese", "deen")
        /// </summary>
        string LanguagePair { get; }

        /// <summary>
        /// Gets whether this provider supports structured model output
        /// </summary>
        bool SupportsStructuredOutput { get; }

        /// <summary>
        /// Gets definitions as structured models
        /// </summary>
        /// <param name="word">The word to look up</param>
        /// <returns>Structured definition result</returns>
        Task<DefinitionResult> GetDefinitionsAsync(string word);

        /// <summary>
        /// Gets definitions as HTML
        /// </summary>
        /// <param name="word">The word to look up</param>
        /// <returns>HTML string with definitions</returns>
        Task<string> GetDefinitionsHtmlAsync(string word);

        /// <summary>
        /// Gets example sentences (if supported by provider)
        /// </summary>
        /// <param name="word">The word to look up</param>
        /// <returns>List of example sentences</returns>
        Task<List<Example>> GetExamplesAsync(string word);
    }

    /// <summary>
    /// Generic definition result that works for all providers
    /// </summary>
    public record DefinitionResult
    {
        public string Word { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public ICollection<DefinitionEntry> Entries { get; set; } = new List<DefinitionEntry>();
    }

    public record DefinitionEntry
    {
        public string Headword { get; set; } = string.Empty;
        public string? PartOfSpeech { get; set; }
        public string? Pronunciation { get; set; }
        public ICollection<DefinitionMeaning> Meanings { get; set; } = new List<DefinitionMeaning>();
    }

    public record DefinitionMeaning
    {
        public string Definition { get; set; } = string.Empty;
        public string DefinitionLanguage { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string TranslationLanguage { get; set; } = string.Empty;
        public ICollection<string> Examples { get; set; } = new List<string>();
    }

    public record Example
    {
        public string Sentence { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? Context { get; set; }
    }
}