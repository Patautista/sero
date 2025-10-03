using Business.Audio;
using Infrastructure.Audio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportServer.Security;
using static Infrastructure.Audio.SpeechService;

namespace SupportServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiKey]
    public class TTSController(SpeechService speechService) : ControllerBase
    {

        [HttpGet]
        public async Task<FileContentResult> GetTTS(string text, string lang, string voice = "female")
        {
            VoiceGender voiceGender = voice.ToLower() == "male" ? 
                VoiceGender.Male : VoiceGender.Female;

            var audioBytes = await speechService.GenerateSpeechAsync(text, voiceGender, lang);

            return File(audioBytes, "audio/mpeg");
        }
    }
}
