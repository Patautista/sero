using Business.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services.Cache
{
    public class MobileAudioCache : IAudioCache
    {
        private readonly string _basePath;

        public MobileAudioCache(string? basePath = null)
        {
            _basePath = basePath ?? Path.Combine(FileSystem.AppDataDirectory, "voice_cache");
        }

        public async Task<byte[]?> GetBytesAsync(string key)
        {
            try
            {
                var path = GetPath(key);
                if (File.Exists(path))
                {
                    return await File.ReadAllBytesAsync(path);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task SetAsync(string key, byte[] data)
        {
            var path = GetPath(key);
            var dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

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
