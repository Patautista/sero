using AppLogic.Web;
using Business;
using Business.Audio;
using Business.Interfaces;
using CommunityToolkit.Maui;
using Infrastructure.Data;
using Infrastructure.Services;
using MauiApp1.Services;
using MauiApp1.Services.Audio;
using MauiApp1.Services.Cache;
using MauiApp1.Services.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Radzen;
using System.IO.Compression;

namespace MauiApp1
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
                });

            builder.Services.AddRadzenComponents();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddApplicationServices(builder.Configuration);
            builder.Services.AddScoped<IAudioCache, MobileAudioCache>();
            builder.Services.AddHttpClient<MauiSoundService>();
            builder.Services.AddScoped<MauiSoundService>();
            builder.Services.AddScoped<DatabaseService>();
            builder.Services.AddScoped<AnswerEvaluator>();
            builder.Services.AddScoped<MobileTranslationCache>();
            builder.Services.AddSingleton<ApiStatusCache>();
            builder.Services.AddHttpClient<ApiService>();
            builder.Services.AddScoped<ApiService>();
            builder.Services.AddScoped<IAIEvaluator,ApiService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<VocabularyService>();            
            builder.Services.AddSingleton<SharedTextService>();            
            builder.Services.AddSingleton<LanguageDetectionService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var basePath = FileSystem.AppDataDirectory;
            builder.Services.AddDbContext<MobileDbContext>(options =>
                options.UseSqlite($"Filename={Path.Combine(basePath, "localdb.db")}")
                .EnableSensitiveDataLogging()
            );

            builder.Services.AddScoped<MobileDbContextInitialiser>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var init = scope.ServiceProvider.GetService<MobileDbContextInitialiser>();
                if (init != null)
                {
                    init.InitialiseAsync().Wait();
                }
            }

            return app;
        }
        
    }
}
