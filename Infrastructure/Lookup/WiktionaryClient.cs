using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Infrastructure.Lookup
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

        public async Task<ConjugationData> GetVerbInflectionData(string verb)
        {
            if (_options.EnableCaching && _cache.TryGetValue(verb, out var cached))
                return JsonSerializer.Deserialize<ConjugationData>(cached.FirstOrDefault()) ?? new ConjugationData(new List<Mood>(), new List<SpecialMood>());

            var url = $"{_options.BaseUrl}{verb}";
            var html = await FetchWithRetryAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Example: //h2/span[@id='Italian']
            var header = doc.DocumentNode.SelectNodes($"//h2[contains(@id,'{_options.Language}')]").FirstOrDefault();

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
