using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MauiApp2.Services
{
    public class LocalApiService : IApiService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<LocalApiService> _logger;
        private readonly Dictionary<string, string> _translationCache = new();

        public LocalApiService(IChatClient chatClient, ILogger<LocalApiService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
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

                var prompt = $@"Translate the following text from {sourceLang} to {targetLang}. Return ONLY the translation, no explanations.

Text to translate: ""{text}""

Translation:";

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

        // TTS - Mock implementation using platform TTS
        public async Task<byte[]> GetTTSAsync(string text, string language)
        {
            try
            {
                // For now, return empty byte array - platform TTS will be used directly in UI
                // In production, this could call Google Cloud TTS API
                _logger.LogInformation($"TTS requested for text: {text} in language: {language}");
                return Array.Empty<byte>();
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
                var prompt = $@"Analyze the following {language} text word-by-word or phrase-by-phrase. Break it down into meaningful chunks with translations and grammar notes.

Text: ""{text}""

Return a JSON object with this structure:
{{
  ""chunks"": [
    {{
      ""word"": ""word or phrase"",
      ""translation"": ""English translation"",
      ""note"": ""grammar note or context (optional)""
    }}
  ]
}}";

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
                var prompt = $@"Analyze the following {language} text for grammar, vocabulary, and spelling mistakes.

Text: ""{text}""

Return a JSON object with this structure:
{{
  ""hasMistakes"": true/false,
  ""mistakes"": [
    {{
      ""segment"": ""incorrect segment from the text"",
      ""corrected"": ""correct version"",
      ""type"": ""Grammar"" or ""Vocabulary"" or ""Spelling"",
      ""concept"": ""brief explanation like 'verb conjugation' or 'article usage'""
    }}
  ]
}}

If there are no mistakes, return {{""hasMistakes"": false, ""mistakes"": []}}";

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
        public async Task<string> GenerateTextAsync(string prompt)
        {
            try
            {
                var completion = await _chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)]);
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
