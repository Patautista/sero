using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using CommunityToolkit.Mvvm.Messaging;
using MauiApp1.Messages;

namespace MauiApp1
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "text/plain")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Intent = intent; // Important: update the Intent property
            HandleIntent(intent);
        }

        private void HandleIntent(Intent? intent)
        {
            if (intent?.Action == Intent.ActionSend && intent.Type == "text/plain")
            {
                var sharedText = intent.GetStringExtra(Intent.ExtraText);
                if (!string.IsNullOrEmpty(sharedText))
                {
                    // Send message using WeakReferenceMessenger
                    WeakReferenceMessenger.Default.Send(new SharedTextMessage(sharedText));
                }
            }
        }
    }
}
