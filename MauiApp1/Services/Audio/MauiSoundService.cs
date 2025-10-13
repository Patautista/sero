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

        public MauiSoundService(IAudioCache audioCache, ApiService apiService)
        {
            _audioCache = audioCache;
            _apiService = apiService;
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

                // Depois de obter audioData
                var cachePath = Path.Combine(FileSystem.CacheDirectory, "tts_cache");
                if (!Directory.Exists(cachePath))
                    Directory.CreateDirectory(cachePath);

                var filePath = Path.Combine(cachePath, $"{key}.mp3");

                // Grava o arquivo se não existir
                if (!File.Exists(filePath))
                    await File.WriteAllBytesAsync(filePath, audioData);

                // Agora toca do arquivo
                var player = AudioManager.Current.CreatePlayer(filePath);
                player.Play();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task PlayCorrectAnswer()
        {
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync("correct_answer.mp3");
                var player = _audioManager.CreatePlayer(stream);
                player.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to play correct answer sound: {ex.Message}");
                // Optionally: fallback to system beep or ignore
            }
        }
    }
}
