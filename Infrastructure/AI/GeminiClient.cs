using Adapter.AI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.AI
{
    public class GeminiClient
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

        public virtual async Task<string> GenerateAsync(string prompt, string model = "gemini-flash-latest", string outputTemplate = "")
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
                generationConfig = string.IsNullOrEmpty(outputTemplate) ? null : new
                {
                    response_mime_type = "application/json",
                    response_json_schema = outputTemplate
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

                    // Remove markdown code block wrapper if present
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Trim();
                        if (text.StartsWith("```json"))
                        {
                            text = text.Substring(7); // Remove "```json"
                        }
                        else if (text.StartsWith("```"))
                        {
                            text = text.Substring(3); // Remove "```"
                        }

                        if (text.EndsWith("```"))
                        {
                            text = text.Substring(0, text.Length - 3); // Remove trailing "```"
                        }

                        text = text.Trim();
                    }

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
        public virtual Task<T> GenerateJsonAsync<T>(string prompt, string model = "")
        {
            var template = ((T?)default).ToJsonTemplate();
            return GenerateAsync(prompt, model, template)
                .ContinueWith(task =>
                {
                    var json = task.Result;
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                        };

                        var obj = JsonSerializer.Deserialize<T>(json, options) ?? Activator.CreateInstance<T>();

                        return obj;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JSON Deserialization Error: {ex.Message}");
                        Console.WriteLine($"JSON Content: {json}");
                        Console.WriteLine();
                        Console.WriteLine("Expected format:");
                        Console.WriteLine(((T?)default).ToJsonTemplate());
                        return Activator.CreateInstance<T>();
                    }
                });
        }
    }
}
