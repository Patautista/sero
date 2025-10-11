using System.Threading.Tasks;

namespace Business.Audio
{
    public interface ISpeechService
    {
        Task<byte[]> GenerateSpeechAsync(string text, VoiceGender voiceGender, string languageCode = "pt-BR");
    }
}