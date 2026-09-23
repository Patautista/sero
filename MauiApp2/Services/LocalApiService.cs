using Business.Audio;
using MauiApp2.Services.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MauiApp2.Services
{
    public class LocalApiService : IApiService
    {
        private readonly IChatClient _chatClient;
        private readonly ISpeechService? _speechService;
        private readonly ICompanionPromptBuilder _promptBuilder;
        private readonly ILogger<LocalApiService> _logger;
        private readonly Dictionary<string, string> _translationCache = new();

        public LocalApiService(
            IChatClient chatClient,
            ILogger<LocalApiService> logger,
            ICompanionPromptBuilder promptBuilder,
            ISpeechService? speechService = null)
        {
            _chatClient = chatClient;
            _logger = logger;
            _promptBuilder = promptBuilder;
            _speechService = speechService;
        }

        // Translation
        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return string.Empty;

                if (sourceLang == targetLang)
                    return text;

                var cacheKey = $"{sourceLang}:{targetLang}:{text}";
                if (_translationCache.TryGetValue(cacheKey, out var cachedTranslation))
                {
                    _logger.LogInformation("Translation retrieved from cache");
                    return cachedTranslation;
                }

                var prompt = _promptBuilder.BuildTranslationPrompt(text, sourceLang, targetLang);

                var completion = await _chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)]);
                var translation = (completion.Messages[^1].Text ?? string.Empty).Trim().Trim('"');

                _translationCache[cacheKey] = translation;
                return translation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text");
                return text; // Fallback to original
            }
        }

        // TTS - Generates real speech audio via Google Cloud Text-to-Speech (ISpeechService),
        // the same infrastructure demonstrated in TestScripts/GoogleSpeechTest.cs. Falls back
        // to an empty array (silently skipping playback) if the service isn't configured.
        public async Task<byte[]> GetTTSAsync(string text, string language)
        {
            if (_speechService is null)
            {
                _logger.LogWarning("TTS requested but no ISpeechService is configured; returning empty audio.");
                return Array.Empty<byte>();
            }

            try
            {
                _logger.LogInformation($"TTS requested for text: {text} in language: {language}");
                return await _speechService.GenerateSpeechAsync(text, VoiceGender.Female, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating TTS");
                return Array.Empty<byte>();
            }
        }

        // Lexical Analysis
        public async Task<LexicalAnalysisResult> AnalyzeLexicalAsync(string text, string language)
        {
            try
            {
                var prompt = _promptBuilder.BuildLexicalAnalysisPrompt(text, language);

                var jsonOptions = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };
                var completion = await _chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], jsonOptions);
                var result = JsonSerializer.Deserialize<LexicalAnalysisResult>(
                    completion.Messages[^1].Text ?? "{}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new LexicalAnalysisResult { Chunks = new List<LexicalChunk>() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text lexically");
                return new LexicalAnalysisResult { Chunks = new List<LexicalChunk>() };
            }
        }

        // Mistake Detection
        public async Task<MistakeAnalysisResult> DetectMistakesAsync(string text, string language)
        {
            try
            {
                var prompt = _promptBuilder.BuildMistakeDetectionPrompt(text, language);

                var jsonOptions = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };
                var completion = await _chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], jsonOptions);
                var result = JsonSerializer.Deserialize<MistakeAnalysisResult>(
                    completion.Messages[^1].Text ?? "{}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new MistakeAnalysisResult { HasMistakes = false, Mistakes = new List<MistakeDetail>() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting mistakes");
                return new MistakeAnalysisResult { HasMistakes = false, Mistakes = new List<MistakeDetail>() };
            }
        }

        // AI Generation (general purpose)
        public async Task<string> GenerateTextAsync(string prompt, Type? schemaType = null)
        {
            try
            {
                if (schemaType != null)
                {
                    JsonNode schema = JsonSerializerOptions.Default.GetJsonSchemaAsNode(schemaType);
                    prompt = $"{prompt}\n\nRespond with valid JSON matching this schema:\n{schema.ToJsonString()}";
                }

                var jsonOptions = schemaType != null ? new ChatOptions { ResponseFormat = ChatResponseFormat.Json } : null;
                var completion = await _chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], jsonOptions);
                return completion.Messages[^1].Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return string.Empty;
            }
        }
    }

    // DTOs
    public class LexicalAnalysisResult
    {
        [JsonPropertyName("chunks")]
        public List<LexicalChunk> Chunks { get; set; } = new();
    }

    public class LexicalChunk
    {
        [JsonPropertyName("word")]
        public string Word { get; set; } = string.Empty;

        [JsonPropertyName("translation")]
        public string Translation { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;
    }

    public class MistakeAnalysisResult
    {
        [JsonPropertyName("hasMistakes")]
        public bool HasMistakes { get; set; }

        [JsonPropertyName("mistakes")]
        public List<MistakeDetail> Mistakes { get; set; } = new();
    }

    public class MistakeDetail
    {
        [JsonPropertyName("segment")]
        public string Segment { get; set; } = string.Empty;

        [JsonPropertyName("corrected")]
        public string Corrected { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("concept")]
        public string Concept { get; set; } = string.Empty;
    }

    public class TranslationResult
    {
        public string Translation { get; set; } = string.Empty;
        public byte[] Audio { get; set; } = Array.Empty<byte>();
    }

    public class MeaningResult
    {
        public string Translation { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }
}
