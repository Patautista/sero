using AppLogic.Web;
using Infrastructure.Audio;
using MauiApp1.Services.Translations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

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
            _httpClient.BaseAddress = new Uri("https://bison-settling-longhorn.ngrok-free.app");
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settingsService.ApiKey.Value);
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
                response.EnsureSuccessStatusCode();
                var translation = await response.Content.ReadAsStringAsync();

                // 3. Cache the translation
                await _translationCache.SetAsync(text, sourceLang, targetLang, translation);

                return translation;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Error";
            }
        }

        /// <summary>
        /// Translates using the default source/target languages from SettingsService. Returns null if not configured.
        /// </summary>
        public async Task<string?> GetNativeTranslationAsync(string text, string sourceLang = "en")
        {
            var studyConfig = _settingsService.StudyConfig.Value;
            if (studyConfig?.SelectedLanguage == null)
                return null;

            var targetLang = studyConfig.SelectedLanguage.Source?.TwoLetterISOLanguageName;

            if (string.IsNullOrWhiteSpace(sourceLang) || string.IsNullOrWhiteSpace(targetLang))
                return null;

            return await GetTranslationAsync(text, sourceLang, targetLang);
        }
    }
}
