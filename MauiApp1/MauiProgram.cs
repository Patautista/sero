using Business;
using AppLogic.Web;
using Infrastructure.Data;
using MauiApp1.Services.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MauiApp1.Services;

namespace MauiApp1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddBlazorBootstrap();  

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<DatabaseService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var basePath = FileSystem.AppDataDirectory;
            builder.Services.AddDbContext<AnkiDbContext>(options =>
                options.UseSqlite($"Filename={Path.Combine(basePath, "localdb.db")}")
            );

            builder.Services.AddScoped<MobileDbContextInitialiser>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var init = scope.ServiceProvider.GetService<MobileDbContextInitialiser>();
                if (init != null)
                {
                    init.InitialiseAsync().Wait();
                    init.SeedAsync().Wait();
                }
            }

            return app;
        }
    }
}
