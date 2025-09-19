using AppLogic.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        public ApiService(HttpClient httpClient, ISettingsService settingsService)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://bison-settling-longhorn.ngrok-free.app");
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settingsService.ApiKey.Value);
        }
        public async Task<byte[]> GetTTSAsync(string text, string lang, string voice = "female")
        {
            var url = $"/api/tts?text={Uri.EscapeDataString(text)}&lang={Uri.EscapeDataString(lang)}&voice={voice}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
