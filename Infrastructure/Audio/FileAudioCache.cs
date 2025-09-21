using Business.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Audio
{
    // Implementação usando arquivos locais
    public class FileAudioCache : IAudioCache
    {
        private readonly string _basePath;

        public FileAudioCache(string basePath = "voice_cache")
        {
            _basePath = basePath;
            Directory.CreateDirectory(_basePath);
        }

        public async Task<byte[]?> GetBytesAsync(string key)
        {
            var path = GetPath(key);
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path);
            }
            return null;
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
