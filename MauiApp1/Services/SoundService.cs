using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MauiApp1.Services
{
    public static class SoundService
    {
        private static IAudioManager _audioManager = new AudioManager();

        public static async Task PlaySuccessSound()
        {
            // success.wav must be in Resources/Raw/
            var player = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("success.wav"));
            player.Play();
        }
    }
}
