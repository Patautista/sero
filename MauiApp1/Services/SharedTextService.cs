using CommunityToolkit.Mvvm.Messaging;
using MauiApp1.Messages;

namespace MauiApp1.Services
{
    public class SharedTextService
    {
        public event EventHandler<string>? TextReceived;

        public SharedTextService()
        {
            // Listen for SharedTextMessage from the MAUI layer
            WeakReferenceMessenger.Default.Register<SharedTextMessage>(this, (recipient, message) =>
            {
                TextReceived?.Invoke(this, message.Text);
            });
        }

        public void NotifyTextReceived(string text)
        {
            TextReceived?.Invoke(this, text);
        }
    }
}