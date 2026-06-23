using Infrastructure.Data;
using MauiApp2.Services;
using Domain.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2.Features.Onboarding
{
    public class OnboardingService
    {
        private readonly PetDbContext _db;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ILogger<OnboardingService> _logger;

        public OnboardingService(
            PetDbContext db,
            LocalApiService api,
            ConfigurationService config,
            ILogger<OnboardingService> logger)
        {
            _db = db;
            _api = api;
            _config = config;
            _logger = logger;
        }

        public async Task<OnboardingResponse> CompleteOnboardingAsync(OnboardingRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting onboarding for user: {request.UserName}");

                // Create user profile
                var userProfile = new UserProfileTable
                {
                    Name = request.UserName,
                    TargetLanguage = request.TargetLanguage,
                    NativeLanguage = request.NativeLanguage,
                    InterestsJson = System.Text.Json.JsonSerializer.Serialize(request.Interests),
                    ActivityPatternsJson = "{}",
                    OnboardedAt = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow
                };

                _db.UserProfiles.Add(userProfile);
                await _db.SaveChangesAsync();

                // Get or create companion
                var companion = await GetOrCreateCompanionAsync();

                // Generate welcome message
                var welcomeMessage = await GenerateWelcomeMessageAsync(userProfile, companion);

                // Create first conversation
                var conversation = new ConversationTable
                {
                    UserProfileId = userProfile.Id,
                    StartedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    IsActive = true
                };

                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();

                // Add welcome message to conversation
                var message = new MessageTable
                {
                    ConversationId = conversation.Id,
                    SenderType = SenderType.Companion,
                    Content = welcomeMessage,
                    MessageType = MessageType.ProactivePrompt,
                    Timestamp = DateTime.UtcNow,
                    LanguageCode = request.TargetLanguage
                };

                _db.Messages.Add(message);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Onboarding completed successfully for user ID: {userProfile.Id}");

                return new OnboardingResponse
                {
                    Success = true,
                    UserProfileId = userProfile.Id,
                    CompanionName = companion.Name,
                    WelcomeMessage = welcomeMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing onboarding");
                return new OnboardingResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred during onboarding. Please try again."
                };
            }
        }

        public async Task<Companion> GetOrCreateCompanionAsync()
        {
            try
            {
                var companionTable = await _db.Companions.FirstOrDefaultAsync();

                if (companionTable == null)
                {
                    _logger.LogWarning("No companion found in database, this should not happen");
                    companionTable = new CompanionTable
                    {
                        Name = "Luna",
                        Personality = "Curious and encouraging, loves learning new things alongside you",
                        CurrentMood = CompanionMood.Curious,
                        LastMoodChange = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Companions.Add(companionTable);
                    await _db.SaveChangesAsync();
                }

                return new Companion
                {
                    Id = companionTable.Id,
                    Name = companionTable.Name,
                    Personality = companionTable.Personality,
                    CurrentMood = companionTable.CurrentMood,
                    LastMoodChange = companionTable.LastMoodChange,
                    CreatedAt = companionTable.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating companion");
                throw;
            }
        }

        private async Task<string> GenerateWelcomeMessageAsync(UserProfileTable userProfile, Companion companion)
        {
            try
            {
                var interestsList = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(userProfile.InterestsJson);
                var interestsText = interestsList != null && interestsList.Any() 
                    ? string.Join(", ", interestsList) 
                    : "various topics";

                var prompt = $@"You are {companion.Name}, a friendly and {companion.Personality} language learning companion.

A new user just joined:
- Name: {userProfile.Name}
- Learning: {userProfile.TargetLanguage}
- Native language: {userProfile.NativeLanguage}
- Interests: {interestsText}
- Your current mood: {companion.CurrentMood}

Write a warm welcome message to {userProfile.Name} in {userProfile.TargetLanguage}. The message should:
1. Introduce yourself briefly
2. Express excitement about helping them learn
3. Mention one of their interests to show you're paying attention
4. End with a simple question to start a conversation
5. Keep it to 3-4 sentences
6. Be appropriate for a {companion.CurrentMood} mood

Return ONLY the welcome message, no JSON, no quotes.";

                var message = await _api.GenerateTextAsync(prompt);
                return message.Trim().Trim('"');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating welcome message, using fallback");
                return $"¡Hola {userProfile.Name}! I'm {companion.Name}, and I'm so excited to help you learn {userProfile.TargetLanguage}! Let's start this journey together. What would you like to talk about today?";
            }
        }

        public async Task<bool> HasCompletedOnboardingAsync()
        {
            try
            {
                return await _db.UserProfiles.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking onboarding status");
                return false;
            }
        }

        public async Task<UserProfile?> GetCurrentUserProfileAsync()
        {
            try
            {
                var userProfileTable = await _db.UserProfiles.FirstOrDefaultAsync();

                if (userProfileTable == null)
                    return null;

                return new UserProfile
                {
                    Id = userProfileTable.Id,
                    Name = userProfileTable.Name,
                    TargetLanguage = userProfileTable.TargetLanguage,
                    NativeLanguage = userProfileTable.NativeLanguage,
                    InterestsJson = userProfileTable.InterestsJson,
                    ActivityPatternsJson = userProfileTable.ActivityPatternsJson,
                    OnboardedAt = userProfileTable.OnboardedAt,
                    LastActiveAt = userProfileTable.LastActiveAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user profile");
                return null;
            }
        }
    }
}
