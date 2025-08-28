
using Domain;
using Infrastructure;
using Infrastructure.AI;
using Infrastructure.Parsing;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        for (int i = 0; i < 10; i++) {
            await RunAITagging();
        }
    }
    static async Task ProcessTatoeba()
    {
        string filePath = "tatoeba it-pt.tsv";

        string[][] sentenceMatrix = TsvReader.ReadFile(filePath).DistinctBy(l => l[1]).OrderBy(l=> l[1].Length).ToArray();
        var lastMeaningId = 0;
        var tagMatrix = TsvReader.ReadFile("tags.csv").Where(t => (!t.ElementAtOrDefault(1)?.Contains("syllable") ?? false)
        && (!t.ElementAtOrDefault(1)?.Contains("word") ?? false)
        && (!t.ElementAtOrDefault(1)?.Contains("by") ?? false)
        && (!t.ElementAtOrDefault(1)?.Contains("OK") ?? false)
        && (!t.ElementAtOrDefault(1)?.Contains("WWWJDIC") ?? false)
        && (!t.ElementAtOrDefault(1)?.Contains("kirjakieli") ?? false)
        && (!t.ElementAtOrDefault(1)?.Contains("List") ?? false)).ToArray();

        
        var decideDifficulty = (int i, int total) => {
            if (i <= total * .33)
            {
                return DifficultyLevel.Beginner;
            }
            else if (i >= total * .82)
            {
                return DifficultyLevel.Advanced;
            }
            else
            {
                return DifficultyLevel.Intermediate;
            }
        };

        var cards = sentenceMatrix.AsParallel().Select((l, index) => new Card
        {
            TargetSentence = new Sentence
            {
                MeaningId = lastMeaningId + index + 1,
                Language = "it",
                Text = l[1]
            },
            NativeSentence = new Sentence
            {
                MeaningId = lastMeaningId + index + 1,
                Language = "pt",
                Text = l[3]
            },
            DifficultyLevel = decideDifficulty.Invoke(index, sentenceMatrix.Count()),
            Tags = tagMatrix.Where(t => t[0] == l[0]).Select(t => new Tag { Name = t[1] }).ToList()
        }).ToList();

        var json = JsonSerializer.Serialize(cards, options: new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText("tatoeba-cards.json", json);
    }
    static async Task RunAITagging()
    {
        //var api = new OpenAIClient(new HttpClient(), "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");
        var api = new GeminiClient("AIzaSyD_cIyYXmvyyCGtLJLNVHvpZ3-0JZh5cA0");

        var batchPath = "C:\\Users\\caleb\\source\\repos\\AspireApp1\\ConsoleTests\\tagging";

        string preffix = "tagged-cards-batch"; // the string to look for in file names

        int count = Directory
            .EnumerateFiles(batchPath, "*", SearchOption.TopDirectoryOnly)
            .Count(file => Path.GetFileName(file).Contains(preffix, StringComparison.OrdinalIgnoreCase));

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        var tags = JsonSerializer.Deserialize<List<Tag>>(File.ReadAllText("tags.json"), options: options) ?? new List<Tag>();
        var cards = JsonSerializer.Deserialize<List<Card>>(File.ReadAllText("tatoeba-cards.json"), options: options) ?? new List<Card>();

        var cardBatch = cards.Where(c => c.NativeSentence.Text.Split(" ").Count() > 1).Skip(count * 30).Take(30);

        var sb = new StringBuilder();
        foreach (var card in cardBatch) {
            sb.AppendLine($"Simply assign tags to the highlighted sentence from this tag pool: {string.Join(", ", tags.Select(t => t.Name))}.");
            sb.AppendLine($"Sentence: {card.NativeSentence.Text}");
            sb.AppendLine($"Give no more explanation.");
            var res = await api.GenerateContentAsync(sb.ToString());
            if (string.IsNullOrEmpty(res)) break;

            var selectedTags = tags.AsParallel().Where(t => res.Contains(t.Name.ToLower())).ToList();
            card.Tags = card.Tags.Union(selectedTags).ToList();
            Console.WriteLine(sb.ToString());
            Console.WriteLine(res);
            sb.Clear();
        }

        var json = JsonSerializer.Serialize(cardBatch, options: options);

        File.WriteAllText($"{batchPath}\\{preffix} {count}" + ".json", json);
    }
}
