using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Infrastructure.Data;
using AppLogic.Web;
using Mobile.Data;

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

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<CardService>();

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
