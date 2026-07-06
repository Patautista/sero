using Domain.Shared.Models;
using Infrastructure.Data;
using MauiApp2.Features.Activities;
using MauiApp2.Services;
using MauiApp2.Services.AI.Schemas;
using Domain.Shared.Models;
using Microsoft.EntityFrameworkCore;
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
        private readonly PetDbContext _db;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ActivitySelectionService _activitySelection;
        private readonly ActivityOrchestrator _activityOrchestrator;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            PetDbContext db,
            LocalApiService api,
            ConfigurationService config,
            ActivitySelectionService activitySelection,
            ActivityOrchestrator activityOrchestrator,
            ILogger<ChatService> logger)
        {
            _db = db;
            _api = api;
            _config = config;
            _activitySelection = activitySelection;
            _activityOrchestrator = activityOrchestrator;
            _logger = logger;
        }

        public async Task<ConversationState> GetOrCreateActiveConversationAsync(int userProfileId)
        {
            try
            {
                // Try to find active conversation
                var activeConversation = await _db.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.UserProfileId == userProfileId && c.IsActive);

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

                    _db.Conversations.Add(activeConversation);
                    await _db.SaveChangesAsync();

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
                var messages = await _db.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToListAsync();

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
                var conversation = await _db.Conversations
                    .Include(c => c.UserProfile)
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

                _db.Messages.Add(userMessage);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

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
            var conversation = await _db.Conversations
                .Include(c => c.UserProfile)
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

            _db.Messages.Add(userMessage);
            conversation.LastMessageAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new ChatMessage
            {
                Id = userMessage.Id,
                SenderType = SenderType.User,
                Content = content,
                Timestamp = userMessage.Timestamp,
                MessageType = MessageType.Normal
            };
        }

        public async Task<List<CorrectionData>?> GetCorrectionsAsync(string content, string language)
        {
            if (!_config.EnableMistakeDetection) return null;

            try
            {
                var analysis = await _api.DetectMistakesAsync(content, language);
                if (!analysis.HasMistakes || analysis.Mistakes.Count == 0) return null;

                return analysis.Mistakes.Select(m => new CorrectionData
                {
                    Original = m.Segment,
                    Corrected = m.Corrected,
                    Explanation = $"{m.Type}: {m.Concept}"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking corrections");
                return null;
            }
        }

        public async Task PersistUserMessageCorrectionsAsync(int userMessageId, List<CorrectionData> corrections)
        {
            try
            {
                var message = await _db.Messages.FindAsync(userMessageId);
                if (message != null)
                {
                    message.CorrectionDataJson = JsonSerializer.Serialize(corrections);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting corrections to user message {MessageId}", userMessageId);
            }
        }

        public async Task<List<ChatMessage>> GenerateCompanionMessageAsync(int conversationId, int userMessageId, List<CorrectionData>? corrections)
        {
            try
            {
                var conversation = await _db.Conversations
                    .Include(c => c.UserProfile)
                    .FirstOrDefaultAsync(c => c.Id == conversationId)
                    ?? throw new InvalidOperationException("Conversation not found");

                var userMessage = await _db.Messages.FindAsync(userMessageId)
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
                        return await PersistCompanionBlocksAsync(conversationId, activityResult.Blocks, context.TargetLanguage);
                    }
                    // If the activity could not continue, fall through to normal conversation.
                }

                // 2. Normal companion flow. The companion may decide *when* to start an
                //    activity by returning a startActivityId, but never decides how to run it.
                var prompt = BuildCompanionResponsePrompt(context, userMessage.Content, corrections);
                var responseJson = await _api.GenerateTextAsync(prompt, typeof(CompanionResponseSchema));
                var response = ParseCompanionResponse(responseJson);

                var results = await PersistCompanionBlocksAsync(conversationId, response.Blocks, context.TargetLanguage);

                // 3. If the companion chose to start an activity, hand control to the
                //    Activity Agent and append its in-character introduction to this turn.
                if (!string.IsNullOrWhiteSpace(response.StartActivityId))
                {
                    var startResult = await _activityOrchestrator.StartActivityAsync(
                        conversationId,
                        response.StartActivityId!,
                        BuildActivityAgentContext(context),
                        BuildRecentConversationLines(context));

                    if (startResult is not null)
                    {
                        var introMessages = await PersistCompanionBlocksAsync(
                            conversationId, startResult.Blocks, context.TargetLanguage);
                        results.AddRange(introMessages);
                    }
                }

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
                _db.Messages.Add(fallback);
                await _db.SaveChangesAsync();

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
                _db.Messages.Add(companionMessage);
                await _db.SaveChangesAsync();

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
                var prompt = BuildCompanionResponsePrompt(context, userMessage.Content, corrections);

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

                _db.Messages.Add(companionMessage);
                await _db.SaveChangesAsync();

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

                _db.Messages.Add(fallbackMessage);
                await _db.SaveChangesAsync();

                return fallbackMessage;
            }
        }

        private async Task<CompanionResponseContext> BuildResponseContextAsync(ConversationTable conversation)
        {
            var userProfile = conversation.UserProfile ?? await _db.UserProfiles.FindAsync(conversation.UserProfileId);
            var companion = await _db.Companions.FirstAsync();

            // Load recent memories
            var memories = await _db.ConversationMemories
                .Where(m => m.UserProfileId == conversation.UserProfileId)
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
                .ToListAsync();

            // Load recent conversation history
            var recentMessages = await LoadConversationHistoryAsync(conversation.Id, 10);

            var interests = JsonSerializer.Deserialize<List<string>>(userProfile?.InterestsJson ?? "[]") ?? new List<string>();

            // Activities the learner currently qualifies for. The companion may offer one
            // of these; it never decides how they are run (that is the Activity Agent's job).
            var skills = SkillProfile.FromJson(userProfile?.SkillsJson);
            var eligibleActivities = _activitySelection.GetEligibleActivities(skills).ToList();

            return new CompanionResponseContext
            {
                UserName = userProfile?.Name ?? "Friend",
                TargetLanguage = userProfile?.TargetLanguage ?? "es",
                NativeLanguage = userProfile?.NativeLanguage ?? "en",
                Interests = interests,
                RecentMemories = memories,
                CurrentMood = companion.CurrentMood,
                Personality = companion.Personality,
                CompanionName = companion.Name,
                ConversationHistory = recentMessages,
                EligibleActivities = eligibleActivities
            };
        }

        private string BuildCompanionResponsePrompt(CompanionResponseContext context, string userMessage, List<CorrectionData>? corrections)
        {
            var moodDescription = context.CurrentMood switch
            {
                CompanionMood.Tired => "You're feeling a bit tired, so keep your response gentle and supportive",
                CompanionMood.Curious => "You're feeling curious about the user's interests and recent activities",
                CompanionMood.Nostalgic => "You're in a reflective, nostalgic mood, thinking about past conversations",
                CompanionMood.Excited => "You're feeling energetic and enthusiastic",
                _ => "You're in a balanced, supportive mood"
            };

            var historyText = string.Join("\n", context.ConversationHistory.TakeLast(5).Select(m => 
                $"{m.SenderType}: {m.Content}"));

            var memoriesText = context.RecentMemories.Any()
                ? string.Join("\n", context.RecentMemories.Select(m => $"- {m.Content}"))
                : "No specific memories yet";

            var correctionsText = corrections != null && corrections.Any()
                ? "Mistakes detected:\n" + string.Join("\n", corrections.Select(c => 
                    $"- '{c.Original}' → '{c.Corrected}' ({c.Explanation})"))
                : "No mistakes detected";

            var activitiesText = context.EligibleActivities.Any()
                ? string.Join("\n", context.EligibleActivities.Select(a =>
                    $"- id: {a.Id} | {a.Name}: {a.Objective}"))
                : "No activities are available right now";

            var prompt = $@"You are {context.CompanionName}, a {context.Personality} language learning companion.

USER CONTEXT:
- Name: {context.UserName}
- Learning: {context.TargetLanguage}
- Native language: {context.NativeLanguage}
- Interests: {string.Join(", ", context.Interests)}

YOUR STATE:
- Current mood: {context.CurrentMood}
- {moodDescription}

RECENT MEMORIES:
{memoriesText}

RECENT CONVERSATION:
{historyText}

USER'S NEW MESSAGE:
""{userMessage}""

LANGUAGE ANALYSIS:
{correctionsText}

LEARNING ACTIVITIES YOU CAN OFFER:
{activitiesText}

INSTRUCTIONS:
1. Respond primarily in {context.TargetLanguage}
2. Your main goal is to help the user practice and improve their language skills. So keep trying to introduce the user to the target language-specific topics such as grammar and vocabulary relative to their level. 
2. Reference past conversations or user's interests when relevant
3. Match your current mood: {moodDescription}
4. Split your reply into 1–3 short paragraphs (each 1–2 sentences). Put each paragraph as a separate entry in the ""blocks"" array
5. Ask a follow-up question to keep the conversation going
6. You may use **bold** for emphasis and *italics* for foreign words, titles, or gentle stress — use them sparingly to feel natural
7. If — and only if — this is a natural moment to practise, you may gently offer ONE of the activities listed above. To start it, set ""startActivityId"" to that activity's id and briefly invite the user in your ""blocks"". Otherwise leave ""startActivityId"" empty. Never describe how the activity works or run it yourself; simply offer it and a dedicated guide will take over.";

            return prompt;
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
