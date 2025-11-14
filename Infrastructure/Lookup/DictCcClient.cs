namespace Infrastructure.Lookup
{
    using HtmlAgilityPack;
    using Infrastructure.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class DictCcClient : IDefinitionProvider
    {
        private readonly HttpClient _httpClient;
        private readonly DictCcConfig _config;
        private readonly ConcurrentDictionary<string, string[]> _cache = new();

        public string ProviderName => "Dict.cc";
        public string LanguagePair => _config.LanguagePair;
        public bool SupportsStructuredOutput => true;

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

        #region IDefinitionProvider Implementation

        public async Task<DefinitionResult> GetDefinitionsAsync(string word)
        {
            var url = $"https://{_config.LanguagePair}.{_config.BaseDomain}/?s={Uri.EscapeDataString(word)}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new DefinitionResult
            {
                Word = word,
                ProviderName = ProviderName,
                Entries = new List<DefinitionEntry>()
            };

            var rows = doc.DocumentNode.SelectNodes("//tr[contains(@id,'tr')]");
            if (rows == null) return result;

            // Create a single entry since Dict.cc doesn't distinguish by part of speech
            var entry = new DefinitionEntry
            {
                Headword = word,
                PartOfSpeech = null,
                Pronunciation = null,
                Meanings = new List<DefinitionMeaning>()
            };

            foreach (var row in rows.Take(_config.MaxResults))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 2) continue;

                var sourceText = CleanText(cells[0].InnerText);
                var targetText = CleanText(cells[1].InnerText);

                if (!string.IsNullOrWhiteSpace(sourceText) && !string.IsNullOrWhiteSpace(targetText))
                {
                    entry.Meanings.Add(new DefinitionMeaning
                    {
                        Definition = sourceText,
                        Translation = targetText,
                        Examples = new List<string>()
                    });
                }
            }

            if (entry.Meanings.Any())
            {
                result.Entries.Add(entry);
            }

            return result;
        }

        public async Task<string> GetDefinitionsHtmlAsync(string word)
        {
            return await GetHtmlTranslationsAsync(word);
        }

        public Task<List<Example>> GetExamplesAsync(string word)
        {
            // Dict.cc doesn't provide example sentences
            return Task.FromResult(new List<Example>());
        }

        #endregion

        #region Dict.cc Specific Methods

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
            catch (Exception)
            {
                return string.Empty;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> FetchWithRetryAsync(string url)
        {
            for (int i = 0; i < _config.MaxRetries; i++)
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
                catch when (i < _config.MaxRetries - 1)
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
                .Replace("<th", "<th class=\"table-light\"");
        }

        private static string CleanText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            var cleaned = System.Net.WebUtility.HtmlDecode(text)
                .Trim()
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ");

            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            return cleaned;
        }

        #endregion
    }

    public class DictCcConfig
    {
        /// <summary>
        /// Base URL of dict.cc
        /// </summary>
        public string BaseDomain { get; set; } = "dict.cc";

        /// <summary>
        /// Language pair, e.g. "deen", "dees", "deenfr"
        /// </summary>
        public string LanguagePair { get; set; } = "deen";

        /// <summary>
        /// Max number of translations to return
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Enable or disable caching of queries in memory
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Max retries for failed HTTP requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }
}
