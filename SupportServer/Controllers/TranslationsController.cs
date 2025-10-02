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
            sourceLang = DeepLHelper.NormalizeSourceLang(sourceLang);
            targetLang = DeepLHelper.NormalizeTargetLang(targetLang);

            var res = await service.TranslateTextAsync(text, sourceLang, targetLang);
            if (res == null || string.IsNullOrEmpty(res.Text))
            {
                throw new Exception("Translation failed");
            }
            return res.Text;
        }
    }
}
