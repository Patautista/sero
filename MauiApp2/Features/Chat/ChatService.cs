using Domain.Shared.Models;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using MauiApp2.Features.Activities;
using MauiApp2.Features.LanguageCoaching;
using MauiApp2.Features.MentalModels;
using MauiApp2.Features.Skills;
using MauiApp2.Services;
using MauiApp2.Services.AI;
using MauiApp2.Services.AI.Schemas;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MauiApp2.Features.Chat
{
    public class ChatService
    {
        private readonly IPetDataStore _store;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ActivitySelectionService _activitySelection;
        private readonly ActivityOrchestrator _activityOrchestrator;
        private readonly ICompanionPromptBuilder _promptBuilder;
        private readonly LanguageDetectionService _languageDetection;
        private readonly LanguageCoachingService _languageCoaching;
        private readonly ConversationEngine _conversationEngine;
        private readonly ISkillAreaCatalogProvider _skillAreaCatalogProvider;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IPetDataStore store,
            LocalApiService api,
            ConfigurationService config,
            ActivitySelectionService activitySelection,
            ActivityOrchestrator activityOrchestrator,
            ICompanionPromptBuilder promptBuilder,
            LanguageDetectionService languageDetection,
            LanguageCoachingService languageCoaching,
            ConversationEngine conversationEngine,
            ISkillAreaCatalogProvider skillAreaCatalogProvider,
            ILogger<ChatService> logger)
        {
            _store = store;
            _api = api;
            _config = config;
            _activitySelection = activitySelection;
            _activityOrchestrator = activityOrchestrator;
            _promptBuilder = promptBuilder;
            _languageDetection = languageDetection;
            _languageCoaching = languageCoaching;
            _conversationEngine = conversationEngine;
            _skillAreaCatalogProvider = skillAreaCatalogProvider;
            _logger = logger;
        }

        public async Task<ConversationState> GetOrCreateActiveConversationAsync(int userProfileId)
        {
            try
            {
                // Try to find active conversation
                var activeConversation = await _store.Conversations
                    .FirstOrDefaultAsync(c => c.UserProfileId == userProfileId && c.IsActive);
                // Note: Include() not needed with LiteDB; load messages separately if needed

                if (activeConversation == null)
                {
                    // Create new conversation
                    activeConversation = new ConversationTable
                    {
                        UserProfileId = userProfileId,
                        StartedAt = DateTime.UtcNow,
                        LastMessageAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _store.Conversations.Add(activeConversation);
                    await _store.SaveChangesAsync();

                    _logger.LogInformation($"Created new conversation {activeConversation.Id} for user {userProfileId}");
                }

                var messages = await LoadConversationHistoryAsync(activeConversation.Id, _config.ConversationHistoryLimit);

                return new ConversationState
                {
                    ConversationId = activeConversation.Id,
                    Messages = messages,
                    IsActive = activeConversation.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating conversation");
                throw;
            }
        }

        public async Task<List<ChatMessage>> LoadConversationHistoryAsync(int conversationId, int limit = 50)
        {
            try
            {
                var allMessages = await _store.Messages.WhereAsync(m => m.ConversationId == conversationId);
                var messages = allMessages
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToList();

                return messages
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ChatMessage
                    {
                        Id = m.Id,
                        SenderType = m.SenderType,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        MessageType = m.MessageType,
                        Corrections = ParseCorrections(m.CorrectionDataJson)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation history");
                return new List<ChatMessage>();
            }
        }

        public async Task<SendMessageResponse> SendUserMessageAsync(int conversationId, string content)
        {
            try
            {
                _logger.LogInformation($"User sending message in conversation {conversationId}");

                // Get conversation and user profile
                var conversation = await _store.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    return new SendMessageResponse
                    {
                        Success = false,
                        ErrorMessage = "Conversation not found"
                    };
                }

                // Create user message
                var userMessage = new MessageTable
                {
                    ConversationId = conversationId,
                    SenderType = SenderType.User,
                    Content = content,
                    MessageType = MessageType.Normal,
                    Timestamp = DateTime.UtcNow,
                    LanguageCode = conversation.UserProfile?.TargetLanguage ?? "es"
                };

                _store.Messages.Add(userMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _store.SaveChangesAsync();

                // Generate companion response
                var companionResponse = await GenerateCompanionResponseAsync(conversation, userMessage);

                var userChatMessage = new ChatMessage
                {
                    Id = userMessage.Id,
                    SenderType = userMessage.SenderType,
                    Content = userMessage.Content,
                    Timestamp = userMessage.Timestamp,
                    MessageType = userMessage.MessageType
                };

                var companionChatMessage = new ChatMessage
                {
                    Id = companionResponse.Id,
                    SenderType = companionResponse.SenderType,
                    Content = companionResponse.Content,
                    Timestamp = companionResponse.Timestamp,
                    MessageType = companionResponse.MessageType,
                    Corrections = ParseCorrections(companionResponse.CorrectionDataJson)
                };

                return new SendMessageResponse
                {
                    Success = true,
                    UserMessage = userChatMessage,
                    CompanionResponse = companionChatMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return new SendMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to send message"
                };
            }
        }

        public async Task<ChatMessage> SaveUserMessageAsync(int conversationId, string content)
        {
            var conversation = await _store.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId)
                ?? throw new InvalidOperationException("Conversation not found");

            var userMessage = new MessageTable
            {
                ConversationId = conversationId,
                SenderType = SenderType.User,
                Content = content,
                MessageType = MessageType.Normal,
                Timestamp = DateTime.UtcNow,
                LanguageCode = conversation.UserProfile?.TargetLanguage ?? "es"
            };

            _store.Messages.Add(userMessage);
            conversation.LastMessageAt = DateTime.UtcNow;
            await _store.SaveChangesAsync();

            return new ChatMessage
            {
                Id = userMessage.Id,
                SenderType = SenderType.User,
                Content = content,
                Timestamp = userMessage.Timestamp,
                MessageType = MessageType.Normal
            };
        }

        public async Task<CorrectionsResponse> GetCorrectionsAsync(int conversationId, string content, string targetLanguage, string nativeLanguage)
        {
            var response = new CorrectionsResponse();

            if (!_config.EnableMistakeDetection) return response;

            try
            {
                // Outside of an activity, a message deliberately written in the learner's
                // native language (e.g. asking a question) isn't a mistake and shouldn't be
                // corrected. Activities always evaluate the learner's response regardless of
                // language, since that evaluation is the point of the activity.
                if (!_activityOrchestrator.IsActivityActive(conversationId)
                    && _languageDetection.IsNativeLanguage(content, targetLanguage, nativeLanguage))
                {
                    return response;
                }

                var analysis = await _api.DetectMistakesAsync(content, targetLanguage);
                if (!analysis.HasMistakes || analysis.Mistakes.Count == 0) return response;

                response.Corrections = analysis.Mistakes.Select(m => new CorrectionData
                {
                    Original = m.Segment,
                    Corrected = m.Corrected,
                    Explanation = $"{m.Type}: {m.Concept}"
                }).ToList();

                // TODO: Get newly mastered concepts from language coaching service
                response.NewlyMasteredConcepts = new();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking corrections");
                return response;
            }
        }

        public async Task PersistUserMessageCorrectionsAsync(int userMessageId, List<CorrectionData> corrections)
        {
            try
            {
                var message = await _store.Messages.FindByIdAsync(userMessageId);
                if (message != null)
                {
                    message.CorrectionDataJson = JsonSerializer.Serialize(corrections);
                    await _store.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting corrections to user message {MessageId}", userMessageId);
            }
        }

        public async Task<List<ChatMessage>> GenerateCompanionMessageAsync(int conversationId, int userMessageId, List<CorrectionData>? corrections, List<string>? newlyMasteredConcepts = null)
        {
            try
            {
                var conversation = await _store.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId)
                    ?? throw new InvalidOperationException("Conversation not found");

                var userMessage = await _store.Messages.FindByIdAsync(userMessageId)
                    ?? throw new InvalidOperationException("User message not found");

                var context = await BuildResponseContextAsync(conversation);

                // 1. If an activity is already running, the Activity Agent owns this turn.
                //    The conversation engine simply forwards the learner's message and
                //    shows whatever the agent produced — it never runs the activity itself.
                if (_activityOrchestrator.IsActivityActive(conversationId))
                {
                    var activityResult = await _activityOrchestrator.ContinueActivityAsync(
                        conversationId,
                        BuildActivityAgentContext(context),
                        userMessage.Content,
                        BuildRecentConversationLines(context));

                    if (activityResult is not null)
                    {
                        return await PersistActivityTurnResultAsync(conversationId, activityResult, context.TargetLanguage);
                    }
                    // If the activity could not continue, fall through to normal conversation.
                }

                // 2. Normal companion flow. The companion may decide *when* to start an
                //    activity by returning a startActivityId, but never decides how to run it.
                var prompt = _promptBuilder.BuildCompanionResponsePrompt(context, userMessage.Content, corrections);
                var responseJson = await _api.GenerateTextAsync(prompt, typeof(CompanionResponseSchema));
                var response = ParseCompanionResponse(responseJson);

                // 3. If the companion chose to start an activity, hand this whole turn to the
                //    Activity Agent's introduction instead of ALSO showing the companion's own
                //    reply. The Activity Agent's introduction already answers in character and
                //    launches the activity, so showing both would mean two replies for a single
                //    learner message.
                if (!string.IsNullOrWhiteSpace(response.StartActivityId))
                {
                    var startResult = await _activityOrchestrator.StartActivityAsync(
                        conversationId,
                        response.StartActivityId!,
                        BuildActivityAgentContext(context),
                        BuildRecentConversationLines(context));

                    if (startResult is not null)
                    {
                        var introResults = await PersistActivityTurnResultAsync(conversationId, startResult, context.TargetLanguage);
                        _logger.LogInformation(
                            "Started activity '{ActivityId}' for conversation {ConversationId}", response.StartActivityId, conversationId);
                        return introResults;
                    }

                    _logger.LogInformation(
                        "Companion requested activity '{ActivityId}' but it could not be started; using the companion's own reply instead.",
                        response.StartActivityId);
                }

                var results = await PersistCompanionBlocksAsync(conversationId, response.Blocks, context.TargetLanguage);

                _logger.LogInformation("Generated {Count} companion message(s) for conversation {ConversationId}", results.Count, conversationId);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating companion message");

                var fallback = new MessageTable
                {
                    ConversationId = conversationId,
                    SenderType = SenderType.Companion,
                    Content = "I'm having trouble thinking right now. Can you try again?",
                    MessageType = MessageType.Normal,
                    Timestamp = DateTime.UtcNow,
                    LanguageCode = "es"
                };
                _store.Messages.Add(fallback);
                await _store.SaveChangesAsync();

                return
                [
                    new ChatMessage
                    {
                        Id = fallback.Id,
                        SenderType = SenderType.Companion,
                        Content = fallback.Content,
                        Timestamp = fallback.Timestamp,
                        MessageType = MessageType.Normal
                    }
                ];
            }
        }

        /// <summary>
        /// Persists an Activity Agent turn: its in-character blocks (as normal companion
        /// bubbles), then any generated passage/prompt as its own highlighted message,
        /// then — when the turn completed the activity with a skill change — a trailing
        /// notification message informing the learner what their skills became.
        /// </summary>
        private async Task<List<ChatMessage>> PersistActivityTurnResultAsync(
            int conversationId, ActivityTurnResult activityResult, string languageCode)
        {
            var results = await PersistCompanionBlocksAsync(conversationId, activityResult.Blocks, languageCode);

            if (!string.IsNullOrWhiteSpace(activityResult.GeneratedContent))
            {
                results.Add(await PersistActivityPromptMessageAsync(conversationId, activityResult.GeneratedContent, languageCode));
            }

            if (!string.IsNullOrWhiteSpace(activityResult.SkillUpdateSummary))
            {
                results.Add(await PersistSkillUpdateMessageAsync(conversationId, activityResult.SkillUpdateSummary, languageCode));
            }

            return results;
        }

        /// <summary>
        /// Persists the activity's generated passage, exercise text or prompt as its own
        /// message, tagged with <see cref="MessageType.ActivityPrompt"/> so the UI can
        /// highlight it as the specific material the learner needs to engage with,
        /// distinct from the companion's in-character chit-chat.
        /// </summary>
        private async Task<ChatMessage> PersistActivityPromptMessageAsync(int conversationId, string content, string languageCode)
        {
            var message = new MessageTable
            {
                ConversationId = conversationId,
                SenderType = SenderType.Companion,
                Content = content,
                MessageType = MessageType.ActivityPrompt,
                Timestamp = DateTime.UtcNow,
                LanguageCode = languageCode
            };
            _store.Messages.Add(message);
            await _store.SaveChangesAsync();

            return new ChatMessage
            {
                Id = message.Id,
                SenderType = SenderType.Companion,
                Content = content,
                Timestamp = message.Timestamp,
                MessageType = MessageType.ActivityPrompt
            };
        }

        /// <summary>
        /// Persists a system-style notification informing the learner that their skill
        /// scores were reevaluated after completing an activity, tagged with
        /// <see cref="MessageType.SkillUpdate"/> so the UI can render it distinctly from
        /// normal in-character companion dialogue.
        /// </summary>
        private async Task<ChatMessage> PersistSkillUpdateMessageAsync(int conversationId, string summary, string languageCode)
        {
            var message = new MessageTable
            {
                ConversationId = conversationId,
                SenderType = SenderType.Companion,
                Content = summary,
                MessageType = MessageType.SkillUpdate,
                Timestamp = DateTime.UtcNow,
                LanguageCode = languageCode
            };
            _store.Messages.Add(message);
            await _store.SaveChangesAsync();

            return new ChatMessage
            {
                Id = message.Id,
                SenderType = SenderType.Companion,
                Content = summary,
                Timestamp = message.Timestamp,
                MessageType = MessageType.SkillUpdate
            };
        }

        /// <summary>
        /// Persists a set of companion message blocks (whether authored by the companion
        /// or produced by the Activity Agent) and returns them as <see cref="ChatMessage"/>
        /// for the UI. Both paths surface through the same companion bubbles, so an active
        /// activity feels like a natural part of the conversation.
        /// </summary>
        private async Task<List<ChatMessage>> PersistCompanionBlocksAsync(
            int conversationId, IReadOnlyList<string> blocks, string languageCode)
        {
            var results = new List<ChatMessage>(blocks.Count);
            var baseTimestamp = DateTime.UtcNow;

            for (int i = 0; i < blocks.Count; i++)
            {
                var companionMessage = new MessageTable
                {
                    ConversationId = conversationId,
                    SenderType = SenderType.Companion,
                    Content = blocks[i],
                    MessageType = MessageType.Normal,
                    Timestamp = baseTimestamp.AddMilliseconds(i * 50),
                    LanguageCode = languageCode
                };
                _store.Messages.Add(companionMessage);
                await _store.SaveChangesAsync();

                results.Add(new ChatMessage
                {
                    Id = companionMessage.Id,
                    SenderType = SenderType.Companion,
                    Content = blocks[i],
                    Timestamp = companionMessage.Timestamp,
                    MessageType = MessageType.Normal
                });
            }

            return results;
        }

        /// <summary>
        /// Projects the companion's conversation context into the in-character context the
        /// Activity Agent needs, so the activity stays consistent with the companion's
        /// persona without coupling the agent to the conversation engine's internals.
        /// </summary>
        private static ActivityAgentContext BuildActivityAgentContext(CompanionResponseContext context) => new()
        {
            CompanionName = context.CompanionName,
            Personality = context.Personality,
            UserName = context.UserName,
            TargetLanguage = context.TargetLanguage,
            NativeLanguage = context.NativeLanguage
        };

        /// <summary>Recent conversation lines given to the Activity Agent for tone and continuity.</summary>
        private static IReadOnlyList<string> BuildRecentConversationLines(CompanionResponseContext context) =>
            context.ConversationHistory
                .TakeLast(6)
                .Select(m => $"{m.SenderType}: {m.Content}")
                .ToList();

        private async Task<MessageTable> GenerateCompanionResponseAsync(ConversationTable conversation, MessageTable userMessage)
        {
            try
            {
                // Load context
                var context = await BuildResponseContextAsync(conversation);

                // Analyze user message for mistakes
                List<CorrectionData>? corrections = null;
                if (_config.EnableMistakeDetection)
                {
                    var mistakeAnalysis = await _api.DetectMistakesAsync(
                        userMessage.Content, 
                        context.TargetLanguage);

                    if (mistakeAnalysis.HasMistakes)
                    {
                        corrections = mistakeAnalysis.Mistakes.Select(m => new CorrectionData
                        {
                            Original = m.Segment,
                            Corrected = m.Corrected,
                            Explanation = $"{m.Type}: {m.Concept}"
                        }).ToList();

                        _logger.LogInformation($"Detected {corrections.Count} mistakes in user message");
                    }
                }

                // Build AI prompt
                var prompt = _promptBuilder.BuildCompanionResponsePrompt(context, userMessage.Content, corrections);

                // Generate response
                var responseJson = await _api.GenerateTextAsync(prompt, typeof(CompanionResponseSchema));
                var response = ParseCompanionResponse(responseJson);

                // Create companion message
                var companionMessage = new MessageTable
                {
                    ConversationId = conversation.Id,
                    SenderType = SenderType.Companion,
                    Content = string.Join("\n\n", response.Blocks),
                    MessageType = corrections != null && corrections.Any() ? MessageType.Correction : MessageType.Normal,
                    Timestamp = DateTime.UtcNow,
                    LanguageCode = context.TargetLanguage,
                    CorrectionDataJson = corrections != null ? JsonSerializer.Serialize(corrections) : null
                };

                _store.Messages.Add(companionMessage);
                await _store.SaveChangesAsync();

                _logger.LogInformation($"Generated companion response for conversation {conversation.Id}");

                return companionMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating companion response, using fallback");

                // Fallback response
                var fallbackMessage = new MessageTable
                {
                    ConversationId = conversation.Id,
                    SenderType = SenderType.Companion,
                    Content = "I'm having trouble thinking right now. Can you try asking again?",
                    MessageType = MessageType.Normal,
                    Timestamp = DateTime.UtcNow,
                    LanguageCode = conversation.UserProfile?.TargetLanguage ?? "es"
                };

                _store.Messages.Add(fallbackMessage);
                await _store.SaveChangesAsync();

                return fallbackMessage;
            }
        }

        private async Task<CompanionResponseContext> BuildResponseContextAsync(ConversationTable conversation)
        {
            var userProfile = conversation.UserProfile ?? await _store.UserProfiles.FindByIdAsync(conversation.UserProfileId);
            var companion = await _store.Companions.FirstOrDefaultAsync(m => true) ?? PetDbContext.DefaultCompanion;

            // Load recent memories
            var allMemories = await _store.ConversationMemories
                .WhereAsync(m => m.UserProfileId == conversation.UserProfileId);

            var memories = allMemories
                .OrderByDescending(m => m.LastReferencedAt)
                .ThenByDescending(m => m.Importance)
                .Take(_config.MaxMemoriesPerRetrieval)
                .Select(m => new ConversationMemory
                {
                    Id = m.Id,
                    UserProfileId = m.UserProfileId,
                    FactType = m.FactType,
                    Content = m.Content,
                    LastReferencedAt = m.LastReferencedAt,
                    Importance = m.Importance,
                    CreatedAt = m.CreatedAt
                })
                .ToList();

            // Load recent conversation history
            var recentMessages = await LoadConversationHistoryAsync(conversation.Id, 12);

            var interests = JsonSerializer.Deserialize<List<string>>(userProfile?.InterestsJson ?? "[]") ?? new List<string>();
            var catalog = await _skillAreaCatalogProvider.GetCatalogAsync();

            // Activities the learner currently qualifies for. The companion may offer one
            // of these; it never decides how they are run (that is the Activity Agent's job).
            var skills = userProfile?.Skills ?? new SkillProfile();
            var areaProgress = userProfile?.AreaProgress ?? new AreaProgress();
            var eligibleActivities = _activitySelection.GetEligibleActivities(skills, areaProgress, catalog).ToList();

            var context = new CompanionResponseContext
            {
                UserName = userProfile?.Name ?? "Friend",
                TargetLanguage = userProfile?.TargetLanguage ?? "es",
                NativeLanguage = userProfile?.NativeLanguage ?? string.Empty,
                Interests = interests,
                RecentMemories = memories,
                CurrentMood = companion.CurrentMood,
                Personality = companion.Personality,
                CompanionName = companion.Name,
                ConversationHistory = recentMessages,
                EligibleActivities = eligibleActivities,
                UserSkillProfile = skills,
                UserAreaProgress = areaProgress,
                SkillAreaCatalog = catalog
            };

            context.Reasoning = await BuildReasoningAsync(conversation, userProfile, companion, context);
            return context;
        }

        private async Task<MentalModelReasoning?> BuildReasoningAsync(
            ConversationTable conversation,
            UserProfileTable? userProfile,
            CompanionTable companion,
            CompanionResponseContext context)
        {
            try
            {
                var latestUserMessage = context.ConversationHistory
                    .LastOrDefault(m => m.SenderType == SenderType.User)?.Content;
                var activeChallenges = await _languageCoaching.GetActiveChallengesAsync(conversation.UserProfileId);

                if (context.SkillAreaCatalog is null)
                    return null;

                var request = new MentalModelRequest
                {
                    UserProfileId = conversation.UserProfileId,
                    ConversationId = conversation.Id,
                    UserName = context.UserName,
                    TargetLanguage = context.TargetLanguage,
                    NativeLanguage = context.NativeLanguage,
                    OnboardedAt = userProfile?.OnboardedAt ?? DateTime.UtcNow,
                    Interests = context.Interests,
                    Skills = context.UserSkillProfile,
                    AreaProgress = context.UserAreaProgress,
                    SkillAreaCatalog = context.SkillAreaCatalog,
                    Companion = new CompanionSnapshot(companion.Name, companion.Personality, companion.CurrentMood),
                    History = context.ConversationHistory,
                    RecentMemories = context.RecentMemories,
                    EligibleActivities = context.EligibleActivities,
                    ActiveChallenges = activeChallenges,
                    LatestUserMessage = latestUserMessage
                };

                return await _conversationEngine.ReasonAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build mental-model reasoning for conversation {ConversationId}", conversation.Id);
                return null;
            }
        }

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private CompanionResponseData ParseCompanionResponse(string json)
        {
            try
            {
                var response = JsonSerializer.Deserialize<CompanionResponseData>(json, _jsonOptions);
                if (response?.Blocks is { Count: > 0 })
                    return response;
            }
            catch { /* ignore, try fallbacks below */ }

            // Graceful fallback: handle legacy "text" field
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("text", out var textEl))
                    return new CompanionResponseData { Blocks = [textEl.GetString() ?? string.Empty] };
            }
            catch { /* ignore */ }

            // Last resort: treat entire response as a single block
            return new CompanionResponseData { Blocks = [json.Trim().Trim('"')] };
        }

        private List<CorrectionData>? ParseCorrections(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<CorrectionData>>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]> GetMessageAudioAsync(string text, string language)
        {
            try
            {
                return await _api.GetTTSAsync(text, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message audio");
                return Array.Empty<byte>();
            }
        }

        private class CompanionResponseData
        {
            [JsonPropertyName("blocks")]
            public List<string> Blocks { get; set; } = new();

            [JsonPropertyName("corrections")]
            public List<CorrectionData>? Corrections { get; set; }

            [JsonPropertyName("startActivityId")]
            public string? StartActivityId { get; set; }
        }

            }
        }

