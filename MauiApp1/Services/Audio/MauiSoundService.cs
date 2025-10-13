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
        private IAudioPlayer? _currentPlayer;

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

                // Stop and dispose any currently playing audio
                _currentPlayer?.Stop();
                _currentPlayer?.Dispose();

                // Create a MemoryStream from the audio data
                var stream = new MemoryStream(audioData);
                _currentPlayer = AudioManager.Current.CreatePlayer(stream);
                _currentPlayer.Play();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to play audio: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task PlayCorrectAnswer()
        {
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync("correct_answer.mp3");
                
                // Stop and dispose any currently playing audio
                _currentPlayer?.Stop();
                _currentPlayer?.Dispose();
                
                _currentPlayer = _audioManager.CreatePlayer(stream);
                _currentPlayer.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to play correct answer sound: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Optionally: fallback to system beep or ignore
            }
        }
    }
}
