using Infrastructure.Data;
using MauiApp2.Services;
using Domain.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2.Features.LanguageCoaching
{
    public class LanguageCoachingService
    {
        private readonly PetDbContext _db;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ILogger<LanguageCoachingService> _logger;

        public LanguageCoachingService(
            PetDbContext db,
            LocalApiService api,
            ConfigurationService config,
            ILogger<LanguageCoachingService> logger)
        {
            _db = db;
            _api = api;
            _config = config;
            _logger = logger;
        }

        public async Task<MistakeAnalysisResult> AnalyzeMessageAsync(string text, string language, int userProfileId)
        {
            try
            {
                if (!_config.EnableMistakeDetection)
                {
                    return new MistakeAnalysisResult { HasMistakes = false };
                }

                _logger.LogInformation($"Analyzing message for mistakes: {text}");

                var apiResult = await _api.DetectMistakesAsync(text, language);

                if (!apiResult.HasMistakes)
                {
                    return new MistakeAnalysisResult { HasMistakes = false };
                }

                var mistakes = apiResult.Mistakes.Select(m => new DetectedMistake
                {
                    OriginalSegment = m.Segment,
                    CorrectedSegment = m.Corrected,
                    MistakeType = m.Type,
                    Concept = m.Concept,
                    Severity = 3 // Default severity
                }).ToList();

                // Record mistakes in database
                await RecordMistakesAsync(userProfileId, mistakes);

                return new MistakeAnalysisResult
                {
                    HasMistakes = true,
                    Mistakes = mistakes,
                    OverallFeedback = GenerateFeedbackSummary(mistakes)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing message for mistakes");
                return new MistakeAnalysisResult { HasMistakes = false };
            }
        }

        public async Task RecordMistakesAsync(int userProfileId, List<DetectedMistake> mistakes)
        {
            try
            {
                foreach (var mistake in mistakes)
                {
                    // Check if this concept has been seen before
                    var existingMistake = await _db.LanguageMistakes
                        .FirstOrDefaultAsync(m => 
                            m.UserProfileId == userProfileId && 
                            m.Concept == mistake.Concept);

                    if (existingMistake != null)
                    {
                        // Update existing mistake
                        existingMistake.OccurrenceCount++;
                        existingMistake.LastSeenAt = DateTime.UtcNow;
                        existingMistake.OriginalText = mistake.OriginalSegment;
                        existingMistake.CorrectedText = mistake.CorrectedSegment;
                    }
                    else
                    {
                        // Create new mistake record
                        var newMistake = new LanguageMistakeTable
                        {
                            UserProfileId = userProfileId,
                            OriginalText = mistake.OriginalSegment,
                            CorrectedText = mistake.CorrectedSegment,
                            MistakeType = mistake.MistakeType,
                            Concept = mistake.Concept,
                            OccurrenceCount = 1,
                            FirstSeenAt = DateTime.UtcNow,
                            LastSeenAt = DateTime.UtcNow
                        };

                        _db.LanguageMistakes.Add(newMistake);
                    }
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation($"Recorded {mistakes.Count} mistakes for user {userProfileId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording mistakes");
            }
        }

        public async Task<List<RecurringMistake>> GetRecurringMistakesAsync(int userProfileId, int minOccurrences = 2)
        {
            try
            {
                var mistakes = await _db.LanguageMistakes
                    .Where(m => m.UserProfileId == userProfileId && m.OccurrenceCount >= minOccurrences)
                    .OrderByDescending(m => m.OccurrenceCount)
                    .ThenByDescending(m => m.LastSeenAt)
                    .Take(10)
                    .Select(m => new RecurringMistake
                    {
                        Id = m.Id,
                        Concept = m.Concept,
                        MistakeType = m.MistakeType,
                        OccurrenceCount = m.OccurrenceCount,
                        LatestExample = m.OriginalText,
                        LastSeenAt = m.LastSeenAt
                    })
                    .ToListAsync();

                return mistakes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recurring mistakes");
                return new List<RecurringMistake>();
            }
        }

        public async Task<StruggleProfile> GetStruggleProfileAsync(int userProfileId)
        {
            try
            {
                var mistakes = await _db.LanguageMistakes
                    .Where(m => m.UserProfileId == userProfileId)
                    .ToListAsync();

                var conceptFrequency = mistakes
                    .GroupBy(m => m.Concept)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.OccurrenceCount));

                var topStruggles = conceptFrequency
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .Select(kvp => kvp.Key)
                    .ToList();

                return new StruggleProfile
                {
                    ConceptFrequency = conceptFrequency,
                    TopStruggles = topStruggles,
                    TotalMistakes = mistakes.Sum(m => m.OccurrenceCount),
                    LastAnalyzed = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting struggle profile");
                return new StruggleProfile();
            }
        }

        public async Task<string> GenerateNaturalCorrectionAsync(string originalText, List<DetectedMistake> mistakes)
        {
            try
            {
                if (!mistakes.Any())
                    return string.Empty;

                var mistakesText = string.Join("\n", mistakes.Select(m => 
                    $"- '{m.OriginalSegment}' → '{m.CorrectedSegment}' ({m.MistakeType}: {m.Concept})"));

                var prompt = $@"Generate a natural, friendly correction for a language learner.

Original text: ""{originalText}""

Mistakes detected:
{mistakesText}

Create a gentle, encouraging correction that:
1. Acknowledges what they're trying to say
2. Explains the correction in simple terms
3. Keeps a supportive, non-judgmental tone
4. Is brief (2-3 sentences)

Return ONLY the correction text, no JSON.";

                var correction = await _api.GenerateTextAsync(prompt);
                return correction.Trim().Trim('"');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating natural correction");
                return "I noticed a small mistake, but let's keep practicing! 😊";
            }
        }

        private string GenerateFeedbackSummary(List<DetectedMistake> mistakes)
        {
            if (!mistakes.Any())
                return "Great job! No mistakes detected.";

            var grammarCount = mistakes.Count(m => m.MistakeType.ToLower().Contains("grammar"));
            var vocabCount = mistakes.Count(m => m.MistakeType.ToLower().Contains("vocabulary"));
            var spellingCount = mistakes.Count(m => m.MistakeType.ToLower().Contains("spelling"));

            if (grammarCount > vocabCount && grammarCount > spellingCount)
                return $"Focus on grammar - {grammarCount} grammar point(s) to review.";
            else if (vocabCount > 0)
                return $"Let's work on vocabulary - {vocabCount} word(s) to learn.";
            else if (spellingCount > 0)
                return $"Watch your spelling - {spellingCount} small typo(s).";
            else
                return $"{mistakes.Count} learning opportunity detected.";
        }

        public async Task<Dictionary<string, int>> GetMistakeTypeDistributionAsync(int userProfileId)
        {
            try
            {
                var distribution = await _db.LanguageMistakes
                    .Where(m => m.UserProfileId == userProfileId)
                    .GroupBy(m => m.MistakeType)
                    .Select(g => new { Type = g.Key, Count = g.Sum(m => m.OccurrenceCount) })
                    .ToDictionaryAsync(x => x.Type, x => x.Count);

                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mistake type distribution");
                return new Dictionary<string, int>();
            }
        }
    }
}
