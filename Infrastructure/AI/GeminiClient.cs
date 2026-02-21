using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.AI
{
    public class GeminiClient : IPromptClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
            };
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
        }

        public async Task<string> GenerateAsync(string prompt, string model = "gemini-flash-latest")
        {
            if (string.IsNullOrEmpty(model))
            {
                model = "gemini-flash-latest";
            }
            var requestBody = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        },
        generationConfig = new
        {
            responseMimeType = "application/json"
        }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            int maxRetries = 3;                  // how many times to retry
            int delayMinutes = 2;                // initial delay
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.PostAsync(
                        $"v1beta/models/{model}:generateContent", content);

                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);

                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt == maxRetries)
                        break; // stop retrying

                    // exponential backoff
                    await Task.Delay(TimeSpan.FromMinutes(delayMinutes));
                    delayMinutes *= 2;
                }
            }

            // If all retries failed, optionally log `lastException`
            return string.Empty;
        }
    }
}
