namespace Infrastructure.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for example sentence providers
    /// </summary>
    public interface IExampleProvider
    {
        /// <summary>
        /// Gets the name of the provider (e.g., "Tatoeba", "Cambridge Corpus")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the language code(s) this provider serves (e.g., "eng", "por", "all")
        /// </summary>
        string LanguageCode { get; }

        /// <summary>
        /// Gets whether this provider supports all languages or is language-specific
        /// </summary>
        bool SupportsAllLanguages { get; }

        /// <summary>
        /// Gets whether this provider supports searching with query text
        /// </summary>
        bool SupportsSearch { get; }

        /// <summary>
        /// Gets whether this provider supports exact matching
        /// </summary>
        bool SupportsExactMatch { get; }

        /// <summary>
        /// Gets whether this provider includes translations of examples
        /// </summary>
        bool SupportsTranslations { get; }

        /// <summary>
        /// Searches for example sentences containing the specified text
        /// </summary>
        /// <param name="query">The text to search for</param>
        /// <param name="languageCode">The ISO language code (e.g., "eng", "por")</param>
        /// <param name="exactMatch">Whether to search for exact matches only</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of example sentences</returns>
        Task<ExampleSearchResult> SearchExamplesAsync(
            string query, 
            string languageCode, 
            bool exactMatch = false,
            int maxResults = 10);

        /// <summary>
        /// Gets examples for a specific word (if provider supports word-specific lookups)
        /// </summary>
        /// <param name="word">The word to get examples for</param>
        /// <param name="languageCode">The ISO language code</param>
        /// <returns>List of example sentences</returns>
        Task<List<ExampleSentence>> GetExamplesForWordAsync(string word, string languageCode);
    }

    /// <summary>
    /// Result of an example search with pagination info
    /// </summary>
    public record ExampleSearchResult
    {
        public string Query { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public ICollection<ExampleSentence> Examples { get; set; } = new List<ExampleSentence>();
    }

    /// <summary>
    /// An example sentence with optional metadata
    /// </summary>
    public record ExampleSentence
    {
        /// <summary>
        /// Unique identifier (if available from provider)
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The example sentence text
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The language of the sentence (ISO code)
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Translation of the sentence (if available)
        /// </summary>
        public string? Translation { get; set; }

        /// <summary>
        /// Source or attribution (e.g., "Tatoeba", "Cambridge English Corpus")
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Additional context or metadata
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Provider-specific metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}