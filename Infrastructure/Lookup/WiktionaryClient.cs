using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            result = TableRefactor.ConvertTableToMobileLayout(result);

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
    public class TableRefactor
    {
        public static string ConvertTableToMobileLayout(string htmlContent)
        {
            // 1. Load the original HTML content into an HtmlDocument.
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 2. Find the main table body.
            var tableBody = doc.DocumentNode.SelectSingleNode("//table/tbody");
            if (tableBody == null)
            {
                return string.Empty; // Return an empty string if the table is not found.
            }

            var sb = new StringBuilder();

            // 3. Start building the new mobile layout div.
            sb.AppendLine(@"<div class=""md:hidden p-4 space-y-4 rounded-lg bg-white shadow-xl"">");

            // 4. Extract and convert the initial simple rows (infinitive, participles, etc.).
            var rows = tableBody.SelectNodes("tr").ToList();

            // Infinitive row (index 0)
            var infinitiveRow = rows[0];
            var infinitiveTitle = infinitiveRow.SelectSingleNode("th").InnerText.Trim();
            var infinitiveValue = infinitiveRow.SelectSingleNode("td").InnerText.Trim();
            sb.AppendLine($@"<div class=""bg-gray-50 p-4 rounded-lg shadow-sm"">
            <div class=""font-semibold text-lg text-gray-800"">{infinitiveTitle}</div>
            <div class=""text-blue-600 font-bold text-2xl mt-1"">{infinitiveValue}</div>
        </div>");

            // Auxiliary & Gerund row (index 1)
            var auxRow = rows[1];
            var auxTitle = auxRow.SelectNodes("th")[0].InnerText.Trim();
            var auxValue = auxRow.SelectNodes("td")[0].InnerText.Trim();
            var gerundTitle = auxRow.SelectNodes("th")[1].InnerText.Trim();
            var gerundValue = auxRow.SelectNodes("td")[1].InnerText.Trim();
            sb.AppendLine($@"<div class=""bg-gray-50 p-4 rounded-lg shadow-sm grid grid-cols-2 gap-4"">
            <div>
                <div class=""font-semibold text-base text-gray-800"">{auxTitle}</div>
                <div class=""text-blue-600 font-bold"">{auxValue}</div>
            </div>
            <div>
                <div class=""font-semibold text-base text-gray-800"">{gerundTitle}</div>
                <div class=""text-blue-600 font-bold"">{gerundValue}</div>
            </div>
        </div>");

            // Present & Past Participle row (index 2)
            var participleRow = rows[2];
            var presentParticipleTitle = participleRow.SelectNodes("th")[0].InnerText.Trim();
            var presentParticipleValue = participleRow.SelectNodes("td")[0].InnerText.Trim();
            var pastParticipleTitle = participleRow.SelectNodes("th")[1].InnerText.Trim();
            var pastParticipleValue = participleRow.SelectNodes("td")[1].InnerText.Trim();
            sb.AppendLine($@"<div class=""bg-gray-50 p-4 rounded-lg shadow-sm grid grid-cols-2 gap-4"">
            <div>
                <div class=""font-semibold text-base text-gray-800"">{presentParticipleTitle}</div>
                <div class=""text-gray-500 font-bold"">{presentParticipleValue}</div>
            </div>
            <div>
                <div class=""font-semibold text-base text-gray-800"">{pastParticipleTitle}</div>
                <div class=""text-blue-600 font-bold"">{pastParticipleValue}</div>
            </div>
        </div>");

            // 5. Handle the main conjugation blocks (Indicative, Subjunctive, etc.).
            // Find the first header row for the conjugations (e.g., "indicative" or "congiuntivo").
            var lastPersonLabelsRow = rows
                .Last(row => row.ChildNodes.Any(child => child.GetAttributeValue("class", null) == "roa-person-number-header"));

            var dict = new Dictionary<string, List<HtmlNode>>();
            foreach ( var row in rows.Skip(5))
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

            foreach(var group in dict)
            {
                var list = group.Value;
                var moodRow = list.First();
                var personRow = moodRow.SelectNodes("th").Skip(1);
                var mood = moodRow.ChildNodes.FindFirst("th").InnerText.Trim();

                sb.AppendLine($@"<div class=""bg-gray-100 p-4 rounded-lg shadow-md"">
                    <h3 class=""font-bold text-2xl mb-4 text-center text-gray-900"">{FirstCharToUpper(mood)}</h3>
                    <div class=""space-y-4"">");

                var tenses = list.Skip(1);

                foreach (var tense in tenses)
                {
                    var tenseName = FirstCharToUpper(tense.ChildNodes?.FindFirst("th")?.InnerText?.Trim());
                    if (!string.IsNullOrEmpty(tenseName))
                    {
                        sb.AppendLine($@"
                        <div>
                            <div class=""font-bold text-lg mb-1 text-gray-700"">{tenseName}</div>
                            <div class=""grid grid-cols-2 gap-y-2 text-base"">");
                        sb.AppendLine($@"
                            </div>
                        </div>");
                    }

                    var conjugationElements = tense.SelectNodes("td");
                    int i = 0;
                    foreach (var node in conjugationElements)
                    {
                        sb.AppendLine($@"
                                <div class = ""flex flex-row gap-2"">
                                    <div class=""text-gray-500"">{FirstCharToUpper(personRow.ElementAt(i)?.InnerText?.Trim())}</div>
                                    <div class=""text-blue-600 font-semibold"">{node.InnerText.Trim()}</div>
                                </div>");
                        
                        i++;
                    }
                    
                }
                sb.AppendLine($@"
                            </div>
                        </div>");
            }

            return sb.ToString();
        }
        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return $"{char.ToUpper(input[0])}{input[1..]}";
        }
    }

    public record Conjugation(string person, string conjugation);
    public record Tense(string Name, ICollection<Conjugation> Conjugations);
    public record Mood(string Name, ICollection<Tense> Tenses);

}
