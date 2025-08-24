namespace AspireApp1.ApiService
{
    using HtmlAgilityPack;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml;

    public class DictCcClient
    {
        private readonly HttpClient _httpClient;
        private readonly DictCcConfig _config;
        private readonly ConcurrentDictionary<string, string[]> _cache = new();

        public DictCcClient(DictCcConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "C# dict.cc client");
        }

        public async Task<string[]> TranslateAsync(string word)
        {
            if (_config.EnableCaching && _cache.TryGetValue(word, out var cached))
            {
                return cached;
            }

            try
            {
                string url = $"https://{_config.LanguagePair}.{_config.BaseDomain}/?s={Uri.EscapeDataString(word)}";

                string html = await FetchWithRetryAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var rows = doc.DocumentNode.SelectNodes("//tr[contains(@id,'tr')]");
                if (rows == null) return Array.Empty<string>();

                var translations = rows
                    .Select(row =>
                    {
                        var cols = row.SelectNodes("td");
                        if (cols == null || cols.Count < 3) return null;

                        cols[2].RemoveChild(cols[2].FirstChild);
                        string left = CleanText(cols[1].InnerText);
                        string right = CleanText(cols[2].InnerText);
                        return $"{left} ⇔ {right}";
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(_config.MaxResults)
                    .ToArray();

                if (_config.EnableCaching)
                {
                    _cache[word] = translations;
                }

                return translations;
            }
            catch(Exception ex)
            {
                return Array.Empty<string>();
            }
        }

        private async Task<string> FetchWithRetryAsync(string url)
        {
            for (int i = 0; i < _config.MaxRetries; i++)
            {
                try
                {
                    return await _httpClient.GetStringAsync(url);
                }
                catch when (i < _config.MaxRetries - 1)
                {
                    await Task.Delay(500);
                }
            }
            throw new HttpRequestException($"Failed to fetch from {url}");
        }

        private static string CleanText(string raw)
        {
            return HtmlEntity.DeEntitize(raw).Trim();
        }
    }

    public class DictCcConfig
    {
        /// <summary>
        /// Base URL of dict.cc. Default = English ↔ German.
        /// </summary>
        public string BaseDomain { get; set; } = "dict.cc";

        /// <summary>
        /// Language pair, e.g. "deen", "dees", "deenfr".
        /// Dict.cc uses these codes in subdomains (see note below).
        /// </summary>
        public string LanguagePair { get; set; } = "deen"; // German-English by default

        /// <summary>
        /// Max number of translations to return.
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Enable or disable caching of queries in memory.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Max retries for failed HTTP requests.
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }
}
