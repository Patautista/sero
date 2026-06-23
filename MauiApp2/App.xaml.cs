using Infrastructure.Data;
using MauiApp2.Features.Onboarding;
using Microsoft.Extensions.DependencyInjection;

namespace MauiApp2
{
    public partial class App : Application
    {
        public App(PetDbContextInitialiser dbInitializer)
        {
            InitializeComponent();

            // Initialize database on startup
            Task.Run(async () =>
            {
                try
                {
#if DEBUG
                    // 🔧 DEVELOPMENT: Uncomment the next line to reset the database on every app start
                    // await dbInitializer.ResetDatabaseAsync();
#endif
                    await dbInitializer.InitialiseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                }
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Language Pet" };
        }
    }
}

