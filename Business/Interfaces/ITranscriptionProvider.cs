using System.Threading.Tasks;

namespace Business.Interfaces
{
    /// <summary>
    /// Interface for phonetic transcription providers (IPA, pinyin, etc.)
    /// </summary>
    public interface ITranscriptionProvider
    {
        /// <summary>
        /// Gets the name of the provider (e.g., "toIPA", "Pinyin Converter")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the language code this provider serves (e.g., "en-US", "zh-CN")
        /// </summary>
        string LanguageCode { get; }

        /// <summary>
        /// Gets the type of transcription this provider produces (e.g., "IPA", "Pinyin")
        /// </summary>
        string TranscriptionType { get; }

        /// <summary>
        /// Gets whether this provider supports the specified language
        /// </summary>
        bool SupportsLanguage(string languageCode);

        /// <summary>
        /// Gets the phonetic transcription for a word
        /// </summary>
        /// <param name="word">The word to transcribe</param>
        /// <returns>Phonetic transcription string</returns>
        Task<string?> GetTranscriptionAsync(string word);

        /// <summary>
        /// Gets the phonetic transcription for a collection of words
        /// </summary>
        /// <param name="words">The collection of words to transcribe</param>
        /// <returns>Phonetic transcription string for the combined text</returns>
        Task<string?> GetTranscriptionAsync(IEnumerable<string> words);
    }
}
