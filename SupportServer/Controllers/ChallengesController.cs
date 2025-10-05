using Business.Model;
using DeepL;
using Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace SupportServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengesController : ControllerBase
    {
        private readonly IPromptClient _geminiClient;
        private readonly DeepLClient _deepLClient;

        public ChallengesController(IPromptClient geminiClient, DeepLClient deepLClient)
        {
            // ideal: mover a chave para config segura (ex: appsettings ou Secret Manager)
            _geminiClient = geminiClient;
            _deepLClient = deepLClient;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateChallenge([FromBody] ChallengeRequest request)
        {
            if (request == null || request.Texts == null || !request.Texts.Any())
                return BadRequest("Missing texts.");

            var language = new CultureInfo(request.TargetLanguageCode);
            var nativeLanguageCode = DeepLHelper.NormalizeSourceLang(request.NativeLanguageCode);

            // Seleciona um subconjunto aleatório do vocabulário
            var random = new Random();
            var subset = request.Texts
                .OrderBy(_ => random.Next())
                .Take(10)
                .ToList();

            var prompt = $@"
                You are helping to create a natural language challenge.
                From the following vocabulary list:

                {string.Join(", ", subset)}

                Please write a sentence or short paragraph in {language.EnglishName} that:
                - Is NATURAL and MEANINFUL in everyday language
                - Uses SOME of the given words (not necessarily all)
                - Keeps the output at most 20 words long
                - Does not introduce very unusual or archaic words
                - Avoids repeating the exact same example from the input text

                DO NOT provide explanations or commentary.";

            var response = await _geminiClient.GenerateAsync(prompt);
            var translated = await _deepLClient.TranslateTextAsync(response, 
                sourceLanguageCode: DeepLHelper.NormalizeSourceLang(language.TwoLetterISOLanguageName), 
                targetLanguageCode: DeepLHelper.NormalizeTargetLang(nativeLanguageCode));

            return Ok(new CardDefinition
            {
                TargetLanguageCode = request.TargetLanguageCode,
                NativeLanguageCode = request.NativeLanguageCode,
                TargetSentence = response,
                NativeSentence = translated.Text
            });
        }
    }
    public class ChallengeRequest
    {
        public string TargetLanguageCode { get; set; }   // ex: "no" ou "en-US"
        public string NativeLanguageCode { get; set; }
        public List<string> Texts { get; set; }    // lista de frases do usuário
    }
}
