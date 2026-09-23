using Business.Interfaces;
using MauiApp1.Services.Cache;
using MauiApp2.Services.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace MauiApp2.Services
{
    /// <summary>
    /// AI-powered definition provider for when traditional providers don't find results
    /// </summary>
    public class AiDefinitionHelper
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger _logger;
        private readonly AiDefinitionCache _cache;
        private readonly ICompanionPromptBuilder _promptBuilder;

        public AiDefinitionHelper(
            IChatClient chatClient,
            ILogger logger,
            AiDefinitionCache cache,
            ICompanionPromptBuilder promptBuilder)
        {
            _chatClient = chatClient;
            _logger = logger;
            _cache = cache;
            _promptBuilder = promptBuilder;
        }

        /// <summary>
        /// Generate a definition using AI when no definitions are found
        /// </summary>
        /// <param name="word">The word to define (in the learning language)</param>
        /// <param name="sourceLanguage">The learning language (e.g., Vietnamese)</param>
        /// <param name="targetLanguage">The user's native language (e.g., English)</param>
        public async Task<DefinitionResult> GenerateDefinitionAsync(string word, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var cached = await _cache.GetAsync(word, sourceLanguage, targetLanguage);
                if (cached != null)
                {
                    _logger.LogInformation($"Using cached AI definition for '{word}'");
                    return cached;
                }

                _logger.LogInformation($"Generating AI definition for '{word}'");

                var prompt = _promptBuilder.BuildDefinitionPrompt(word, sourceLanguage, targetLanguage);

                var chatOptions = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };
                var response = await _chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], chatOptions);

                var jsonText = response.Messages[^1].Text;
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    return CreateEmptyResult(word);
                }

                // Parse the JSON response
                var jsonObject = JsonNode.Parse(jsonText) as JsonObject;
                if (jsonObject == null)
                {
                    return CreateEmptyResult(word);
                }

                var definition = jsonObject["definition"]?.GetValue<string>() ?? string.Empty;
                var partOfSpeech = jsonObject["partOfSpeech"]?.GetValue<string>();
                var pronunciation = jsonObject["pronunciation"]?.GetValue<string>();
                var translation = jsonObject["translation"]?.GetValue<string>() ?? string.Empty;
                var examples = jsonObject["examples"]?.AsArray()
                    .Select(e => e?.GetValue<string>() ?? string.Empty)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList() ?? new List<string>();

                var result = new DefinitionResult
                {
                    Word = word,
                    ProviderName = "⚡ AI Definition Generator (AI Generated)",
                    Entries = new List<DefinitionEntry>
                    {
                        new DefinitionEntry
                        {
                            Headword = word,
                            PartOfSpeech = partOfSpeech,
                            Pronunciation = pronunciation,
                            Meanings = new List<DefinitionMeaning>
                            {
                                new DefinitionMeaning
                                {
                                    Definition = definition,
                                    DefinitionLanguage = targetLanguage,
                                    Translation = translation,
                                    TranslationLanguage = sourceLanguage,
                                    Examples = examples
                                }
                            }
                        }
                    }
                };

                try
                {
                    await _cache.SetAsync(word, sourceLanguage, targetLanguage, result);
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, $"Failed to cache AI definition for '{word}'");
                }

                _logger.LogInformation($"Successfully generated AI definition for '{word}'");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating AI definition for '{word}'");
                return CreateEmptyResult(word);
            }
        }

        private DefinitionResult CreateEmptyResult(string word)
        {
            return new DefinitionResult
            {
                Word = word,
                ProviderName = "⚡ AI Definition Generator",
                Entries = new List<DefinitionEntry>()
            };
        }
    }
}
