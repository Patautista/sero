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
        string? systemInstruction = options?.Instructions + AILanguageInstructions + "\n" + PunctuationInstructions;

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
    private static string AILanguageInstructions => "## AI-Sounding Language Avoidance\r\n\r\nWhen generating content, avoid language patterns commonly associated with AI-generated writing. These include overused expressions, vague buzzwords, formulaic transitions, inflated descriptors, and hedge phrases.\r\n\r\n### Do NOT use:\r\n\r\n- Phrases: “in today’s fast-paced world,” “in the ever-evolving world,” “in the realm of,” “it’s important to note,” “aims to explore,” “when it comes to,” “at the end of the day,” “navigating the landscape,” “because of this,” “in other words,” “overall”\r\n\r\n- Buzzwords/Adjectives: revolutionary, groundbreaking, cutting-edge, paradigm-shifting, transformative, game-changing, disruptive, innovative, comprehensive, robust, seamless, holistic, pivotal, crucial, paramount, quintessential, remarkable, amazing, striking, captivating, significant, substantial, notable, considerable, meticulous, intricate, multifaceted, profound\r\n\r\n- Verbs: delve, dive, unlock, unleash, harness, leverage, orchestrate, streamline, facilitate, enhance, showcase, underscore, spearhead, revolutionize, transcend, galvanize, cultivate, proliferate, utilize, strategize, synthesize, delineate, articulate, conceptualize, manifest, elucidate, inquire, discern, unveil\r\n\r\n- Nouns/Concepts: journey, landscape, realm, tapestry, symphony, odyssey, paradigm, nexus, spectrum, trajectory, synergy, alignment, benchmark, milestone, facet, epitome, pinnacle, testament, gusto\r\n\r\n- Transitions/Hedges: moreover, furthermore, therefore, consequently, subsequently, accordingly, nevertheless, however, indeed, notably, particularly, additionally, “it seems that,” “it appears,” “one could argue,” “might,” “can be,” “tends to,” “appears to be,” “could potentially,” “seems to suggest”\r\n\r\n### Instead, use:\r\n\r\n- Clear, simple, specific language\r\n\r\n- A mix of short, medium, and long sentence lengths\r\n\r\n- Natural transitions: “also,” “then,” “so,” “but”\r\n\r\n- Contractions: don’t, won’t, can’t, it’s, I’m, etc.\r\n\r\n- Personal pronouns: I, you, we\r\n\r\n- Specific examples and concrete details\r\n\r\n- Rhetorical questions or slight imperfections to mimic natural speech\r\n\r\n- A relaxed, conversational tone that feels human and grounded";

    private static string PunctuationInstructions => "## Punctuation Pattern Avoidance\r\n\r\nWhen generating content, avoid AI-typical punctuation habits. Write with natural variation that reflects how humans use punctuation in real communication—expressive, imperfect, and context-aware.\r\n\r\n### Do NOT use:\r\n\r\n- Em dashes (—) as default punctuation; avoid stacking them or using them in place of commas, periods, or parentheses\r\n\r\n- Colons in titles, before simple lists, or in casual explanations\r\n\r\n- Curly quotation marks (“ ”) or smart apostrophes (’)—use straight versions (\" and ')\r\n\r\n- Overuse of parentheses—especially formulaic clarifications\r\n\r\n- Bullet points as the default structure—especially nested or overformatted ones\r\n\r\n- Avoidance of semicolons—use them naturally to link related clauses\r\n\r\n- Overuse of quotation marks for paraphrasing, emphasis, or generic phrases\r\n\r\n- Excessive punctuation perfection (e.g., perfect apostrophe use without variation)\r\n\r\n- Artificial ellipses (...) unless genuinely reflecting hesitation or trailing thoughts\r\n\r\n- Unnatural restraint with exclamation marks—include them occasionally in informal or expressive writing\r\n\r\n### Instead, use:\r\n\r\n- Commas to soften or interrupt thoughts\r\n\r\n- Periods to complete ideas and improve rhythm\r\n\r\n- Parentheses sparingly for genuine asides or clarifications\r\n\r\n- Natural transitions in place of colons (e.g., “such as,” “for example,” “including”)\r\n\r\n- Straight quotes and apostrophes for all text\r\n\r\n- Semicolons to connect closely related ideas\r\n\r\n- Human-like quotation usage—only for direct quotes, dialogue, or verifiable citations\r\n\r\n- Slight imperfections or inconsistencies in punctuation to reflect human spontaneity\r\n\r\n- Ellipses or exclamation marks where tone demands it\r\n\r\n- Flowing prose over rigid lists—especially for storytelling or narrative writing";

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
