using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MauiApp2.Services.AI;

/// <summary>
/// Direct <see cref="IChatClient"/> implementation for Google's Gemini REST API.
/// Handles role mapping, system instructions, and JSON response mode.
/// </summary>
public sealed class GeminiChatClient : IChatClient
{
    public const string DefaultModel = "gemini-flash-latest";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;
    private readonly ChatClientMetadata _metadata;

    public GeminiChatClient(string apiKey, string model = DefaultModel)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model;
        _httpClient = new HttpClient();
        _metadata = new ChatClientMetadata("Gemini", new Uri(BaseUrl), _model);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(messages, options);
        var url = $"{BaseUrl}/{_model}:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
        if (result?.Candidates is not { Count: > 0 })
            throw new InvalidOperationException("Gemini returned no candidates.");

        var text = result.Candidates[0].Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        return new ChatResponse([new ChatMessage(ChatRole.Assistant, text)])
        {
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var completion = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var message in completion.Messages)
        {
            if (message.Text is not null)
            {
                yield return new ChatResponseUpdate
                {
                    Contents = [new TextContent(message.Text)],
                    Role = message.Role,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceKey is not null ? null
            : serviceType == typeof(ChatClientMetadata) ? _metadata
            : serviceType?.IsInstanceOfType(this) is true ? this
            : null;
    }

    public void Dispose() => _httpClient.Dispose();

    // -------------------------------------------------------------------------
    // Request construction
    // -------------------------------------------------------------------------

    private static GeminiRequest BuildRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var contents = new List<GeminiContent>();
        string? systemInstruction = null;

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.System)
            {
                // Accumulate system messages into the top-level systemInstruction field.
                systemInstruction = (systemInstruction is null ? string.Empty : systemInstruction + "\n")
                    + (message.Text ?? string.Empty);
                continue;
            }

            var role = message.Role == ChatRole.Assistant ? "model" : "user";
            contents.Add(new GeminiContent
            {
                Role = role,
                Parts = [new GeminiPart { Text = message.Text ?? string.Empty }]
            });
        }

        var request = new GeminiRequest { Contents = contents };

        if (systemInstruction is not null)
            request.SystemInstruction = new GeminiSystemInstruction
            {
                Parts = [new GeminiPart { Text = systemInstruction }]
            };

        // JSON mode
        if (options?.ResponseFormat == ChatResponseFormat.Json)
            request.GenerationConfig = new GeminiGenerationConfig
            {
                ResponseMimeType = "application/json"
            };

        return request;
    }

    // -------------------------------------------------------------------------
    // Gemini API models
    // -------------------------------------------------------------------------

    private sealed class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("systemInstruction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiSystemInstruction? SystemInstruction { get; set; }

        [JsonPropertyName("generationConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiSystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; } = string.Empty;
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = [];
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
