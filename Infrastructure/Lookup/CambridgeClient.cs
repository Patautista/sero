namespace Infrastructure.Lookup
{
    using HtmlAgilityPack;
    using Infrastructure.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class CambridgeConfig
    {
        /// <summary>
        /// Base URL for Cambridge Dictionary
        /// </summary>
        public string BaseUrl { get; set; } = "https://dictionary.cambridge.org";

        /// <summary>
        /// Language pair (e.g., "english-vietnamese", "english-spanish")
        /// </summary>
        public string LanguagePair { get; set; } = "english-vietnamese";

        /// <summary>
        /// Max number of retries for HTTP requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retries in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// User agent string
        /// </summary>
        public string UserAgent { get; set; } =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/114.0 Safari/537.36";
    }

    public class CambridgeClient : IDefinitionProvider, IExampleProvider
    {
        private readonly HttpClient _httpClient;
        private readonly CambridgeConfig _config;

        #region IDefinitionProvider Properties
        public string ProviderName => "Cambridge Dictionary";
        public string LanguagePair => _config.LanguagePair;
        public bool SupportsStructuredOutput => true;
        #endregion

        #region IExampleProvider Properties
        string IExampleProvider.ProviderName => "Cambridge English Corpus";
        
        public string LanguageCode => ExtractSourceLanguage(_config.LanguagePair);
        public bool SupportsAllLanguages => false; // Only English-based pairs
        public bool SupportsSearch => false; // Cambridge provides examples for specific words only
        public bool SupportsExactMatch => false;
        public bool SupportsTranslations => false; // Examples are in source language only
        #endregion

        public CambridgeClient(CambridgeConfig? config = null, HttpClient? httpClient = null)
        {
            _config = config ?? new CambridgeConfig();
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
        }

        #region IDefinitionProvider Implementation

        public async Task<DefinitionResult> GetDefinitionsAsync(string word)
        {
            var wordDef = await GetWordDefinitionsAsync(word);
            
            return new DefinitionResult
            {
                Word = word,
                ProviderName = ProviderName,
                Entries = wordDef.Entries.Select(e => new DefinitionEntry
                {
                    Headword = e.Headword,
                    PartOfSpeech = e.PartOfSpeech,
                    Pronunciation = e.Pronunciation,
                    Meanings = e.Meanings.Select(m => new DefinitionMeaning
                    {
                        Definition = m.Definition,
                        DefinitionLanguage = "en",
                        Translation = m.Translation,
                        Examples = m.Examples.ToList()
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<string> GetDefinitionsHtmlAsync(string word)
        {
            var url = $"{_config.BaseUrl}/dictionary/{_config.LanguagePair}/{Uri.EscapeDataString(word)}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract just the entry body content
            var entryBody = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'entry-body')]");
            if (entryBody == null) return "<p>No definitions found</p>";

            // Clean up the HTML
            var htmlContent = entryBody.OuterHtml;
            
            // Apply styling for better display
            return ApplyHtmlStyling(htmlContent);
        }

        public async Task<List<Example>> GetExamplesAsync(string word)
        {
            var corpusExamples = await GetCorpusExamplesAsync(word);
            
            return corpusExamples.Select(e => new Example
            {
                Sentence = e.Sentence,
                Source = e.Source,
                Context = null
            }).ToList();
        }

        #endregion

        #region IExampleProvider Implementation

        public async Task<ExampleSearchResult> SearchExamplesAsync(
            string query, 
            string? languageCode = null, 
            bool exactMatch = false, 
            int maxResults = 10)
        {
            // Cambridge doesn't support search - it only provides examples for specific words
            // We'll treat the query as a word lookup
            var examples = await GetExamplesForWordAsync(query, languageCode);
            
            return new ExampleSearchResult
            {
                Query = query,
                LanguageCode = languageCode ?? LanguageCode,
                TotalResults = examples.Count,
                Page = 1,
                PageSize = maxResults,
                Examples = examples.Take(maxResults).ToList()
            };
        }

        public async Task<List<ExampleSentence>> GetExamplesForWordAsync(string word, string? languageCode = null)
        {
            var corpusExamples = await GetCorpusExamplesAsync(word);
            
            var targetLanguage = languageCode ?? LanguageCode;
            
            return corpusExamples.Select(e => new ExampleSentence
            {
                Text = e.Sentence,
                Language = targetLanguage,
                Source = e.Source,
                Context = "From dictionary entry"
            }).ToList();
        }

        #endregion

        #region Cambridge-Specific Methods

        /// <summary>
        /// Gets word definitions with examples (Cambridge-specific format)
        /// </summary>
        public async Task<WordDefinitionResult> GetWordDefinitionsAsync(string word)
        {
            var url = $"{_config.BaseUrl}/dictionary/{_config.LanguagePair}/{Uri.EscapeDataString(word)}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new WordDefinitionResult
            {
                Word = word,
                Entries = new List<DictionaryEntry>()
            };

            // Find only the main dictionary entries with proper structure
            var entries = doc.DocumentNode.SelectNodes("//div[contains(@class, 'entry-body')]//div[contains(@class, 'di') and .//div[contains(@class, 'di-head')]]");
            if (entries == null) return result;

            // Track seen combinations to avoid duplicates
            var seenEntries = new HashSet<string>();

            foreach (var entry in entries)
            {
                var header = entry.SelectSingleNode(".//div[contains(@class, 'di-head')]");
                if (header == null) continue;

                var partOfSpeech = header.SelectSingleNode(".//span[contains(@class, 'pos')]")?.InnerText.Trim();
                var pronunciation = header.SelectSingleNode(".//span[contains(@class, 'ipa')]")?.InnerText.Trim();
                var headword = header.SelectSingleNode(".//h2")?.InnerText.Trim();

                // Skip if no part of speech (these are usually duplicate or malformed entries)
                if (string.IsNullOrWhiteSpace(partOfSpeech))
                    continue;

                // Create unique key to detect duplicates
                var entryKey = $"{headword}_{partOfSpeech}_{pronunciation}";
                if (seenEntries.Contains(entryKey))
                    continue;

                seenEntries.Add(entryKey);

                var dictionaryEntry = new DictionaryEntry
                {
                    Headword = headword ?? word,
                    PartOfSpeech = partOfSpeech,
                    Pronunciation = pronunciation,
                    Meanings = new List<Meaning>()
                };

                // Extract meanings from the body section only
                var body = entry.SelectSingleNode(".//div[contains(@class, 'di-body')]");
                if (body != null)
                {
                    var senses = body.SelectNodes(".//div[contains(@class, 'sense-block')]");
                    if (senses != null)
                    {
                        foreach (var sense in senses)
                        {
                            // Get the definition block
                            var defBlock = sense.SelectSingleNode(".//div[contains(@class, 'def-block')]");
                            if (defBlock == null) continue;

                            // Get clean definition (from def-head section)
                            var defHead = defBlock.SelectSingleNode(".//div[contains(@class, 'def-head')]");
                            var definition = defHead?.SelectSingleNode(".//div[contains(@class, 'def')]")?.InnerText.Trim();

                            // Get clean translation (from def-body section)
                            var defBody = defBlock.SelectSingleNode(".//div[contains(@class, 'def-body')]");
                            var translation = defBody?.SelectSingleNode(".//span[contains(@class, 'trans')]")?.InnerText.Trim();
                            
                            var meaning = new Meaning
                            {
                                Definition = CleanText(definition),
                                Translation = CleanText(translation),
                                Examples = new List<string>()
                            };

                            // Extract examples from def-body only
                            if (defBody != null)
                            {
                                var exampleNodes = defBody.SelectNodes(".//div[contains(@class, 'examp')]//span[contains(@class, 'eg')]");
                                if (exampleNodes != null)
                                {
                                    foreach (var example in exampleNodes)
                                    {
                                        var exampleText = CleanText(example.InnerText);
                                        if (!string.IsNullOrWhiteSpace(exampleText))
                                        {
                                            meaning.Examples.Add(exampleText);
                                        }
                                    }
                                }
                            }

                            // Only add meanings that have both definition and translation
                            if (!string.IsNullOrWhiteSpace(meaning.Definition) && !string.IsNullOrWhiteSpace(meaning.Translation))
                            {
                                dictionaryEntry.Meanings.Add(meaning);
                            }
                        }
                    }
                }

                // Only add entries that have meanings
                if (dictionaryEntry.Meanings.Any())
                {
                    result.Entries.Add(dictionaryEntry);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets corpus examples for a word
        /// </summary>
        public async Task<List<CorpusExample>> GetCorpusExamplesAsync(string word)
        {
            var url = $"{_config.BaseUrl}/dictionary/{_config.LanguagePair}/{Uri.EscapeDataString(word)}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var examples = new List<CorpusExample>();

            // Find the examples section
            var examplesSection = doc.DocumentNode.SelectSingleNode("//div[@id='dataset-example']");
            if (examplesSection == null) return examples;

            var exampleBlocks = examplesSection.SelectNodes(".//div[contains(@class, 'lbb') and contains(@class, 'lb-cm')]");
            if (exampleBlocks == null) return examples;

            foreach (var block in exampleBlocks)
            {
                var sentence = block.SelectSingleNode(".//span[contains(@class, 'deg')]")?.InnerText.Trim();
                
                // Get clean source text (remove "From the" prefix if present)
                var sourceNode = block.SelectSingleNode(".//div[contains(@class, 'dsource')]");
                var source = "Cambridge English Corpus";
                
                if (sourceNode != null)
                {
                    var sourceText = sourceNode.InnerText;
                    if (sourceText.Contains("Cambridge English Corpus"))
                    {
                        source = "Cambridge English Corpus";
                    }
                    else
                    {
                        source = CleanText(sourceText).Replace("From the ", "").Replace("From ", "");
                    }
                }

                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    examples.Add(new CorpusExample
                    {
                        Sentence = CleanText(sentence),
                        Source = source
                    });
                }
            }

            return examples;
        }

        /// <summary>
        /// Gets translations in other languages
        /// </summary>
        public async Task<List<LanguageTranslation>> GetTranslationsAsync(string word)
        {
            var url = $"{_config.BaseUrl}/dictionary/{_config.LanguagePair}/{Uri.EscapeDataString(word)}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var translations = new List<LanguageTranslation>();

            // Find translations section - look for the proper containers
            var translationSection = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'cdo-translations')]");
            if (translationSection == null) return translations;

            var translationBlocks = translationSection.SelectNodes(".//div[contains(@class, 'pr') and contains(@class, 'bw') and contains(@class, 'lp-10')]");
            if (translationBlocks == null) return translations;

            foreach (var block in translationBlocks)
            {
                var language = block.SelectSingleNode(".//div[contains(@class, 'tc-bd') and contains(@class, 'fs14')]")?.InnerText.Trim();
                var translationText = block.SelectSingleNode(".//div[contains(@class, 'tc-bb') and contains(@class, 'tb')]")?.InnerText.Trim();

                if (!string.IsNullOrWhiteSpace(language) && !string.IsNullOrWhiteSpace(translationText))
                {
                    // Extract language name (e.g., "in Chinese (Traditional)" -> "Chinese (Traditional)")
                    language = language.Replace("in ", "").Trim();

                    translations.Add(new LanguageTranslation
                    {
                        Language = language,
                        Translation = CleanText(translationText)
                    });
                }
            }

            return translations;
        }

        /// <summary>
        /// Gets complete word data (definitions, examples, and translations)
        /// </summary>
        public async Task<CompleteWordData> GetCompleteWordDataAsync(string word)
        {
            var definitions = await GetWordDefinitionsAsync(word);
            var corpusExamples = await GetCorpusExamplesAsync(word);
            var translations = await GetTranslationsAsync(word);

            return new CompleteWordData
            {
                Word = word,
                Definitions = definitions,
                CorpusExamples = corpusExamples,
                Translations = translations
            };
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
                    if (res.IsSuccessStatusCode)
                        return await res.Content.ReadAsStringAsync();
                }
                catch when (i < _config.MaxRetries - 1)
                {
                    await Task.Delay(_config.RetryDelayMs);
                }
            }
            throw new HttpRequestException($"Failed to fetch from {url}");
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

        private static string ApplyHtmlStyling(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return html
                .Replace("<div class=\"entry-body\">", "<div class=\"entry-body cambridge-dictionary\">")
                .Replace("<h2", "<h2 class=\"font-bold text-lg mb-2\"")
                .Replace("class=\"pos\"", "class=\"pos text-blue-600 font-semibold\"")
                .Replace("class=\"def\"", "class=\"def text-gray-700\"")
                .Replace("class=\"trans\"", "class=\"trans text-green-600 font-medium\"")
                .Replace("class=\"eg\"", "class=\"eg text-gray-600 italic\"");
        }

        private static string ExtractSourceLanguage(string languagePair)
        {
            // Extract source language from pairs like "english-vietnamese" -> "eng"
            var parts = languagePair.Split('-');
            if (parts.Length > 0)
            {
                var lang = parts[0].ToLower();

                return new CultureInfo(lang).TwoLetterISOLanguageName;
            }
            return "eng";
        }

        #endregion
    }

    // Cambridge-specific models (kept for backward compatibility)
    public record WordDefinitionResult
    {
        public string Word { get; set; } = string.Empty;
        public ICollection<DictionaryEntry> Entries { get; set; } = new List<DictionaryEntry>();
    }

    public record DictionaryEntry
    {
        public string Headword { get; set; } = string.Empty;
        public string? PartOfSpeech { get; set; }
        public string? Pronunciation { get; set; }
        public ICollection<Meaning> Meanings { get; set; } = new List<Meaning>();
    }

    public record Meaning
    {
        public string Definition { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public ICollection<string> Examples { get; set; } = new List<string>();
    }

    public record CorpusExample
    {
        public string Sentence { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public record LanguageTranslation
    {
        public string Language { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
    }

    public record CompleteWordData
    {
        public string Word { get; set; } = string.Empty;
        public WordDefinitionResult Definitions { get; set; } = new();
        public ICollection<CorpusExample> CorpusExamples { get; set; } = new List<CorpusExample>();
        public ICollection<LanguageTranslation> Translations { get; set; } = new List<LanguageTranslation>();
    }
}