using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MauiApp2.Services.Audio;
using Microsoft.AspNetCore.Components;
using System.Runtime.InteropServices;

namespace MauiApp2.Components;

public abstract class SharedBaseComponent : ComponentBase
{
    [Inject]
    protected MauiSoundService SoundService { get; set; } = default!;

    protected async Task PlayVoiceClip(string text, string lang)
    {
        bool succeeded;
        try
        {
            succeeded = await SoundService.PlayVoiceClip(text, lang);
        }
        catch
        {
            succeeded = false;
        }

        await ShowPlaybackToastAsync(succeeded);
    }

    protected async Task PlayVoiceClip(byte[] audioData)
    {
        bool succeeded;
        try
        {
            succeeded = await SoundService.PlayAudioAsync(audioData);
        }
        catch
        {
            succeeded = false;
        }

        await ShowPlaybackToastAsync(succeeded);
    }

    private static async Task ShowPlaybackToastAsync(bool succeeded)
    {
        var message = succeeded ? "Reproduzindo áudio." : "Falha ao reproduzir áudio.";

        try
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show(CancellationToken.None);
        }
        catch (COMException)
        {
            System.Diagnostics.Debug.WriteLine("Native toast notifications are unavailable in the current environment.");
        }
    }
}
