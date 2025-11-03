using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Audio
{
    public enum VoiceGender
    {
        Female,
        Male
    }
    public interface IAudioCache
    {
        Task<byte[]?> GetBytesAsync(string key);
        Task SetAsync(string key, byte[] data);
        string ComputeCacheKey(string text, string lang, VoiceGender voiceGender);
    }
}
