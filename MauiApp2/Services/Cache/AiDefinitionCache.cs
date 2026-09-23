using Business.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services.Cache
{
    // File-based cache for AI-generated definitions
    public class AiDefinitionCache
    {
        private readonly string _basePath;

        public AiDefinitionCache(string basePath = "ai_definitions_cache/")
        {
            _basePath = Path.Combine(FileSystem.Current.CacheDirectory, basePath);
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        public async Task<DefinitionResult?> GetAsync(string word, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var key = ComputeCacheKey(word, sourceLanguage, targetLanguage);
                var path = GetPath(key);
                if (File.Exists(path))
                {
                    var json = await File.ReadAllTextAsync(path);
                    return JsonSerializer.Deserialize<DefinitionResult>(json);
                }
                return null;
            }
            catch
            {
                // Treat unreadable/corrupt cache entries as a cache miss
                return null;
            }
        }

        public async Task SetAsync(string word, string sourceLanguage, string targetLanguage, DefinitionResult result)
        {
            var key = ComputeCacheKey(word, sourceLanguage, targetLanguage);
            var path = GetPath(key);
            var json = JsonSerializer.Serialize(result);
            await File.WriteAllTextAsync(path, json);
        }

        public string ComputeCacheKey(string word, string sourceLanguage, string targetLanguage)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes($"{sourceLanguage}:{targetLanguage}:{word}"))
            );
            return hash;
        }

        private string GetPath(string key) => Path.Combine(_basePath, $"{key}.json");
    }
}
