using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class WiktionaryOptions
    {
        public string BaseUrl { get; set; } = "https://en.m.wiktionary.org/wiki/";
        public string Language { get; set; } = "Italian";
        public int MaxRetries { get; set; } = 5;
        public int RetryDelayMs { get; set; } = 1000;
        public bool EnableCaching { get; set; } = true;
        public string UserAgent { get; set; } =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/114.0 Safari/537.36";
    }

    public class WiktionaryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, string[]> _cache = new();
        private readonly WiktionaryOptions _options;

        public WiktionaryClient(WiktionaryOptions? options = null, HttpClient? httpClient = null)
        {
            _options = options ?? new WiktionaryOptions();

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
        }

        public async Task<string> GetHtmlVerbInflectionTable(string verb)
        {
            if (_options.EnableCaching && _cache.TryGetValue(verb, out var cached))
                return cached.FirstOrDefault() ?? string.Empty;

            var url = $"{_options.BaseUrl}{verb}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Example: //h2/span[@id='Italian']
            var header = doc.DocumentNode.SelectNodes($"//h2[contains(@id,'{_options.Language}')]").FirstOrDefault();

            if (header == null)
                return string.Empty;

            // Climb up to <h2> then find the following tables in that section
            var collapsableSection = header.ParentNode.ParentNode;
            var table = collapsableSection
                .SelectNodes(".//table[contains(@class, 'roa-inflection-table')]")
                ?.FirstOrDefault();

            var result = ApplyBootstrapStyling(table?.OuterHtml) ?? string.Empty;

            if (_options.EnableCaching)
                _cache[verb] = new[] { result };

            return result;
        }

        private async Task<string> FetchWithRetryAsync(string url)
        {
            for (int i = 0; i < _options.MaxRetries; i++)
            {
                try
                {
                    var res = await _httpClient.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                        return await res.Content.ReadAsStringAsync();
                }
                catch when (i < _options.MaxRetries - 1)
                {
                    await Task.Delay(_options.RetryDelayMs);
                }
            }
            throw new HttpRequestException($"Failed to fetch from {url}");
        }

        private string ApplyBootstrapStyling(string tableHtml)
        {
            if (string.IsNullOrWhiteSpace(tableHtml))
                return string.Empty;

            return tableHtml
                .Replace("<table", "<table class=\"table table-bordered table-striped table-hover\"")
                .Replace("<th", "<th class=\"table-light\"");
        }
    }
}
