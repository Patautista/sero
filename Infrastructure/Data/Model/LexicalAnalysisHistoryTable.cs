using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Model
{
    /// <summary>
    /// Stores history of lexical analyses performed by users.
    /// Each record represents when a user analyzed a specific text.
    /// </summary>
    public class LexicalAnalysisHistoryTable
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Reference to the cached lexical analysis
        /// </summary>
        [ForeignKey(nameof(LexicalAnalysisCache))]
        public int LexicalAnalysisCacheId { get; set; }

        /// <summary>
        /// Navigation property to the cached analysis
        /// </summary>
        public LexicalAnalysisTable? LexicalAnalysisCache { get; set; }

        /// <summary>
        /// When this analysis was performed
        /// </summary>
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
