using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.AI
{
    public class OpenAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public OpenAIClient(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<string?> GetTextResponseAsync(string prompt, string model = "gpt-4.1-mini")
        {
            var requestBody = new
            {
                model,
                input = prompt
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            using var response = await _httpClient.PostAsync("responses", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OpenAIResponse>(json, _jsonOptions);

            return result?.Output?[0]?.Content?[0]?.Text;
        }

        // Optional: For structured JSON responses
        public async Task<T?> GetStructuredResponseAsync<T>(string prompt, string model = "gpt-4.1-mini")
        {
            var requestBody = new
            {
                model,
                input = prompt,
                response_format = new { type = "json_schema", json_schema = new { name = "response", schema = new { type = "object", properties = new { result = new { type = "string" } }, required = new[] { "result" } } } }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            using var response = await _httpClient.PostAsync("responses", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }

    // Minimal DTOs for parsing OpenAI response
    public class OpenAIResponse
    {
        public OpenAIOutput[]? Output { get; set; }
    }

    public class OpenAIOutput
    {
        public OpenAIContent[]? Content { get; set; }
    }

    public class OpenAIContent
    {
        public string? Text { get; set; }
    }
}
