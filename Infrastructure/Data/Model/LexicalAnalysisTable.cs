using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    /// <summary>
    /// Stores cached lexical analysis results for text content.
    /// The key is the normalized text (lowercase, no punctuation).
    /// </summary>
    public class LexicalAnalysisTable
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Normalized key: lowercase text without punctuation (. , ? ! ; : etc)
        /// Used for quick lookup
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string NormalizedText { get; set; } = string.Empty;

        /// <summary>
        /// Original text that was analyzed
        /// </summary>
        [Required]
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// Language code of the analyzed text (e.g., "en", "pt", "es")
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;

        /// <summary>
        /// JSON serialized LexicalAnalysisResponse containing the analysis results
        /// </summary>
        [Required]
        public string AnalysisJson { get; set; } = string.Empty;

        /// <summary>
        /// When this analysis was created/cached
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time this analysis was accessed (for cache management)
        /// </summary>
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    }
}
