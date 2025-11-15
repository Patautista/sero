namespace Infrastructure.Lookup
{
    using HtmlAgilityPack;
    using Infrastructure.Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
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

            // Group translations by part of speech
            var entriesByPos = new Dictionary<string, DefinitionEntry>();

            foreach (var row in rows.Take(_config.MaxResults))
            {
                var cells = row.SelectNodes(".//td[contains(@class,'td7nl')]");
                if (cells == null || cells.Count < 2) continue;

                // Extract source (first language) cell
                var sourceCell = cells[0];
                var sourceText = ExtractMainText(sourceCell);
                var sourcePosInfo = ExtractPartOfSpeech(sourceCell);
                var sourceContext = ExtractContext(sourceCell);

                // Extract target (second language) cell
                var targetCell = cells[1];
                var targetText = ExtractMainText(targetCell);
                var targetPosInfo = ExtractPartOfSpeech(targetCell);

                if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(targetText))
                    continue;

                // Determine part of speech (prefer source language)
                var partOfSpeech = NormalizePartOfSpeech(sourcePosInfo ?? targetPosInfo);

                // Use part of speech as grouping key, or "general" if none
                var entryKey = partOfSpeech ?? "general";

                // Get or create entry for this part of speech
                if (!entriesByPos.TryGetValue(entryKey, out var entry))
                {
                    entry = new DefinitionEntry
                    {
                        Headword = word,
                        PartOfSpeech = partOfSpeech,
                        Pronunciation = null,
                        Meanings = new List<DefinitionMeaning>()
                    };
                    entriesByPos[entryKey] = entry;
                }

                // Add the meaning to the appropriate entry
                var meaning = new DefinitionMeaning
                {
                    Definition = CleanText(sourceText),
                    Translation = CleanText(targetText),
                    Examples = new List<string>()
                };

                // Add context as a prefix to definition if available
                if (!string.IsNullOrWhiteSpace(sourceContext))
                {
                    meaning.Definition = $"[{sourceContext}] {meaning.Definition}";
                }

                entry.Meanings.Add(meaning);
            }

            // Add all entries that have meanings
            foreach (var entry in entriesByPos.Values)
            {
                if (entry.Meanings.Any())
                {
                    result.Entries.Add(entry);
                }
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

        /// <summary>
        /// Extracts the main text from a cell, excluding part of speech tags and context markers
        /// </summary>
        private string ExtractMainText(HtmlNode cell)
        {
            // Clone the cell to avoid modifying the original
            var clone = cell.CloneNode(true);

            // Remove var tags (part of speech markers)
            var varNodes = clone.SelectNodes(".//var")?.ToList();
            if (varNodes != null)
            {
                foreach (var varNode in varNodes)
                {
                    varNode.Remove();
                }
            }

            // Remove dfn tags (context/subject markers) - we extract these separately
            var dfnNodes = clone.SelectNodes(".//dfn")?.ToList();
            if (dfnNodes != null)
            {
                foreach (var dfnNode in dfnNodes)
                {
                    dfnNode.Remove();
                }
            }

            // Remove kbd tags (brackets and explanatory text)
            var kbdNodes = clone.SelectNodes(".//kbd")?.ToList();
            if (kbdNodes != null)
            {
                foreach (var kbdNode in kbdNodes)
                {
                    kbdNode.Remove();
                }
            }

            // Remove abbr tags (verb prefix indicators like "å")
            var abbrNodes = clone.SelectNodes(".//abbr")?.ToList();
            if (abbrNodes != null)
            {
                foreach (var abbrNode in abbrNodes)
                {
                    // Keep the text content but remove the tag
                    var textNode = clone.OwnerDocument.CreateTextNode(abbrNode.InnerText);
                    abbrNode.ParentNode.ReplaceChild(textNode, abbrNode);
                }
            }

            var text = CleanText(clone.InnerText);
            
            // Remove leading numbers (e.g., "3hair" -> "hair", "12word" -> "word")
            text = RemoveLeadingNumber(text);
            
            return text;
        }

        /// <summary>
        /// Removes leading numbers from text (e.g., "3hair" -> "hair")
        /// </summary>
        private static string RemoveLeadingNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Match one or more digits at the start of the string followed by any character
            // This handles cases like "3hair", "12word", etc.
            var match = Regex.Match(text, @"^(\d+)(.+)$");
            
            if (match.Success && match.Groups.Count >= 3)
            {
                // Return the text after the number
                return match.Groups[2].Value;
            }

            return text;
        }

        /// <summary>
        /// Extracts part of speech information from var tags
        /// </summary>
        private string? ExtractPartOfSpeech(HtmlNode cell)
        {
            var varNodes = cell.SelectNodes(".//var");
            if (varNodes == null || !varNodes.Any()) return null;

            // Collect all part of speech markers
            var posList = new List<string>();
            
            foreach (var varNode in varNodes)
            {
                var text = varNode.InnerText.Trim();
                // Remove curly braces and clean up
                text = text.Replace("{", "").Replace("}", "").Trim();
                
                if (!string.IsNullOrWhiteSpace(text))
                {
                    posList.Add(text);
                }
            }

            return posList.Any() ? string.Join(", ", posList) : null;
        }

        /// <summary>
        /// Extracts context/subject information from dfn tags
        /// </summary>
        private string? ExtractContext(HtmlNode cell)
        {
            var dfnNode = cell.SelectSingleNode(".//dfn");
            if (dfnNode == null) return null;

            var title = dfnNode.GetAttributeValue("title", null);
            return title;
        }

        /// <summary>
        /// Normalizes part of speech abbreviations to full forms
        /// </summary>
        private string? NormalizePartOfSpeech(string? pos)
        {
            if (string.IsNullOrWhiteSpace(pos)) return null;

            // Handle multiple parts of speech separated by commas or slashes
            if (pos.Contains(",") || pos.Contains("/"))
            {
                var parts = pos.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(p => NormalizeSinglePos(p.Trim()))
                               .Where(p => !string.IsNullOrEmpty(p));
                return string.Join(", ", parts);
            }

            return NormalizeSinglePos(pos);
        }

        /// <summary>
        /// Normalizes a single part of speech abbreviation
        /// </summary>
        private string? NormalizeSinglePos(string pos)
        {
            if (string.IsNullOrWhiteSpace(pos)) return null;

            return pos.ToLower() switch
            {
                "adj" => "adjective",
                "adv" => "adverb",
                "n" => "noun",
                "v" => "verb",
                "m" => "masculine noun",
                "f" => "feminine noun",
                "m/f" => "masculine/feminine noun",
                "pl" => "plural",
                _ => pos // Return as-is if not recognized
            };
        }

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

            // Remove multiple spaces
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
