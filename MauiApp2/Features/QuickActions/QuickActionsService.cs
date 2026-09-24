using MauiApp2.Services;
using MauiApp2.Services.AI;
using MauiApp2.Services.AI.Schemas;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2.Features.QuickActions
{
    public class QuickActionsService
    {
        private readonly LocalApiService _api;
        private readonly ICompanionPromptBuilder _promptBuilder;
        private readonly ILogger<QuickActionsService> _logger;
        private readonly PetDbContext _dbContext;

        public QuickActionsService(
            LocalApiService api,
            ICompanionPromptBuilder promptBuilder,
            ILogger<QuickActionsService> logger,
            PetDbContext dbContext)
        {
            _api = api;
            _promptBuilder = promptBuilder;
            _logger = logger;
            _dbContext = dbContext;
        }

        public Task AddHistoryAsync(int userProfileId, string actionType, string originalText, string summary)
        {
            _dbContext.QuickActionHistories.Insert(new QuickActionHistoryTable
            {
                UserProfileId = userProfileId,
                ActionType = actionType,
                OriginalText = originalText,
                Summary = summary,
                UsedAt = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task<List<QuickActionHistoryItem>> GetHistoryAsync(int userProfileId, string actionType)
        {
            var history = _dbContext.QuickActionHistories
                .Find(entry => entry.UserProfileId == userProfileId && entry.ActionType == actionType)
                .OrderByDescending(entry => entry.UsedAt)
                .Select(entry => new QuickActionHistoryItem
                {
                    OriginalText = entry.OriginalText,
                    Summary = entry.Summary,
                    UsedAt = entry.UsedAt
                })
                .ToList();

            return Task.FromResult(history);
        }

        public async Task<PronunciationResult> GetPronunciationAsync(string text, string language)
        {
            try
            {
                _logger.LogInformation($"Getting pronunciation for: {text} in {language}");

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new PronunciationResult
                    {
                        Success = false,
                        ErrorMessage = "Please enter some text"
                    };
                }

                var audio = await _api.GetTTSAsync(text, language);

                return new PronunciationResult
                {
                    Text = text,
                    AudioData = audio,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pronunciation");
                return new PronunciationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to get pronunciation"
                };
            }
        }

        public async Task<TranslationResult> TranslateAsync(string text, string fromLang, string toLang)
        {
            try
            {
                _logger.LogInformation($"Translating from {fromLang} to {toLang}: {text}");

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new TranslationResult
                    {
                        Success = false,
                        ErrorMessage = "Please enter some text"
                    };
                }

                var translation = await _api.TranslateAsync(text, fromLang, toLang);
                var audio = await _api.GetTTSAsync(translation, toLang);

                var analysis = await _api.AnalyzeLexicalAsync(translation, toLang);
                var chunks = analysis.Chunks.Select(c => new TextChunk
                {
                    Word = c.Word,
                    Translation = c.Translation,
                    Note = c.Note
                }).ToList();

                return new TranslationResult
                {
                    OriginalText = text,
                    TranslatedText = translation,
                    AudioData = audio,
                    Chunks = chunks,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text");
                return new TranslationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to translate text"
                };
            }
        }

        public async Task<MeaningResult> GetMeaningAsync(string text, string targetLang, string nativeLang)
        {
            try
            {
                _logger.LogInformation($"Getting meaning for: {text}");

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new MeaningResult
                    {
                        Success = false,
                        ErrorMessage = "Please enter some text"
                    };
                }

                // Get translation
                var translation = await _api.TranslateAsync(text, targetLang, nativeLang);

                // Get context and example using AI
                var contextPrompt = _promptBuilder.BuildWordContextPrompt(text, targetLang);

                var contextResponse = await _api.GenerateTextAsync(contextPrompt, typeof(WordContextSchema));

                string context = "";
                string example = "";

                try
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(contextResponse);
                    context = jsonDoc.RootElement.GetProperty("context").GetString() ?? "";
                    example = jsonDoc.RootElement.GetProperty("example").GetString() ?? "";
                }
                catch
                {
                    context = "No additional context available";
                    example = "";
                }

                return new MeaningResult
                {
                    Word = text,
                    Translation = translation,
                    Context = context,
                    Example = example,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meaning");
                return new MeaningResult
                {
                    Success = false,
                    ErrorMessage = "Failed to get meaning"
                };
            }
        }

        public async Task<TextAnalysisResult> AnalyzeTextAsync(string text, string language)
        {
            try
            {
                _logger.LogInformation($"Analyzing text: {text}");

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new TextAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = "Please enter some text"
                    };
                }

                var analysis = await _api.AnalyzeLexicalAsync(text, language);

                var chunks = analysis.Chunks.Select(c => new TextChunk
                {
                    Word = c.Word,
                    Translation = c.Translation,
                    Note = c.Note
                }).ToList();

                return new TextAnalysisResult
                {
                    OriginalText = text,
                    Chunks = chunks,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text");
                return new TextAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Failed to analyze text"
                };
            }
        }
    }
}
