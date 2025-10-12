using MauiApp1.Services;

namespace MauiApp1
{
    public partial class App : Application
    {
        public App(SharedTextService sharedTextService)
        {
            InitializeComponent();

            MainPage = new MainPage(sharedTextService);
        }
    }
}
