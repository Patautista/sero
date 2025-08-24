using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class WiktionaryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, string[]> _cache = new();

        public WiktionaryClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/114.0 Safari/537.36");
        }
        public async Task<string> GetHtmlVerbInflectionTable(string verb)
        {
            var url = $"https://en.m.wiktionary.org/wiki/{verb}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var header = doc.DocumentNode.SelectNodes("//h2[contains(@id,'Italian')]").First();

            var collapsableSection = header.ParentNode;
            var table = collapsableSection.SelectNodes("//table[contains(@class, 'roa-inflection-table')]").FirstOrDefault();

            return table?.OuterHtml ?? string.Empty;
        }

        private async Task<string> FetchWithRetryAsync(string url)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var res = await _httpClient.GetAsync(url);
                    if (!res.IsSuccessStatusCode) continue;
                    return await res.Content.ReadAsStringAsync();
                }
                catch when (i < 5 - 1)
                {
                    await Task.Delay(1000);
                }
            }
            throw new HttpRequestException($"Failed to fetch from {url}");
        }
    }
}
