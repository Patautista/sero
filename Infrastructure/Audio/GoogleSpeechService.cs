using Business.Audio;
using Google.Cloud.TextToSpeech.V1;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Audio
{
    public class GoogleSpeechService : ISpeechService
    {
        private readonly TextToSpeechClient _client;
        private readonly IAudioCache _cache;

        public GoogleSpeechService(TextToSpeechClient client, IAudioCache cache)
        {
            _client = client;
            _cache = cache;
        }

        public async Task<byte[]> GenerateSpeechAsync(string text, VoiceGender voiceGender, string languageCode = "pt-BR")
        {
            var cacheKey = _cache.ComputeCacheKey(text, languageCode, voiceGender);

            // Check cache first
            var cached = await _cache.GetBytesAsync(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            // The input to be synthesized
            var input = new SynthesisInput
            {
                Text = text
            };

            // Build the voice request
            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = languageCode,
                SsmlGender = voiceGender == VoiceGender.Male ? SsmlVoiceGender.Male : SsmlVoiceGender.Female
            };

            // Specify the type of audio file
            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            // Perform the text-to-speech request
            var response = await _client.SynthesizeSpeechAsync(input, voiceSelection, audioConfig);

            var bytes = response.AudioContent.ToByteArray();

            // Save to cache for future calls
            await _cache.SetAsync(cacheKey, bytes);

            return bytes;
        }
    }
}