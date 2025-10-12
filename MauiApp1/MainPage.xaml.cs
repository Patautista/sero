using CommunityToolkit.Mvvm.Messaging;
using MauiApp1.Messages;
using MauiApp1.Services;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        private readonly SharedTextService _sharedTextService;

        public MainPage(SharedTextService sharedTextService)
        {
            InitializeComponent();
            _sharedTextService = sharedTextService;

            // Register to receive SharedTextMessage
            WeakReferenceMessenger.Default.Register<SharedTextMessage>(this, (recipient, message) =>
            {
                // Notify the Blazor layer through the service
                _sharedTextService.NotifyTextReceived(message.Text);
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Unregister when page is no longer visible to prevent memory leaks
            WeakReferenceMessenger.Default.Unregister<SharedTextMessage>(this);
        }
    }
}
