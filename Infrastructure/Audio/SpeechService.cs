using Business.Audio;
using DeepL;
using ElevenLabs;
using ElevenLabs.Models;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
using Infrastructure.Services;
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

        private readonly ElevenLabsClient _elevenLabsClient;
        private readonly IAudioCache _cache;

        public SpeechService(ElevenLabsClient elevenLabsClient, IAudioCache cache)
        {
            _elevenLabsClient = elevenLabsClient;
            _cache = cache;
        }

        public async Task<byte[]> GenerateSpeechAsync(string text, VoiceGender voiceGender, string languageCode = "pt-BR")
        {
            var cacheKey = _cache.ComputeCacheKey(text, languageCode, voiceGender);

            // ✅ Busca no cache antes
            var cached = await _cache.GetBytesAsync(cacheKey);
            if (cached != null)
            {
                return cached;
            }

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
            if (lang == AvailableCodes.Italian && voiceGender == VoiceGender.Female)
            {
                return "3DPhHWXDY263XJ1d2EPN";
            }
            if (lang == AvailableCodes.Norwegian && voiceGender == VoiceGender.Female)
            {
                return "uNsWM1StCcpydKYOjKyu";
            }
            throw new NotSupportedException($"No voice configured for {lang}-{voiceGender}");
        }
    }
}
