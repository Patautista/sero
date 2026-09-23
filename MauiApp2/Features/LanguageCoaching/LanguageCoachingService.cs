using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using MauiApp2.Services;
using Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2.Features.LanguageCoaching
{
    public class LanguageCoachingService
    {
        private readonly IPetDataStore _store;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ILogger<LanguageCoachingService> _logger;

        public LanguageCoachingService(
            IPetDataStore store,
            LocalApiService api,
            ConfigurationService config,
            ILogger<LanguageCoachingService> logger)
        {
            _store = store;
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
                    var existingMistake = await _store.LanguageMistakes
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
                        existingMistake.MistakeType = mistake.MistakeType;
                        existingMistake.Status = MistakeChallengeStatus.Active;
                        existingMistake.ConsecutiveCorrectCount = 0;
                        existingMistake.ResolvedAt = null;
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
                            LastSeenAt = DateTime.UtcNow,
                            Status = MistakeChallengeStatus.Active,
                            ConsecutiveCorrectCount = 0
                        };

                        _store.LanguageMistakes.Add(newMistake);
                    }
                }

                await _store.SaveChangesAsync();
                _logger.LogInformation($"Recorded {mistakes.Count} mistakes for user {userProfileId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording mistakes");
            }
        }

        public async Task<List<string>> AdvanceChallengeProgressAsync(int userProfileId, IReadOnlyCollection<string> conceptsMistakenThisTurn)
        {
            try
            {
                var mistakenConcepts = conceptsMistakenThisTurn
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var activeChallenges = (await _store.LanguageMistakes
                    .WhereAsync(m => m.UserProfileId == userProfileId && m.Status == MistakeChallengeStatus.Active))
                    .ToList();

                if (activeChallenges.Count == 0)
                    return new List<string>();

                var now = DateTime.UtcNow;
                var newlyResolved = new List<string>();

                foreach (var challenge in activeChallenges)
                {
                    if (mistakenConcepts.Contains(challenge.Concept))
                        continue;

                    challenge.ConsecutiveCorrectCount++;
                    if (challenge.ConsecutiveCorrectCount >= PracticeChallenge.MasteryThreshold)
                    {
                        challenge.Status = MistakeChallengeStatus.Resolved;
                        challenge.ResolvedAt = now;
                        challenge.ConsecutiveCorrectCount = PracticeChallenge.MasteryThreshold;
                        newlyResolved.Add(challenge.Concept);
                    }
                }

                await _store.SaveChangesAsync();
                return newlyResolved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error advancing practice challenge progress for user {UserProfileId}", userProfileId);
                return new List<string>();
            }
        }

        public async Task<List<PracticeChallenge>> GetActiveChallengesAsync(int userProfileId, int take = 3)
        {
            try
            {
                var activeMistakes = await _store.LanguageMistakes
                    .WhereAsync(m => m.UserProfileId == userProfileId && m.Status == MistakeChallengeStatus.Active);

                var mistakes = activeMistakes
                    .OrderByDescending(m => m.OccurrenceCount)
                    .ThenByDescending(m => m.LastSeenAt)
                    .Take(take)
                    .Select(m => new PracticeChallenge
                    {
                        Concept = m.Concept,
                        MistakeType = m.MistakeType,
                        OriginalText = m.OriginalText,
                        CorrectedText = m.CorrectedText,
                        OccurrenceCount = m.OccurrenceCount,
                        ConsecutiveCorrectCount = m.ConsecutiveCorrectCount
                    })
                    .ToList();

                return mistakes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active practice challenges for user {UserProfileId}", userProfileId);
                return new List<PracticeChallenge>();
            }
        }

        public async Task<List<RecurringMistake>> GetRecurringMistakesAsync(int userProfileId, int minOccurrences = 2)
        {
            try
            {
                var mistakes = (await _store.LanguageMistakes
                    .WhereAsync(m => m.UserProfileId == userProfileId && m.OccurrenceCount >= minOccurrences))
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
                    .ToList();

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
                var mistakes = (await _store.LanguageMistakes
                    .WhereAsync(m => m.UserProfileId == userProfileId))
                    .ToList();

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
                var mistakes = (await _store.LanguageMistakes
                    .WhereAsync(m => m.UserProfileId == userProfileId))
                    .ToList();

                var distribution = mistakes
                    .GroupBy(m => m.MistakeType)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.OccurrenceCount));

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
