using Infrastructure.Data.Repositories;
using Infrastructure.Data;
using MauiApp2.Services;
using MauiApp2.Services.AI;
using Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MauiApp2.Features.Onboarding
{
    public class OnboardingService
    {
        private readonly IPetDataStore _store;
        private readonly LocalApiService _api;
        private readonly ConfigurationService _config;
        private readonly ICompanionPromptBuilder _promptBuilder;
        private readonly ILogger<OnboardingService> _logger;

        public OnboardingService(
            IPetDataStore store,
            LocalApiService api,
            ConfigurationService config,
            ICompanionPromptBuilder promptBuilder,
            ILogger<OnboardingService> logger)
        {
            _store = store;
            _api = api;
            _config = config;
            _promptBuilder = promptBuilder;
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
                    SkillsJson = SkillProfile.FromSelfAssessment(request.SkillAssessments).ToJson(),
                    OnboardedAt = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow
                };

                _store.UserProfiles.Add(userProfile);
                await _store.SaveChangesAsync();

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

                _store.Conversations.Add(conversation);
                await _store.SaveChangesAsync();

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

                _store.Messages.Add(message);
                await _store.SaveChangesAsync();

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
                var companionTable = await _store.Companions.FirstOrDefaultAsync(c => true);

                if (companionTable == null)
                {
                    _logger.LogWarning("No companion found in database, this should not happen");
                    companionTable = PetDbContext.DefaultCompanion;
                    _store.Companions.Add(companionTable);
                    await _store.SaveChangesAsync();
                }

                return new Companion
                {
                    Id = companionTable.Id,
                    Name = companionTable.Name,
                    Avatar = companionTable.Avatar,
                    Personality = companionTable.Personality,
                    CurrentMood = companionTable.CurrentMood,
                    LastMoodChange = companionTable.LastMoodChange,
                    EnergyLevel = companionTable.EnergyLevel,
                    LastEnergyUpdate = companionTable.LastEnergyUpdate,
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
                var interestsList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(userProfile.InterestsJson) ?? new List<string>();

                var personaContext = new CompanionPersonaContext
                {
                    CompanionName = companion.Name,
                    Personality = companion.Personality,
                    CurrentMood = companion.CurrentMood,
                    UserName = userProfile.Name,
                    TargetLanguage = userProfile.TargetLanguage,
                    NativeLanguage = userProfile.NativeLanguage,
                    Interests = interestsList,
                    UserSkillProfile = SkillProfile.FromJson(userProfile.SkillsJson)
                };

                var prompt = _promptBuilder.BuildWelcomeMessagePrompt(personaContext);

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
                return await _store.UserProfiles.AnyAsync();
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
                var userProfileTable = await _store.UserProfiles.FirstOrDefaultAsync(p => true);

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
                    SkillsJson = userProfileTable.SkillsJson,
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
