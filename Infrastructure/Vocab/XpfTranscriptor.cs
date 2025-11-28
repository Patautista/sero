using Infrastructure.Interfaces;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Infrastructure.Vocab;

public class Rule
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("sfrom")]
    public string SFrom { get; set; }
    [JsonPropertyName("sto")]
    public string STo { get; set; }
    [JsonPropertyName("precede")]
    public string Precede { get; set; }
    [JsonPropertyName("follow")]
    public string Follow { get; set; }
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}

public static class StringExtensions
{
    private static readonly Regex DictPattern = new Regex(@"\{[^}]+\}");

    public static string DictFormat(this string s, Dictionary<string, string> dict)
    {
        return DictPattern.Replace(s, match =>
        {
            var key = match.Value.Trim('{', '}');
            return dict.TryGetValue(key, out var val) ? val : match.Value;
        });
    }
}

public class SubRule
{
    public Regex From { get; }
    public string To { get; }
    public Regex Precede { get; }
    public Regex Follow { get; }
    public double Weight { get; }

    public SubRule(Rule rule, Dictionary<string, string> classes)
    {
        string from = rule.SFrom;
        string to = rule.STo;
        string precede = rule.Precede;
        string follow = rule.Follow;

        // Formatação de classes para todos os campos
        while (Regex.IsMatch(from, @"\{.*?\}"))
            from = from.DictFormat(classes);
        while (Regex.IsMatch(to, @"\{.*?\}"))
            to = to.DictFormat(classes);
        while (Regex.IsMatch(precede, @"\{.*?\}"))
            precede = precede.DictFormat(classes);
        while (Regex.IsMatch(follow, @"\{.*?\}"))
            follow = follow.DictFormat(classes);

        // Boundaries Python ? JS ? C# equivalentes
        from = from.Replace(@"\b$", "(?=\\s|$)")
                   .Replace(@"^\b", "(?<=^|\\s)");

        // Backreferences estilo JS ? C# equivalentes
        // Convert \1, \2, etc. to $1, $2, etc. for C# Regex
        // $$$ = $$ (literal $) + $ (start of backreference), then 1 = capture group 1
        to = Regex.Replace(to, @"\\([1-9])", "$$$1");

        From = new Regex(from, RegexOptions.Compiled);
        Precede = new Regex(precede + "$", RegexOptions.Compiled);
        Follow = new Regex("^" + follow, RegexOptions.Compiled);
        To = to;

        Weight = rule.Weight;
    }

    public double? Score(string sfrom, string precede, string follow)
    {
        if (From.IsMatch(sfrom) &&
            Precede.IsMatch(precede) &&
            Follow.IsMatch(follow))
            return Weight;

        return null;
    }

    public string Apply(string input)
    {
        return From.Replace(input, To);
    }
}

/// <summary>
/// XPF (eXtensible Phonetic Framework) Transcriptor for converting orthographic text to phonetic transcriptions (IPA)
/// </summary>
public class XpfTranscriptor : ITranscriptionProvider
{
    private readonly string rulesContent;

    private readonly Dictionary<string, string> classes = new();
    private readonly Dictionary<string, string> matches = new();
    private readonly Dictionary<string, string[]> words = new();
    private readonly List<(string, string)> pre = new();

    private readonly List<SubRule> subs = new();
    private readonly List<SubRule> ipasubs = new();

    private const string NO_TRANSLATE = "@";

    public string ProviderName { get; }
    public string LanguageCode { get; }
    public string TranscriptionType => "XPF";

    /// <summary>
    /// Creates a new XpfTranscriptor instance from a local file path
    /// </summary>
    public XpfTranscriptor(string rulesPath, string providerName = "XpfTranscriptor", string languageCode = "unknown")
    {
        this.ProviderName = providerName;
        this.LanguageCode = languageCode;
        this.rulesContent = File.ReadAllText(rulesPath);
        var rules = LoadRules(rulesContent);
        Init(rules);
    }

    /// <summary>
    /// Creates a new XpfTranscriptor instance from a remote URI
    /// </summary>
    public XpfTranscriptor(Uri rulesUri, string providerName = "XpfTranscriptor", string languageCode = "unknown")
    {
        this.ProviderName = providerName;
        this.LanguageCode = languageCode;
        this.rulesContent = DownloadRulesAsync(rulesUri).GetAwaiter().GetResult();
        var rules = LoadRules(rulesContent);
        Init(rules);
    }

    private async Task<string> DownloadRulesAsync(Uri uri)
    {
        using (var client = new HttpClient())
        {
            return await client.GetStringAsync(uri);
        }
    }

    private void Init(List<Rule> rules)
    {
        foreach (var r in rules)
        {
            switch (r.Type)
            {
                case "pre":
                    pre.Add((r.SFrom, r.STo));
                    break;

                case "class":
                    classes[r.SFrom] = r.STo;
                    break;

                case "match":
                    string v = r.STo;
                    while (Regex.IsMatch(v, @"\{.*?\}"))
                        v = v.DictFormat(classes);
                    matches[r.SFrom] = v;
                    break;

                case "sub":
                    subs.Add(new SubRule(r, classes));
                    break;

                case "ipasub":
                    var ip = new SubRule(r, classes);
                    ipasubs.Add(ip);
                    break;

                case "word":
                    words[r.SFrom] = r.STo.Split();
                    break;
            }
        }
    }

    public bool SupportsLanguage(string languageCode)
    {
        return this.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase) || 
               this.LanguageCode == "unknown";
    }

    public Task<string?> GetTranscriptionAsync(string word)
    {
        var result = Translate(word);
        var concatenated = string.Join("", result);
        return Task.FromResult<string?>(concatenated);
    }

    public Task<string?> GetTranscriptionAsync(IEnumerable<string> words)
    {
        var allTranscriptions = new List<string>();
        foreach (var word in words)
        {
            var result = Translate(word);
            allTranscriptions.Add(string.Join("", result));
        }
        var concatenated = string.Join(" ", allTranscriptions);
        return Task.FromResult<string?>(concatenated);
    }

    /// <summary>
    /// Translates a word from orthographic form to phonetic transcription
    /// </summary>
    public string[] Translate(string source)
    {
        if (words.TryGetValue(source, out var saved))
            return saved;

        // Clean punctuation marks
        source = source.RemovePunctuation().Trim();

        // Pré-processamento
        foreach (var (from, to) in pre)
            source = Regex.Replace(source, from, to);

        source = source.ToLowerInvariant();

        var srcList = source.ToCharArray().Select(c => c.ToString()).ToArray();
        var tgtList = new List<string>();

        for (int i = 0; i < srcList.Length; i++)
        {
            string letter = srcList[i];

            if (matches.TryGetValue(letter, out var match))
            {
                tgtList.Add(match);
                continue;
            }

            string precede = string.Concat(srcList.Take(i));
            string follow = string.Concat(srcList.Skip(i + 1));

            var candidates = subs
                .Select(s => (score: s.Score(letter, precede, follow), value: s))
                .Where(x => x.score.HasValue)
                .OrderByDescending(x => x.score)
                .ToList();

            if (candidates.Count > 0)
            {
                var best = candidates.First().value.Apply(letter);
                if (best.Length > 0)
                    tgtList.Add(best);
            }
            else
            {
                tgtList.Add(NO_TRANSLATE);
            }
        }

        string targetString = string.Join(" ", tgtList);

        // IPA phase
        foreach (var ip in ipasubs.OrderByDescending(i => i.Weight))
            targetString = ip.From.Replace(targetString, ip.To);

        return targetString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private List<Rule> LoadRules(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var list = new List<Rule>();

        string[] headers = lines.First(l => l.Length > 0 && !l.StartsWith("#")).Split(',');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var cols = line.Split(',');
            if (cols[0] == headers[0]) // pula o header
                continue;

            var r = new Rule();
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i];
                string v = cols.Length > i ? cols[i] : "";

                var prop = typeof(Rule).GetProperties().Where(p => p.Name.ToLower() == h).FirstOrDefault();
                if (prop == null) continue;

                object converted = v;

                if (prop.PropertyType == typeof(double))
                {
                    converted = double.TryParse(v, out double d) ? d : 0.0;
                }
                else if (prop.PropertyType == typeof(int))
                {
                    converted = int.TryParse(v, out int num) ? num : 0;
                }
                // string não precisa de conversão

                prop.SetValue(r, converted);
            }
            if (double.TryParse(r.Weight.ToString(), out double w))
                r.Weight = w;

            list.Add(r);
        }

        return list;
    }
}
