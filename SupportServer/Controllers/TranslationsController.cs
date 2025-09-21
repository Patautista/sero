using Business.Audio;
using DeepL;
using ElevenLabs.Voices;
using Infrastructure.Audio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportServer.Security;
using static Infrastructure.Audio.SpeechService;

namespace SupportServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[ApiKey]
    public class TranslationsController(DeepLClient service) : ControllerBase
    {

        [HttpGet]
        public async Task<string> GetTTS(string text, string sourceLang, string targetLang)
        {
            if (sourceLang == targetLang || string.IsNullOrEmpty(text))
            {
                return text;
            }
            sourceLang = NormalizeSourceLang(sourceLang);
            targetLang = NormalizeTargetLang(targetLang);

            var res = await service.TranslateTextAsync(text, sourceLang, targetLang);
            if (res == null || string.IsNullOrEmpty(res.Text))
            {
                throw new Exception("Translation failed");
            }
            return res.Text;
        }
        private string NormalizeSourceLang(string code)
        {
            if(code.Equals("en", StringComparison.OrdinalIgnoreCase) || code.Equals("en-us", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.English;
            }
            if (code.Equals("pt", StringComparison.OrdinalIgnoreCase) || code.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Portuguese;
            }
            if (code.Equals("it", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Italian;
            }
            return LanguageCode.English;
        }
        private string NormalizeTargetLang(string code)
        {
            if (code.Equals("en", StringComparison.OrdinalIgnoreCase) || code.Equals("en-us", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.EnglishAmerican;
            }
            if (code.Equals("pt", StringComparison.OrdinalIgnoreCase) || code.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.PortugueseBrazilian;
            }
            if (code.Equals("it", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Italian;
            }
            return LanguageCode.EnglishAmerican;
        }
    }
}
