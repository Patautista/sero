using CommunityToolkit.Maui;
using Infrastructure.Data;
using MauiApp2.Services.AI;
using Microsoft.Extensions.AI;
using MauiApp2.Features.Activities;
using MauiApp2.Features.Chat;
using MauiApp2.Features.LanguageCoaching;
using MauiApp2.Features.Memory;
using MauiApp2.Features.Onboarding;
using MauiApp2.Features.ProactiveMessages;
using MauiApp2.Features.QuickActions;
using MauiApp2.Services;
using Microsoft.EntityFrameworkCore;
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
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, config.DatabaseFileName);
            builder.Services.AddDbContext<PetDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            builder.Services.AddScoped<PetDbContextInitialiser>();

            // AI Services — backed by Gemini via Microsoft.Extensions.AI IChatClient
            // Passing an empty key preserves the existing unconfigured-key behaviour (fails at the first call).
            builder.Services.AddSingleton<IChatClient>(
                _ => new GeminiChatClient(config.IsConfigured() ? config.GeminiApiKey : string.Empty));

            // Core Services
            builder.Services.AddSingleton<LocalApiService>();
            builder.Services.AddSingleton<IApiService>(sp => sp.GetRequiredService<LocalApiService>());
            builder.Services.AddSingleton<Services.NotificationService>();

            // Feature Services
            builder.Services.AddScoped<OnboardingService>();
            builder.Services.AddScoped<QuickActionsService>();
            builder.Services.AddScoped<ChatService>();
            builder.Services.AddScoped<LanguageCoachingService>();
            builder.Services.AddScoped<MemoryService>();
            builder.Services.AddScoped<TimingLearningService>();
            builder.Services.AddScoped<ActivitySelectionService>();

            // Activity Agent architecture — the dedicated agent that conducts and evaluates
            // learning activities, decoupled from the conversation engine. The instance store
            // is a singleton so an in-flight activity survives across scoped ChatService uses.
            builder.Services.AddSingleton<IActivityInstanceStore, InMemoryActivityInstanceStore>();
            builder.Services.AddScoped<ActivityAgent>();
            builder.Services.AddScoped<ActivityOrchestrator>();

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

            // Apply pending EF Core migrations at startup
            using (var scope = app.Services.CreateScope())
            {
                var initialiser = scope.ServiceProvider.GetRequiredService<PetDbContextInitialiser>();
                initialiser.InitialiseAsync().GetAwaiter().GetResult();
            }

            return app;
        }
    }
}

