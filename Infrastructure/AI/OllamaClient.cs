using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.AI
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class OllamaClient : IPromptClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public OllamaClient(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(300);
        }

        /// <summary>
        /// Send a prompt to the given model running in Ollama
        /// </summary>
        public async Task<string> GenerateAsync(string prompt, string model)
        {
            var requestBody = new
            {
                model = model,
                prompt = prompt,
                think = true,
                options = new
                {
                    temperature =  0.3
                },
                stream = false   // set to true if you want incremental streaming
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("response", out var respElement))
                return respElement.GetString() ?? string.Empty;

            return string.Empty;
        }
    }
}
