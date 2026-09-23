using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using MauiApp2.Services;
using MauiApp2.Services.AI.Schemas;
using Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MauiApp2.Features.Memory
{
    public class MemoryService
    {
        private readonly IPetDataStore _store;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ILogger<MemoryService> _logger;

        public MemoryService(
            IPetDataStore store,
            LocalApiService api,
            ConfigurationService config,
            ILogger<MemoryService> logger)
        {
            _store = store;
            _api = api;
            _config = config;
            _logger = logger;
        }

        public async Task<MemoryExtractionResult> ExtractMemoriesFromConversationAsync(int conversationId, int userProfileId)
        {
            try
            {
                if (!_config.MemoryExtractionEnabled)
                {
                    return new MemoryExtractionResult { Success = true, MemoriesCreated = 0 };
                }

                _logger.LogInformation($"Extracting memories from conversation {conversationId}");

                // Get recent messages
                var allMessages = await _store.Messages.WhereAsync(m => m.ConversationId == conversationId);
                var messages = allMessages
                    .OrderByDescending(m => m.Timestamp)
                    .Take(20)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ConversationMessage
                    {
                        Sender = m.SenderType == SenderType.User ? "User" : "Companion",
                        Content = m.Content,
                        Timestamp = m.Timestamp
                    })
                    .ToList();

                if (!messages.Any())
                {
                    return new MemoryExtractionResult { Success = true, MemoriesCreated = 0 };
                }

                // Build conversation text
                var conversationText = string.Join("\n", messages.Select(m => $"{m.Sender}: {m.Content}"));

                // Extract memories using AI
                var prompt = $@"Analyze this conversation and extract important facts about the user that should be remembered for future conversations.

Conversation:
{conversationText}

Guidelines:
- Only extract facts explicitly mentioned by the user
- Be specific and concrete
- Combine related facts into single memories
- Importance 4-5: Core interests, significant events, important goals
- Importance 2-3: Casual mentions, minor events
- Importance 1: Very minor details
- type must be one of: Interest, Event, Preference, Goal";

                var response = await _api.GenerateTextAsync(prompt, typeof(MemoryExtractionSchema));
                var extractionData = JsonSerializer.Deserialize<MemoryExtractionData>(response);

                if (extractionData?.Memories == null || !extractionData.Memories.Any())
                {
                    _logger.LogInformation("No new memories extracted");
                    return new MemoryExtractionResult { Success = true, MemoriesCreated = 0 };
                }

                // Save memories to database
                var memoriesCreated = 0;
                foreach (var memory in extractionData.Memories)
                {
                    // Check for similar existing memories to avoid duplicates
                    var existingMemory = await _store.ConversationMemories
                        .FirstOrDefaultAsync(m => 
                            m.UserProfileId == userProfileId &&
                            m.FactType == memory.FactType &&
                            m.Content.ToLower().Contains(memory.Content.ToLower().Substring(0, Math.Min(20, memory.Content.Length))));

                    if (existingMemory != null)
                    {
                        // Update existing memory
                        existingMemory.Content = memory.Content;
                        existingMemory.Importance = Math.Max(existingMemory.Importance, memory.Importance);
                        existingMemory.LastReferencedAt = DateTime.UtcNow;
                        _logger.LogInformation($"Updated existing memory: {memory.Content}");
                    }
                    else
                    {
                        // Create new memory
                        var newMemory = new ConversationMemoryTable
                        {
                            UserProfileId = userProfileId,
                            FactType = memory.FactType,
                            Content = memory.Content,
                            Importance = memory.Importance,
                            CreatedAt = DateTime.UtcNow,
                            LastReferencedAt = DateTime.UtcNow
                        };

                        _store.ConversationMemories.Add(newMemory);
                        memoriesCreated++;
                        _logger.LogInformation($"Created new memory: {memory.Content}");
                    }
                }

                await _store.SaveChangesAsync();

                return new MemoryExtractionResult
                {
                    Success = true,
                    Memories = extractionData.Memories,
                    MemoriesCreated = memoriesCreated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting memories");
                return new MemoryExtractionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to extract memories"
                };
            }
        }

        public async Task<List<ConversationMemory>> GetRelevantMemoriesAsync(int userProfileId, int limit = 5)
        {
            try
            {
                var allMemories = await _store.ConversationMemories
                    .WhereAsync(m => m.UserProfileId == userProfileId && m.Importance >= _config.MinImportanceThreshold);
                var memories = allMemories
                    .OrderByDescending(m => m.Importance)
                    .ThenByDescending(m => m.LastReferencedAt)
                    .Take(limit)
                    .Select(m => new ConversationMemory
                    {
                        Id = m.Id,
                        UserProfileId = m.UserProfileId,
                        FactType = m.FactType,
                        Content = m.Content,
                        Importance = m.Importance,
                        LastReferencedAt = m.LastReferencedAt,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList();

                return memories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting relevant memories");
                return new List<ConversationMemory>();
            }
        }

        public async Task UpdateMemoryRelevanceAsync(int memoryId)
        {
            try
            {
                var memory = await _store.ConversationMemories.FindByIdAsync(memoryId);
                if (memory != null)
                {
                    memory.LastReferencedAt = DateTime.UtcNow;
                    await _store.SaveChangesAsync();
                    _logger.LogInformation($"Updated memory {memoryId} relevance");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating memory {memoryId} relevance");
            }
        }

        public async Task<int> AddManualMemoryAsync(int userProfileId, string factType, string content, int importance)
        {
            try
            {
                var memory = new ConversationMemoryTable
                {
                    UserProfileId = userProfileId,
                    FactType = factType,
                    Content = content,
                    Importance = importance,
                    CreatedAt = DateTime.UtcNow,
                    LastReferencedAt = DateTime.UtcNow
                };

                _store.ConversationMemories.Add(memory);
                await _store.SaveChangesAsync();

                _logger.LogInformation($"Added manual memory for user {userProfileId}");
                return memory.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding manual memory");
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetMemoryStatisticsAsync(int userProfileId)
        {
            try
            {
                var allMemories = await _store.ConversationMemories
                    .WhereAsync(m => m.UserProfileId == userProfileId);
                var stats = allMemories
                    .GroupBy(m => m.FactType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.Type, x => x.Count);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory statistics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<bool> DeleteMemoryAsync(int memoryId)
        {
            try
            {
                var memory = await _store.ConversationMemories.FindByIdAsync(memoryId);
                if (memory != null)
                {
                    _store.ConversationMemories.Remove(memory);
                    await _store.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting memory {memoryId}");
                return false;
            }
        }

        private class MemoryExtractionData
        {
            [JsonPropertyName("memories")]
            public List<ExtractedMemory> Memories { get; set; } = new();
        }
    }
}
