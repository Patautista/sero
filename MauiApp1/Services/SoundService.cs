using Business.Audio;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
    public class SoundService
    {
        private static IAudioManager _audioManager = new AudioManager();
        private readonly IAudioCache _audioCache;
        public SoundService(IAudioCache audioCache)
        {
            _audioCache = audioCache;
        }
        public async Task PlayVoice(string text, string lang, VoiceGender voiceGender = VoiceGender.Female)
        {
            try
            {
                var key = _audioCache.ComputeCacheKey(text, lang, voiceGender);
                var player = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync($"Sounds/voice_cache/{key}.mp3"));
                player.Play();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static async Task PlaySuccessSound()
        {
            // success.wav must be in Resources/Raw/
            var player = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("success.wav"));
            player.Play();
        }
    }
}
