using HtmlAgilityPack;
using Business.Interfaces;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Business.Lookup
{
    public class WiktionaryOptions
    {
        public string BaseUrl { get; set; } = "https://en.m.wiktionary.org/wiki/";
        public string ApiBaseUrl { get; set; } = "https://en.wiktionary.org/api/rest_v1/page/definition/";
        public CultureInfo TargetLanguage = new CultureInfo("it");
        public int MaxRetries { get; set; } = 5;
        public int RetryDelayMs { get; set; } = 1000;
        public bool EnableCaching { get; set; } = true;
        public string UserAgent { get; set; } =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/114.0 Safari/537.36";
    }

    public class WiktionaryClient : IDefinitionProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, string[]> _cache = new();
        private readonly WiktionaryOptions _options;

        public string ProviderName => "Wiktionary";
        public string LanguagePair => _options.TargetLanguage.EnglishName;
        public bool SupportsStructuredOutput => true;

        public WiktionaryClient(WiktionaryOptions? options = null, HttpClient? httpClient = null)
        {
            _options = options ?? new WiktionaryOptions();

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
        }

        public async Task<DefinitionResult> GetDefinitionsAsync(string word)
        {
            var cacheKey = $"def_{word}";
            if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out var cached))
            {
                var cachedResult = JsonSerializer.Deserialize<DefinitionResult>(cached.FirstOrDefault());
                if (cachedResult != null)
                    return cachedResult;
            }

            var encodedWord = Uri.EscapeDataString(word);
            var url = $"{_options.ApiBaseUrl}{encodedWord}";
            
            var json = await FetchWithRetryAsync(url);
            var apiResponse = JsonSerializer.Deserialize<WiktionaryApiResponse>(json);

            if (apiResponse == null)
                return new DefinitionResult { Word = word, ProviderName = ProviderName };

            var result = BuildDefinitionResult(word, apiResponse);

            if (_options.EnableCaching)
                _cache[cacheKey] = new[] { JsonSerializer.Serialize(result) };

            return result;
        }

        public async Task<string> GetDefinitionsHtmlAsync(string word)
        {
            var definitions = await GetDefinitionsAsync(word);
            return ConvertDefinitionResultToHtml(definitions);
        }

        public async Task<List<Example>> GetExamplesAsync(string word)
        {
            var definitions = await GetDefinitionsAsync(word);
            var examples = new List<Example>();

            foreach (var entry in definitions.Entries)
            {
                foreach (var meaning in entry.Meanings)
                {
                    foreach (var example in meaning.Examples)
                    {
                        examples.Add(new Example 
                        { 
                            Sentence = example,
                            Source = ProviderName,
                            Context = $"{entry.PartOfSpeech ?? "Unknown"}"
                        });
                    }
                }
            }

            return examples;
        }

        private DefinitionResult BuildDefinitionResult(string word, WiktionaryApiResponse apiResponse)
        {
            var result = new DefinitionResult
            {
                Word = word,
                ProviderName = ProviderName
            };

            var targetLanguageName = _options.TargetLanguage.EnglishName;
            var allLanguageGroups = new List<(string language, List<WiktionaryDefinitionGroup> groups)>();

            // Add entries from the target language if available (from LanguageSpecific)
            if (apiResponse.LanguageSpecific != null)
            {
                foreach (var kvp in apiResponse.LanguageSpecific)
                {
                    allLanguageGroups.Add((kvp.Key, kvp.Value));
                }
            }

            // Add "other" entries
            if (apiResponse.Other != null && apiResponse.Other.Count > 0)
            {
                allLanguageGroups.Add(("other", apiResponse.Other));
            }

            // Process all language groups
            foreach (var (languageCode, groups) in allLanguageGroups)
            {
                foreach (var group in groups)
                {
                    // Filter: Only process entries that match the target language English name
                    if (group.Language == null || 
                        !group.Language.Equals(targetLanguageName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var entry = new DefinitionEntry
                    {
                        Headword = word,
                        PartOfSpeech = group.PartOfSpeech,
                        Pronunciation = null // API doesn't provide pronunciation in this endpoint
                    };

                    foreach (var def in group.Definitions ?? new List<WiktionaryDefinition>())
                    {
                        var meaning = new DefinitionMeaning
                        {
                            Definition = StripHtmlTags(def.Definition ?? string.Empty),
                            DefinitionLanguage = group.Language ?? languageCode,
                            Translation = string.Empty,
                            TranslationLanguage = string.Empty
                        };

                        // Extract examples - prefer parsedExamples, fall back to examples
                        if (def.ParsedExamples != null && def.ParsedExamples.Any())
                        {
                            foreach (var parsedExample in def.ParsedExamples)
                            {
                                var exampleText = StripHtmlTags(parsedExample.Example ?? string.Empty);
                                if (!string.IsNullOrWhiteSpace(parsedExample.Translation))
                                {
                                    exampleText += $" → {StripHtmlTags(parsedExample.Translation)}";
                                }
                                if (!string.IsNullOrWhiteSpace(parsedExample.Literally))
                                {
                                    exampleText += $" (lit: {StripHtmlTags(parsedExample.Literally)})";
                                }
                                meaning.Examples.Add(exampleText);
                            }
                        }
                        else if (def.Examples != null && def.Examples.Any())
                        {
                            foreach (var example in def.Examples)
                            {
                                meaning.Examples.Add(StripHtmlTags(example));
                            }
                        }

                        entry.Meanings.Add(meaning);
                    }

                    // Only add entries that have meanings
                    if (entry.Meanings.Any())
                    {
                        result.Entries.Add(entry);
                    }
                }
            }

            return result;
        }

        private string ConvertDefinitionResultToHtml(DefinitionResult result)
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"wiktionary-definitions\">");
            sb.Append($"<h3>{result.Word}</h3>");

            foreach (var entry in result.Entries)
            {
                sb.Append("<div class=\"entry\">");
                if (!string.IsNullOrEmpty(entry.PartOfSpeech))
                {
                    sb.Append($"<p class=\"part-of-speech\"><em>{entry.PartOfSpeech}</em></p>");
                }

                if (!string.IsNullOrEmpty(entry.Pronunciation))
                {
                    sb.Append($"<p class=\"pronunciation\">{entry.Pronunciation}</p>");
                }

                sb.Append("<ol class=\"meanings\">");
                foreach (var meaning in entry.Meanings)
                {
                    sb.Append("<li>");
                    sb.Append($"<span class=\"definition\">{meaning.Definition}</span>");
                    
                    if (!string.IsNullOrEmpty(meaning.DefinitionLanguage))
                    {
                        sb.Append($" <span class=\"language\">({meaning.DefinitionLanguage})</span>");
                    }

                    if (meaning.Examples.Any())
                    {
                        sb.Append("<ul class=\"examples\">");
                        foreach (var example in meaning.Examples)
                        {
                            sb.Append($"<li>{example}</li>");
                        }
                        sb.Append("</ul>");
                    }
                    sb.Append("</li>");
                }
                sb.Append("</ol>");
                sb.Append("</div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            // Get inner text and decode HTML entities
            var text = doc.DocumentNode.InnerText;
            
            // Manually decode common HTML entities since we're in .NET 8
            text = text.Replace("&amp;", "&")
                       .Replace("&lt;", "<")
                       .Replace("&gt;", ">")
                       .Replace("&quot;", "\"")
                       .Replace("&#39;", "'")
                       .Replace("&nbsp;", " ");
            
            return text;
        }

        public async Task<ConjugationData> GetVerbInflectionData(string verb)
        {
            if (_options.EnableCaching && _cache.TryGetValue(verb, out var cached))
                return JsonSerializer.Deserialize<ConjugationData>(cached.FirstOrDefault()) ?? new ConjugationData(new List<Mood>(), new List<SpecialMood>());

            var url = $"{_options.BaseUrl}{verb}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Example: //h2/span[@id='Italian']
            var header = doc.DocumentNode.SelectNodes($"//h2[contains(@id,'{_options.TargetLanguage.EnglishName}')]").FirstOrDefault();

            if (header == null)
                return new ConjugationData(new List<Mood>(), new List<SpecialMood>());

            // Climb up to <h2> then find the following tables in that section
            var collapsableSection = header.ParentNode.ParentNode;
            var table = collapsableSection
                .SelectNodes(".//table[contains(@class, 'roa-inflection-table')]")
                ?.FirstOrDefault();

            var result = ApplyBootstrapStyling(table?.OuterHtml) ?? string.Empty;

            var data = TableRefactor.ConvertTableToMobileLayout(result);

            if (_options.EnableCaching)
                _cache[verb] = new[] { JsonSerializer.Serialize(data) };

            return TableRefactor.ConvertTableToMobileLayout(result);
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

    // Wiktionary API response models
    public class WiktionaryApiResponse
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }

        [JsonIgnore]
        public Dictionary<string, List<WiktionaryDefinitionGroup>>? LanguageSpecific
        {
            get
            {
                if (AdditionalData == null)
                    return null;

                var result = new Dictionary<string, List<WiktionaryDefinitionGroup>>();
                foreach (var kvp in AdditionalData)
                {
                    // Skip "other" as it's handled separately
                    if (kvp.Key == "other")
                        continue;

                    try
                    {
                        var groups = JsonSerializer.Deserialize<List<WiktionaryDefinitionGroup>>(kvp.Value.GetRawText());
                        if (groups != null)
                        {
                            result[kvp.Key] = groups;
                        }
                    }
                    catch
                    {
                        // Skip invalid entries
                    }
                }
                return result;
            }
        }

        [JsonPropertyName("other")]
        public List<WiktionaryDefinitionGroup>? Other { get; set; }
    }

    public class WiktionaryDefinitionGroup
    {
        [JsonPropertyName("partOfSpeech")]
        public string? PartOfSpeech { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("definitions")]
        public List<WiktionaryDefinition>? Definitions { get; set; }
    }

    public class WiktionaryDefinition
    {
        [JsonPropertyName("definition")]
        public string? Definition { get; set; }

        [JsonPropertyName("examples")]
        public List<string>? Examples { get; set; }

        [JsonPropertyName("parsedExamples")]
        public List<WiktionaryParsedExample>? ParsedExamples { get; set; }
    }

    public class WiktionaryParsedExample
    {
        [JsonPropertyName("example")]
        public string? Example { get; set; }

        [JsonPropertyName("translation")]
        public string? Translation { get; set; }

        [JsonPropertyName("literally")]
        public string? Literally { get; set; }
    }

    public class TableRefactor
    {
        public static ConjugationData ConvertTableToMobileLayout(string htmlContent)
        {
            // 1. Load the original HTML content into an HtmlDocument.
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 2. Find the main table body.
            var tableBody = doc.DocumentNode.SelectSingleNode("//table/tbody");
            if (tableBody == null)
            {
                return new ConjugationData(new List<Mood>(), new List<SpecialMood>()); // Return an empty string if the table is not found.
            }

            // 4. Extract and convert the initial simple rows (infinitive, participles, etc.).
            var rows = tableBody.SelectNodes("tr").ToList();

            var specialMoodRows = rows.Take(5);

            var specialMoods = new List<SpecialMood>();

            foreach(var row in specialMoodRows)
            {
                foreach(var block in row.ChildNodes
                    .Where(c => c.Name == "th" || c.Name == "td")
                    .Select((value, index) => new { value, index }).GroupBy(x => x.index / 2).ToList())
                {
                    if(block.Count() == 2)
                    {
                        var title = block.FirstOrDefault(e => e.value.Name == "th")?.value.InnerText?.Trim();
                        var form = block.FirstOrDefault(e => e.value.Name == "td")?.value.InnerText?.Trim();
                        if (title != null && form != null)
                        {
                            specialMoods.Add(new SpecialMood(title, form));
                        }
                    }
                }
            }

            var dict = new Dictionary<string, List<HtmlNode>>();
            foreach ( var row in rows.SkipWhile(row => !CommonMoods.Any(row.InnerText.Trim().ToLower().Contains)))
            {
                if (row.ChildNodes.FindFirst("th")?.GetClasses() != null)
                {
                    var key = string.Join("", row.ChildNodes.FindFirst("th")?.GetClasses());
                    if (dict.ContainsKey(key))
                    {
                        dict[key].Add(row);
                    }
                    else
                    {
                        dict[key] = new List<HtmlNode> { row };
                    }
                }
                else
                {
                    // Try on previous siblings
                    HtmlNode node = row.PreviousSibling;
                    for (int i = 0; i < 5; i++)
                    {
                        if (node.Name == "tr" || node.PreviousSibling == null)
                        {
                            break;
                        }
                        node = node.PreviousSibling;
                    }
                    if(node.ChildNodes.FindFirst("th")?.GetClasses() != null)
                    {
                        var key = string.Join("", node.ChildNodes.FindFirst("th").GetClasses());
                        if (dict.ContainsKey(key))
                        {
                            dict[key].Add(row);
                        }
                        else
                        {
                            dict[key] = new List<HtmlNode> { row };
                        }
                    }
                }
            }

            var moods = new List<Mood>();

            foreach(var group in dict)
            {

                var list = group.Value;
                var moodRow = list.First();
                var personRow = moodRow.SelectNodes("th").Skip(1);
                var moodName = FirstCharToUpper(moodRow.ChildNodes.FindFirst("th").InnerText.Trim());

                var tenseBlocks = list.Skip(1);
                var tenses = new List<Tense>();

                foreach (var block in tenseBlocks)
                {
                    var tenseName = FirstCharToUpper(block.ChildNodes?.FindFirst("th")?.InnerText?.Trim());

                    var conjugations = new List<Conjugation>();

                    var conjugationElements = block.SelectNodes("td");
                    int i = 0;
                    foreach (var node in conjugationElements)
                    {
                        
                        conjugations.Add(
                            new Conjugation(
                                FirstCharToUpper(personRow.ElementAt(i)?.InnerText?.Trim()), 
                                node.InnerText.Trim()
                            ));
                        i++;
                    }
                    tenses.Add(new Tense(tenseName, conjugations));
                }
                moods.Add(new Mood(moodName, tenses));
            }

            return new ConjugationData(moods, specialMoods);
        }
        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return $"{char.ToUpper(input[0])}{input[1..]}";
        }
        private static List<string> CommonMoods = new List<string>
        {
            "indicative",
            "subjunctive",
            "imperative"
        };
    }
    public record ConjugationData(ICollection<Mood> Moods, ICollection<SpecialMood> SpecialMoods);

    public record Conjugation(string Person, string Form);
    public record Tense(string Name, ICollection<Conjugation> Conjugations);
    public record Mood(string Name, ICollection<Tense> Tenses);
    public record SpecialMood(string Name, string Form);
}
