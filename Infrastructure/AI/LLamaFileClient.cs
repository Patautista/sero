using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.AI
{
    public class LlamafileClient : IPromptClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public LlamafileClient(string baseUrl = "http://localhost:8080/v1")
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient.Timeout = TimeSpan.FromSeconds(300);
        }

        public async Task<string> GenerateAsync(string prompt, string model = "")
        {
            var requestBody = new
            {
                model = string.IsNullOrEmpty(model) ? "mistral-7b-instruct-v0.2.Q4_K_M.gguf" : model,
                messages = new[]
                {
                new { role = "user", content = prompt }
            },
                temperature = 0.2,
            };

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {

                    var json = JsonSerializer.Serialize(requestBody);
                    var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions",
                        new StringContent(json, Encoding.UTF8, "application/json"));

                    response.EnsureSuccessStatusCode();

                    var responseString = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    // Extract the assistant message
                    var message = root
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    var endTag = "</think>";

                    message = message.Substring(message.IndexOf(endTag) + endTag.Length).Replace("<｜end▁of▁sentence｜>", string.Empty).Replace("\n", string.Empty);
                    return message ?? string.Empty;

                }
                catch (Exception ex)
                {
                    if (attempt == 5)
                        break; // stop retrying
                }
            }
            return string.Empty;

        }
    }
}
