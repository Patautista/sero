using Business.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services.Cache
{
    // Implementação usando arquivos locais
    public class MobileAudioCache : IAudioCache
    {
        private readonly string _basePath;

        public MobileAudioCache(string basePath = "Sounds/voice_cache/")
        {
            _basePath = basePath;
        }

        public async Task<byte[]?> GetBytesAsync(string key)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(GetPath(key));
            if (stream != null)
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
            else
            {
                return null;
            }
        }

        public async Task SetAsync(string key, byte[] data)
        {
            var path = GetPath(key);
            await File.WriteAllBytesAsync(path, data);
        }

        public string ComputeCacheKey(string text, string lang, VoiceGender voiceGender)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes($"{lang}:{voiceGender}:{text}"))
            );
            return hash;
        }

        private string GetPath(string key) => Path.Combine(_basePath, $"{key}.mp3");
    }
}
