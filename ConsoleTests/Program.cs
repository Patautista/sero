
using Domain;
using Infrastructure;
using Infrastructure.AI;
using Infrastructure.ETL;
using Infrastructure.Parsing;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        await RunAITagging();
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
        //var api = new GeminiClient("AIzaSyD_cIyYXmvyyCGtLJLNVHvpZ3-0JZh5cA0");
        var api = new OllamaClient();
        var batchPath = "C:\\Users\\caleb\\source\\repos\\AspireApp1\\ConsoleTests\\etl\\2 tagged";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
        var service = new TaggingService("deepseek-llm:latest", api);

        var tags = JsonSerializer.Deserialize<List<Tag>>(File.ReadAllText("tags.json"), options: options) ?? new List<Tag>();
        var cards = JsonSerializer.Deserialize<List<Card>>(File.ReadAllText("tatoeba-cards.json"), options: options) ?? new List<Card>();

        await service.RunAITagging(batchPath, tags, cards);
    }
}
