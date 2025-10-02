using AppLogic.Web;
using Infrastructure.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using MauiApp1.Services.Cache;
using Business;
using System.Text.Json;
using System.Net.Http.Json;
using Business.Model;

namespace MauiApp1.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly MobileTranslationCache _translationCache;
        private readonly ISettingsService _settingsService;

        public ApiService(HttpClient httpClient, ISettingsService settingsService, MobileTranslationCache translationCache)
        {
            _httpClient = httpClient;
            if (!string.IsNullOrEmpty(settingsService.ApiConfig?.Value?.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(settingsService.ApiConfig?.Value?.BaseUrl ?? "");
            }
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settingsService.ApiConfig.Value?.ApiKey);
            _translationCache = translationCache;
            _settingsService = settingsService;
        }

        public async Task<byte[]> GetTTSAsync(string text, string lang, string voice = "female")
        {
            var url = $"/api/tts?text={Uri.EscapeDataString(text)}&lang={Uri.EscapeDataString(lang)}&voice={voice}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<string> GetTranslationAsync(string text, string sourceLang, string targetLang)
        {
            try
            {
                if (sourceLang == targetLang || string.IsNullOrEmpty(text))
                {
                    return text;
                }

                // 1. Try file cache
                var cached = await _translationCache.GetAsync(text, sourceLang, targetLang);
                if (cached != null)
                    return cached;

                // 2. Fetch from API
                var url = $"/api/Translations?text={Uri.EscapeDataString(text)}&sourceLang={Uri.EscapeDataString(sourceLang)}&targetLang={Uri.EscapeDataString(targetLang)}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var translation = await response.Content.ReadAsStringAsync();

                    // 3. Cache the translation
                    await _translationCache.SetAsync(text, sourceLang, targetLang, translation);

                    return translation;
                }
                else
                {
                    Console.WriteLine($"Translation API error: {response.StatusCode}");
                    return text;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception("Translation failed", ex);
            }
        }

        /// <summary>
        /// Translates using the default source/target languages from SettingsService. Returns null if not configured.
        /// </summary>
        public async Task<string?> GetNativeTranslationAsync(string text, string originalLang = "en")
        {
            try
            {
                var studyConfig = _settingsService.StudyConfig.Value;
                string targetLang;
                if (studyConfig?.SelectedLanguage != null)
                {
                    targetLang = studyConfig.SelectedLanguage.Source?.TwoLetterISOLanguageName;
                }
                else
                {
                    // Fallback to app language
                    targetLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                }

                if (string.IsNullOrWhiteSpace(originalLang) || string.IsNullOrWhiteSpace(targetLang))
                    return text;

                return await GetTranslationAsync(text, originalLang, targetLang);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return text;
            }
        }

        public async Task<CardDefinition?> GenerateBossChallengeAsync(string targetLanguageCode, string nativeLanguageCode, List<string> texts)
        {
            var request = new
            {
                TargetLanguageCode = targetLanguageCode,
                NativeLanguageCode = nativeLanguageCode,
                Texts = texts
            };

            var response = await _httpClient.PostAsJsonAsync("/api/challenges/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"GenerateChallenge API error: {response.StatusCode}");
                return null;
            }

            var card = await response.Content.ReadFromJsonAsync<CardDefinition>();
            return card;
        }
    }
}
