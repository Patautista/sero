using Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LexicalAnalysisController : ControllerBase
    {
        private readonly IPromptClient _promptClient;

        public LexicalAnalysisController(IPromptClient promptClient)
        {
            _promptClient = promptClient;
        }

        public class LexicalAnalysisRequest
        {
            public string Text { get; set; }
            public string Language { get; set; }
        }

        public class LexicalChunk
        {
            [JsonPropertyName("chunk")]
            public string Chunk { get; set; }

            [JsonPropertyName("translation")]
            public string Translation { get; set; }

            [JsonPropertyName("note")]
            public string Note { get; set; }
        }

        public class LexicalAnalysisResponse
        {
            [JsonPropertyName("items")]
            public List<LexicalChunk> Items { get; set; }
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] LexicalAnalysisRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Missing required text field.");

            var language = new CultureInfo(request.Language ?? "en").EnglishName;

            // Compose prompt for LLM lexical analysis
            var prompt = $@"You perform lexical segmentation.

Rules:
1. Identify meaningful lexical chunks (words or multi-word expressions).
2. Prefer multi-word expressions when they form a stable unit of meaning.
3. Do not provide explanations.
4. Output must be valid JSON only.
5. Keep chunks in original order.

Rules:
1. For each lexical chunk, provide:
   - ""translation"": a short, natural translation.
   - ""note"": a short usage note ONLY when the chunk is non-literal,
             idiomatic, fixed, or grammatically required.
2. Notes must be at most one sentence.
3. Keep output minimal and factual.

Return format:
{{
  ""items"": [
    {{
      ""chunk"": ""..."",
      ""translation"": ""..."",
      ""note"": ""...""   // empty string if no note
    }}
  ]
}}

Input: ""{request.Text}""
Input Language: {language}";

            var response = await _promptClient.GenerateAsync(prompt);

            if (string.IsNullOrWhiteSpace(response))
                return StatusCode(500, "Failed to generate lexical analysis.");

            try
            {
                // Deserialize the LLM response to ensure it's valid JSON
                var analysisResponse = JsonSerializer.Deserialize<LexicalAnalysisResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (analysisResponse == null || analysisResponse.Items == null)
                {
                    return StatusCode(500, "Invalid response format from AI.");
                }

                // Return the structured object
                return Ok(analysisResponse);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON deserialization error: {ex.Message}");
                Console.WriteLine($"Response was: {response}");
                return StatusCode(500, "Failed to parse AI response.");
            }
        }
    }
}
