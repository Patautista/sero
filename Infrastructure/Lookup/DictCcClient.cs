namespace Infrastructure.Lookup
{
    using Domain.Entity;
    using HtmlAgilityPack;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        private string UserAgent { get; set; } =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/114.0 Safari/537.36";

        public DictCcClient(DictCcConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        public async Task<string> GetHtmlTranslationsAsync(string word)
        {

            try
            {
                string url = $"https://{_config.LanguagePair}.{_config.BaseDomain}/?s={Uri.EscapeDataString(word)}";

                string html = await FetchWithRetryAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var rows = doc.DocumentNode.SelectNodes("//tr[contains(@id,'tr')]");
                if (rows == null) return string.Empty;

                // Grab only the translation table instead of whole page
                var table = rows.First().ParentNode;

                var tableLanguageHeader = table.ChildNodes.Where(x => x.Name == "tr").ToList().First();
                table.ChildNodes.Remove(tableLanguageHeader);

                var inflexionHeader = table.ChildNodes.Where(x => x.Name == "tr").ToList().First();
                table.ChildNodes.Remove(inflexionHeader);


                var editNodes = table.ChildNodes.Where(x => x.InnerText == "edit").ToList();

                foreach (var editNode in editNodes)
                {
                    table.ChildNodes.Remove(editNode);
                }

                // Get a list of the translation rows to safely iterate
                var translations = table.ChildNodes.Where(x => x.Name == "tr" && x.Id != "").ToList();

                foreach (var translation in translations)
                {
                    // Convert to a list to safely remove nodes
                    var imgNodes = translation.ChildNodes.Where(x => string.IsNullOrEmpty(x.InnerText)).ToList();

                    foreach (var node in imgNodes)
                    {
                        translation.ChildNodes.Remove(node);
                    }

                    var textNodes = translation.ChildNodes.Where(x => !string.IsNullOrEmpty(x.InnerText)).ToList();

                    if (textNodes.Count() > 1)
                    {
                        textNodes.ElementAt(0).SetAttributeValue("class", "text-blue-600 w-full");
                        textNodes.ElementAt(1).SetAttributeValue("class", "text-gray-800 w-full");
                    }
                }
                return ApplyCustomStyling(table?.OuterHtml) ?? "<p>No results found</p>";

            }
            catch(Exception ex)
            {
                return string.Empty;
            }
        }

        private async Task<string> FetchWithRetryAsync(string url)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var res = await _httpClient.GetAsync(url);
                    if (!res.IsSuccessStatusCode)
                    {
                        await Task.Delay((i + 1) * 3000);
                        continue;
                    }
                    return await res.Content.ReadAsStringAsync();
                }
                catch when (i < 3 - 1)
                {
                    await Task.Delay(1000);
                }
            }
            throw new HttpRequestException($"Failed to fetch from {url}");
        }

        private string ApplyCustomStyling(string tableHtml)
        {
            if (string.IsNullOrWhiteSpace(tableHtml))
                return string.Empty;

            return tableHtml
                .Replace("<table", "<table class=\"table table-bordered table-striped table-hover table-sm\"")
                .Replace("class=\"noline", "class=\"noline w-full text-sm")
                //.Replace("table", "div")
                //.Replace("tbody", "div")
                //.Replace("tr", "div")
                //.Replace("td", "div")
                //.Replace("th", "div");

                .Replace("<th", "<th class=\"table-light\"");
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
