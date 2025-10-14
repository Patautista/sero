using Business.Audio;
using Google.Cloud.TextToSpeech.V1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Audio
{
    public class GoogleSpeechService : ISpeechService
    {
        private readonly TextToSpeechClient _client;
        private readonly IAudioCache _cache;

        // Mapping of two-letter ISO codes to BCP-47 language codes
        private static readonly Dictionary<string, string> LanguageCodeMap = new()
        {
            { "pt", "pt-BR" },
            { "en", "en-US" },
            { "es", "es-ES" },
            { "fr", "fr-FR" },
            { "de", "de-DE" },
            { "it", "it-IT" },
            { "ja", "ja-JP" },
            { "ko", "ko-KR" },
            { "zh", "zh-CN" },
            { "ru", "ru-RU" },
            { "ar", "ar-XA" },
            { "hi", "hi-IN" },
            { "nb", "nb-NO" },
            { "no", "nb-NO" },
            { "sv", "sv-SE" },
            { "da", "da-DK" },
            { "fi", "fi-FI" },
            { "nl", "nl-NL" },
            { "pl", "pl-PL" },
            { "tr", "tr-TR" },
            { "vi", "vi-VN" },
        };

        public GoogleSpeechService(TextToSpeechClient client, IAudioCache cache)
        {
            _client = client;
            _cache = cache;
        }

        public async Task<byte[]> GenerateSpeechAsync(string text, VoiceGender voiceGender, string languageCode = "pt-BR")
        {
            // Convert two-letter ISO code to BCP-47 if needed
            var bcp47LanguageCode = ConvertToBcp47(languageCode);

            var voices = await _client.ListVoicesAsync(new ListVoicesRequest
            {
                LanguageCode = bcp47LanguageCode
            });
            var sslmGender = voiceGender == VoiceGender.Male ? SsmlVoiceGender.Male : SsmlVoiceGender.Female;

            string voiceName = voices.Voices.Count > 0 ? voices.Voices.Where(v => v.SsmlGender == sslmGender).First().Name : null;

            var cacheKey = _cache.ComputeCacheKey(text, bcp47LanguageCode, voiceGender);

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
                LanguageCode = bcp47LanguageCode
            };

            // Set voice name if provided, otherwise use SSML gender
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                voiceSelection.Name = voiceName;
            }
            else
            {
                voiceSelection.SsmlGender = sslmGender;
            }

            // Specify the type of audio file
            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Linear16
            };

            // Perform the text-to-speech request
            var response = await _client.SynthesizeSpeechAsync(input, voiceSelection, audioConfig);

            var bytes = response.AudioContent.ToByteArray();

            // Save to cache for future calls
            await _cache.SetAsync(cacheKey, bytes);

            return bytes;
        }

        private string ConvertToBcp47(string languageCode)
        {
            // If already in BCP-47 format (contains hyphen), return as is
            if (languageCode.Contains('-'))
            {
                return languageCode;
            }

            // Try to map two-letter ISO code to BCP-47
            if (LanguageCodeMap.TryGetValue(languageCode.ToLower(), out var bcp47Code))
            {
                return bcp47Code;
            }

            // If no mapping found, return the original code
            return languageCode;
        }
    }
}