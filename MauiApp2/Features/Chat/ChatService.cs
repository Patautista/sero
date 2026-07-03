using Domain.Shared.Models;
using Infrastructure.Data;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MauiApp2.Features.Chat
{
    public class ChatService
    {
        private readonly PetDbContext _db;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            PetDbContext db,
            LocalApiService api,
            ConfigurationService config,
            ILogger<ChatService> logger)
        {
            _db = db;
            _api = api;
            _config = config;
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
                var prompt = BuildCompanionResponsePrompt(context, userMessage.Content, corrections);
                var responseJson = await _api.GenerateTextAsync(prompt, typeof(CompanionResponseSchema));
                var response = ParseCompanionResponse(responseJson);

                var paragraphs = SplitIntoParagraphs(response.Text);
                var results = new List<ChatMessage>(paragraphs.Count);
                var baseTimestamp = DateTime.UtcNow;

                for (int i = 0; i < paragraphs.Count; i++)
                {
                    var companionMessage = new MessageTable
                    {
                        ConversationId = conversationId,
                        SenderType = SenderType.Companion,
                        Content = paragraphs[i],
                        MessageType = MessageType.Normal,
                        Timestamp = baseTimestamp.AddMilliseconds(i * 50),
                        LanguageCode = context.TargetLanguage
                    };
                    _db.Messages.Add(companionMessage);
                    await _db.SaveChangesAsync();

                    results.Add(new ChatMessage
                    {
                        Id = companionMessage.Id,
                        SenderType = SenderType.Companion,
                        Content = paragraphs[i],
                        Timestamp = companionMessage.Timestamp,
                        MessageType = MessageType.Normal
                    });
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
                    Content = response.Text,
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

            return new CompanionResponseContext
            {
                UserName = userProfile?.Name ?? "Friend",
                TargetLanguage = userProfile?.TargetLanguage ?? "es",
                NativeLanguage = userProfile?.NativeLanguage ?? "en",
                Interests = interests,
                RecentMemories = memories,
                CurrentMood = companion.CurrentMood,
                Personality = companion.Personality,
                ConversationHistory = recentMessages
            };
        }

        private string BuildCompanionResponsePrompt(CompanionResponseContext context, string userMessage, List<CorrectionData>? corrections)
        {
            var moodDescription = context.CurrentMood switch
            {
                CompanionMood.Tired => "You're feeling a bit tired, so keep your response gentle and supportive",
                CompanionMood.Curious => "You're feeling curious and excited to learn together",
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

            var prompt = $@"You are Luna, a {context.Personality} language learning companion.

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

INSTRUCTIONS:
1. Respond primarily in {context.TargetLanguage}
2. If mistakes were detected, correct them naturally and gently in your response (e.g., ""I see what you mean! By the way, we usually say X instead of Y."")
3. Reference past conversations or user's interests when relevant
4. Match your current mood: {moodDescription}
5. Keep response conversational, 2-4 sentences
6. Ask a follow-up question to keep the conversation going
7. Be encouraging and supportive";

            return prompt;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        // Splits after ./?/! that is NOT part of an ellipsis (...) and is
        // followed by whitespace + a Unicode uppercase letter.
        private static readonly Regex _sentenceBoundaryRe = new(
            @"(?<=[^.][.!?])\s+(?=\p{Lu})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private CompanionResponseData ParseCompanionResponse(string json)
        {
            try
            {
                var response = JsonSerializer.Deserialize<CompanionResponseData>(json, _jsonOptions);
                return response ?? new CompanionResponseData { Text = json };
            }
            catch
            {
                // If parsing fails, treat entire response as text
                return new CompanionResponseData { Text = json.Trim().Trim('"') };
            }
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
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

            [JsonPropertyName("corrections")]
            public List<CorrectionData>? Corrections { get; set; }
        }

        /// <summary>
        /// Splits an AI response into chunks for progressive message delivery.<br/>
        /// Priority order: \n\n → \n → sentence boundaries (.&nbsp;!&nbsp;?) → single chunk.
        /// Single-word fragments ending in '.' are treated as abbreviations (M., Mme., Dr., …)
        /// and merged into the following sentence to avoid spurious splits.
        /// </summary>
        private static List<string> SplitIntoParagraphs(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [string.Empty];

            static List<string> Trimmed(IEnumerable<string> src) =>
                src.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

            // 1. Explicit paragraph breaks (\n\n)
            var parts = Trimmed(text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries));
            if (parts.Count >= 2) return parts;

            // 2. Single line-breaks
            parts = Trimmed(text.Split(["\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries));
            if (parts.Count >= 2) return parts;

            // 3. Sentence boundaries: after ./?/! (not ellipsis) + space + uppercase
            var sentences = Trimmed(_sentenceBoundaryRe.Split(text.Trim()));
            if (sentences.Count >= 2)
            {
                // Merge single-word abbreviation fragments (M., Mme., Dr., …)
                // into the next sentence to avoid splitting "M. Aznavour" in two.
                var merged = new List<string>(sentences.Count);
                for (int i = 0; i < sentences.Count; i++)
                {
                    var s = sentences[i];
                    if (s.EndsWith('.') && !s.Contains(' ') && i + 1 < sentences.Count)
                        sentences[i + 1] = s + " " + sentences[i + 1];
                    else
                        merged.Add(s);
                }
                if (merged.Count >= 2) return merged;
            }

            return [text.Trim()];
        }
    }
}
