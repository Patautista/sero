using Business.Audio;
using ElevenLabs;
using ElevenLabs.Models;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Infrastructure.Audio.SpeechService;

namespace Infrastructure.Audio
{
    public class SpeechService
    {
        public enum VoiceGender
        {
            Female,
            Male
        }

        private readonly ElevenLabsClient _elevenLabsClient;
        private readonly IAudioCache _cache;

        public SpeechService(ElevenLabsClient elevenLabsClient, IAudioCache cache)
        {
            _elevenLabsClient = elevenLabsClient;
            _cache = cache;
        }

        public async Task<byte[]> GenerateSpeechAsync(string text, VoiceGender voiceGender, string languageCode = "pt-BR")
        {
            var cacheKey = GetCacheKey(text, languageCode, voiceGender);

            // ✅ Busca no cache antes
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            // Caso não exista, gera via ElevenLabs
            var voice = await _elevenLabsClient.VoicesEndpoint.GetVoiceAsync(GetVoiceId(languageCode, voiceGender));

            await _elevenLabsClient.VoicesEndpoint.EditVoiceSettingsAsync(
                voice,
                new VoiceSettings(stability: 0.5F, similarityBoost: 0.7F, speakerBoost: true)
            );

            var request = new TextToSpeechRequest(voice, text, model: Model.MultiLingualV2);
            var voiceClip = await _elevenLabsClient.TextToSpeechEndpoint.TextToSpeechAsync(request);

            var bytes = voiceClip.ClipData.ToArray();

            // ✅ Salva no cache para chamadas futuras
            await _cache.SetAsync(cacheKey, bytes);

            return bytes;
        }

        private string GetVoiceId(string lang, VoiceGender voiceGender)
        {
            if (lang == "it" && voiceGender == VoiceGender.Female)
            {
                return "3DPhHWXDY263XJ1d2EPN";
            }
            throw new NotSupportedException($"No voice configured for {lang}-{voiceGender}");
        }

        private string GetCacheKey(string text, string lang, VoiceGender voiceGender)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes($"{lang}:{voiceGender}:{text}"))
            );
            return hash;
        }
    }
}
