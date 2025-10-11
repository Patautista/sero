using Business.Model;
using Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;

namespace SupportServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationController : ControllerBase
    {
        private readonly IPromptClient _promptClient;

        public EvaluationController(IPromptClient promptClient)
        {
            _promptClient = promptClient;
        }

        public class EvaluationRequest
        {
            public string Challenge { get; set; }
            public string UserAnswer { get; set; }
            public ICollection<string> PossibleAnswers { get; set; }
        }

        [HttpPost("evaluate")]
        public async Task<IActionResult> Evaluate([FromBody] EvaluationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Challenge) || string.IsNullOrWhiteSpace(request.UserAnswer))
                return BadRequest("Missing required fields.");

            // Compose prompt for LLM evaluation
            var prompt = $@"
                Translation Prompt: {request.Challenge}
                User Answer: {request.UserAnswer}
                Evaluate and output the user's answer quality: Wrong, Hard, Ok or Perfect in the following format:
                Quality: Ok ### Feedback: The translation is correct.
                Provide feedback ONLY if the answer needs any significant corrections.";




            var response = await _promptClient.GenerateAsync(prompt);

            // Parse response (assume format: "quality:correct;closestMatch:...") - adjust as needed
            var quality = AnswerQuality.Wrong;
            var closestMatch = "";

            if (!string.IsNullOrWhiteSpace(response))
            {
                var parts = response.Split("###");
                foreach (var part in parts)
                {
                    if (part.StartsWith("Quality:", StringComparison.OrdinalIgnoreCase))
                    {
                        Enum.TryParse(part.Substring(8), true, out quality);
                    }
                }
            }

            var evaluation = new AnswerEvaluation(quality, closestMatch);
            return Ok(quality.ToString());
        }
    }
}