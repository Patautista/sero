using AppLogic.Web;
using Business.Audio;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MauiApp1.Services.Audio
{
    public class MauiSoundService
    {
        private static IAudioManager _audioManager = new AudioManager();
        private readonly IAudioCache _audioCache;
        private readonly ApiService _apiService;
        private readonly HttpClient _httpClient;

        public MauiSoundService(IAudioCache audioCache, ApiService apiService)
        {
            _audioCache = audioCache;
            _apiService = apiService;
            _httpClient = new HttpClient();
        }

        public async Task<bool> PlayVoiceClip(string text, string lang, VoiceGender voiceGender = VoiceGender.Female)
        {
            try
            {
                var key = _audioCache.ComputeCacheKey(text, lang, voiceGender);
                byte[]? audioData = await _audioCache.GetBytesAsync(key);

                if (audioData == null)
                {
                    // Fetch from TTSController
                    var voice = voiceGender == VoiceGender.Male ? "male" : "female";

                    audioData = await _apiService.GetTTSAsync(text, lang, voice);

                    if (audioData != null && audioData.Length > 0)
                    {
                        await _audioCache.SetAsync(key, audioData);
                    }
                    else
                    {
                        Console.WriteLine("Failed to fetch audio from server.");
                        return false;
                    }
                }

                // Play from byte array
                using var stream = new MemoryStream(audioData);
                var player = _audioManager.CreatePlayer(stream);
                player.Play();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
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
