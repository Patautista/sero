using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services.Cache
{
    // File-based cache for translations
    public class MobileTranslationCache
    {
        private readonly string _basePath;

        public MobileTranslationCache(string basePath = "translations_cache/")
        {
            _basePath = Path.Combine(FileSystem.Current.CacheDirectory, basePath);
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        public async Task<string?> GetAsync(string text, string sourceLang, string targetLang)
        {
            var key = ComputeCacheKey(text, sourceLang, targetLang);
            var path = GetPath(key);
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            return null;
        }

        public async Task SetAsync(string text, string sourceLang, string targetLang, string translation)
        {
            var key = ComputeCacheKey(text, sourceLang, targetLang);
            var path = GetPath(key);
            await File.WriteAllTextAsync(path, translation);
        }

        public string ComputeCacheKey(string text, string sourceLang, string targetLang)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes($"{sourceLang}:{targetLang}:{text}"))
            );
            return hash;
        }

        private string GetPath(string key) => Path.Combine(_basePath, $"{key}.txt");
    }
}