using Business.Audio;
using CommunityToolkit.Maui;
using Google.Cloud.TextToSpeech.V1;
using Infrastructure.Audio;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using LiteDB;
using MauiApp1.Services.Cache;
using MauiApp2.Services.AI;
using MauiApp2.Services.Audio;
using Microsoft.Extensions.AI;
using MauiApp2.Features.Activities;
using MauiApp2.Features.Chat;
using MauiApp2.Features.LanguageCoaching;
using MauiApp2.Features.Memory;
using MauiApp2.Features.MentalModels;
using MauiApp2.Features.MentalModels.Models;
using MauiApp2.Features.MentalModels.Strategy;
using MauiApp2.Features.Onboarding;
using MauiApp2.Features.ProactiveMessages;
using MauiApp2.Features.QuickActions;
using MauiApp2.Features.Skills;
using MauiApp2.Services;
using Microsoft.Extensions.Logging;
using Radzen;

namespace MauiApp2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("PressStart2P-Regular.ttf", "PressStart2P");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddRadzenComponents();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Configuration
            var config = new ConfigurationService();
            builder.Services.AddSingleton(config);

            // Database
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, Path.ChangeExtension(config.DatabaseFileName, ".litedb"));
            builder.Services.AddSingleton<ILiteDatabase>(_ =>
            {
                UserProfileTable.ConfigureBsonMapper(BsonMapper.Global);
                return new LiteDatabase(dbPath);
            });
            builder.Services.AddScoped<PetDbContext>();
            builder.Services.AddScoped<PetDbContextInitialiser>();
            builder.Services.AddScoped<IPetDataStore, LiteDbPetDataStore>();

            // AI Services — backed by Gemini via Microsoft.Extensions.AI IChatClient
            // Passing an empty key preserves the existing unconfigured-key behaviour (fails at the first call).
            builder.Services.AddSingleton<IChatClient>(
                _ => new GeminiChatClient(config.IsConfigured() ? config.GeminiApiKey : string.Empty));
            builder.Services.AddScoped<AiDefinitionCache>();

            // Core Services
            builder.Services.AddSingleton<LocalApiService>();
            builder.Services.AddSingleton<IApiService>(sp => sp.GetRequiredService<LocalApiService>());
            builder.Services.AddSingleton<Services.NotificationService>();
            builder.Services.AddSingleton<ICompanionPromptBuilder, CompanionPromptBuilder>();
            builder.Services.AddSingleton<LanguageDetectionService>();

            // Voice / TTS — powers audio-only companion messages (e.g. listening activities).
            // Registered defensively: if Google Cloud credentials aren't available on this
            // device, ISpeechService is simply left unregistered and LocalApiService.GetTTSAsync
            // falls back to returning empty audio instead of crashing the app at startup.
            builder.Services.AddScoped<IAudioCache>(_ => new MobileAudioCache());
            if (config.EnableVoiceFeatures)
            {
                try
                {
                    var ttsClient = TextToSpeechClient.Create();
                    builder.Services.AddSingleton(ttsClient);
                    builder.Services.AddScoped<ISpeechService, GoogleSpeechService>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Voice features disabled: could not initialise Google TTS client ({ex.Message}).");
                }
            }
            builder.Services.AddScoped<MauiSoundService>();

            // Feature Services
            builder.Services.AddScoped<OnboardingService>();
            builder.Services.AddScoped<QuickActionsService>();
            builder.Services.AddScoped<ChatService>();
            builder.Services.AddScoped<LanguageCoachingService>();
            builder.Services.AddScoped<MemoryService>();
            builder.Services.AddScoped<TimingLearningService>();
            builder.Services.AddScoped<ActivitySelectionService>();
            builder.Services.AddSingleton<ISkillAreaCatalogProvider, SkillAreaCatalogProvider>();

            // Activity Agent architecture — the dedicated agent that conducts and evaluates
            // learning activities, decoupled from the conversation engine. The instance store
            // is a singleton so an in-flight activity survives across scoped ChatService uses.
            builder.Services.AddSingleton<IActivityInstanceStore, InMemoryActivityInstanceStore>();
            builder.Services.AddScoped<ActivityAgent>();
            builder.Services.AddScoped<ActivityOrchestrator>();

            // Mental Model Architecture — specialised, single-responsibility models
            // orchestrated by the Conversation Engine. Each is registered as IMentalModel so
            // the engine discovers them automatically; adding a future model (Emotion,
            // Motivation, Goal, …) is just a new class plus one registration here.
            builder.Services.AddSingleton<IPetIdentityProvider, PetIdentityProvider>();
            builder.Services.AddScoped<IMentalModel, SkillModel>();
            builder.Services.AddScoped<IMentalModel, UserModel>();
            builder.Services.AddScoped<IMentalModel, PetModel>();
            builder.Services.AddScoped<IMentalModel, RelationshipModel>();
            builder.Services.AddScoped<IMentalModel, ConversationMemoryModel>();
            builder.Services.AddScoped<IConversationStrategist, ConversationStrategist>();
            builder.Services.AddScoped<ConversationEngine>();

            // Settings service (if it exists in Business project)
            try
            {
                var settingsServiceType = Type.GetType("AppLogic.Web.SettingsService, MauiApp1");
                if (settingsServiceType != null)
                {
                    var iSettingsServiceType = Type.GetType("Business.Interfaces.ISettingsService, Business");
                    if (iSettingsServiceType != null)
                    {
                        builder.Services.AddSingleton(iSettingsServiceType, settingsServiceType);
                    }
                }
            }
            catch
            {
                // Ignore if not available
            }

            var app = builder.Build();

            // Initialize the local document store at startup
            using (var scope = app.Services.CreateScope())
            {
                var initialiser = scope.ServiceProvider.GetRequiredService<PetDbContextInitialiser>();
                initialiser.InitialiseAsync().GetAwaiter().GetResult();
            }

            return app;
        }
    }
}

