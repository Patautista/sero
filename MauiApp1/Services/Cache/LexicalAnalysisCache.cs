using Infrastructure.Data;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MauiApp1.Services;

namespace MauiApp1.Services.Cache
{
    public class LexicalAnalysisCache
    {
        private readonly MobileDbContext _dbContext;

        public LexicalAnalysisCache(MobileDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Normalizes text by converting to lowercase and removing punctuation
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Convert to lowercase and remove punctuation
            var normalized = text.ToLowerInvariant();
            normalized = Regex.Replace(normalized, @"[.,!?;:\-—""'()[\]{}]", "");
            // Collapse multiple spaces into one
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Gets cached lexical analysis for the given text and language
        /// </summary>
        public async Task<LexicalAnalysisResponse?> GetAsync(string text, string languageCode)
        {
            try
            {
                var normalizedText = NormalizeText(text);
                if (string.IsNullOrEmpty(normalizedText))
                    return null;

                var count = _dbContext.LexicalAnalyses.Count();

                var cached = await _dbContext.LexicalAnalyses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => 
                        l.NormalizedText == normalizedText && 
                        l.LanguageCode == languageCode);

                if (cached == null)
                    return null;

                // Update last accessed time (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var entity = await _dbContext.LexicalAnalyses
                            .FirstOrDefaultAsync(l => l.Id == cached.Id);
                        if (entity != null)
                        {
                            entity.LastAccessedAt = DateTime.UtcNow;
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating LastAccessedAt: {ex.Message}");
                    }
                });

                // Deserialize the JSON
                var response = JsonSerializer.Deserialize<LexicalAnalysisResponse>(cached.AnalysisJson);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving cached lexical analysis: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves lexical analysis to the cache
        /// </summary>'
        public async Task SetAsync(string text, string languageCode, LexicalAnalysisResponse analysis)
        {
            try
            {
                var normalizedText = NormalizeText(text);
                if (string.IsNullOrEmpty(normalizedText))
                    return;

                // Serialize the analysis
                var json = JsonSerializer.Serialize(analysis);

                // Check if already exists
                var existing = await _dbContext.LexicalAnalyses
                    .FirstOrDefaultAsync(l => 
                        l.NormalizedText == normalizedText && 
                        l.LanguageCode == languageCode);

                if (existing != null)
                {
                    // Update existing
                    existing.OriginalText = text;
                    existing.AnalysisJson = json;
                    existing.LastAccessedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new
                    var newEntry = new LexicalAnalysisTable
                    {
                        NormalizedText = normalizedText,
                        OriginalText = text,
                        LanguageCode = languageCode,
                        AnalysisJson = json,
                        CreatedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow
                    };

                    _dbContext.LexicalAnalyses.Add(newEntry);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error caching lexical analysis: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears old cache entries (e.g., not accessed in the last 30 days)
        /// </summary>
        public async Task CleanupOldEntriesAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                
                var oldEntries = await _dbContext.LexicalAnalyses
                    .Where(l => l.LastAccessedAt < cutoffDate)
                    .ToListAsync();

                if (oldEntries.Any())
                {
                    _dbContext.LexicalAnalyses.RemoveRange(oldEntries);
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"Cleaned up {oldEntries.Count} old lexical analysis cache entries");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up old cache entries: {ex.Message}");
            }
        }
    }
}
