using Domain.Entity;
using Domain.Entity.Specification;
using Infrastructure;
using Infrastructure.AI;
using Infrastructure.ETL;
using Infrastructure.Parsing;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;

class Program
{
    static string DeepSeek14BQ = "deepseek-r1:14b-qwen-distill-q4_K_M";
    static string DeepSeek14Q_LLamaFile = "DeepSeek-R1-Distill-Qwen-14B-Q4_K_M";
    static string DeepSeek14B_Pure = "deepseek-r1:14b";
    static string DeepSeek8B = "deepseek-r1:8b";
    static string GeminiFlash = "gemini-2.5-flash";
    static async Task Main()
    {
        var spec = new UntypedPropertySpecificationDto(nameof(User.Id), MatchOperator.Equals, 1);
        var json = JsonSerializer.Serialize(spec);
        Console.WriteLine(json);

        var expr = SpecificationExpressionFactory.ToExpression<User>(spec);
        var predicate = expr.Compile();
        var matches = predicate(new User { Id = 1 });
        Console.WriteLine($"Matches? {matches}"); // True
    }
    static async Task NormalizeTatoeba()
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
    static async Task RunAITagging(string dataset)
    {
        //var api = new GeminiClient("AIzaSyD_cIyYXmvyyCGtLJLNVHvpZ3-0JZh5cA0");
        var api = new OllamaClient();
        //var api = new LlamafileClient();
        var service = new TaggingService(DeepSeek14B_Pure, api);

        //var service = new TaggingService("deepseek-r1:14b-qwen-distill-q4_K_M", api);

        var batchPath = "C:\\Users\\caleb\\source\\repos\\AspireApp1\\ConsoleTests\\etl\\2 tagged";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        var tags = JsonSerializer.Deserialize<List<Tag>>(File.ReadAllText("tags.json"), options: options) ?? new List<Tag>();
        var cards = JsonSerializer.Deserialize<List<Card>>(File.ReadAllText($"C:\\Users\\caleb\\source\\repos\\AspireApp1\\ConsoleTests\\etl\\1 normalized\\{dataset}"), options: options) ?? new List<Card>();

        await service.RunAITagging(batchPath, tags, cards);
    }
    static void SummarizeTatoebaCards()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
        var cards = JsonSerializer.Deserialize<List<Card>>(File.ReadAllText("C:\\Users\\caleb\\source\\repos\\AspireApp1\\ConsoleTests\\etl\\1 normalized\\tatoeba-full.json"), options: options) ?? new List<Card>();
        cards = cards.Where(c => c.NativeSentence.Text.Split(" ").Count() > 1)
            .DistinctBy(c => c.NativeSentence.Text)
            .DistinctBy(c => c.TargetSentence.Text).ToList();
        var beginner = cards
           .Where(c => c.DifficultyLevel == DifficultyLevel.Beginner)
           .Take(640); // 540 + 100
        var intermediate = cards
            .Where(c => c.DifficultyLevel == DifficultyLevel.Intermediate)
            .Skip(200)
            .Take(500);
        var advanced = cards
            .Where(c => c.DifficultyLevel == DifficultyLevel.Advanced)
            .Take(350);

        var result = beginner
            .Concat(intermediate)
            .Concat(advanced)
            .ToList();

        var json = JsonSerializer.Serialize(result, options: options);

        File.WriteAllText($"C:\\\\Users\\\\caleb\\\\source\\\\repos\\\\AspireApp1\\\\ConsoleTests\\\\etl\\\\1 normalized\\\\tatoeba-summarized.json", json);
    }
    static void JoinTaggedBatches()
    {
        var batchPath = "C:\\Users\\caleb\\source\\repos\\AspireApp1\\ConsoleTests\\etl\\2 tagged";
        var files = Directory
                .EnumerateFiles(batchPath, $"{TaggingService.Prefix}*.json", SearchOption.TopDirectoryOnly).ToList();

        var cards = new List<Card>();
        foreach (var file in files) {
            var batchResult = JsonSerializer.Deserialize<TaggingBatchResult>(File.ReadAllText(file));
            cards.AddRange(batchResult.Cards);
        }
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
        var json = JsonSerializer.Serialize(cards, options: options);

        File.WriteAllText($"C:\\\\Users\\\\caleb\\\\source\\\\repos\\\\AspireApp1\\\\ConsoleTests\\\\etl\\\\3 joined\\\\tatoeba-tagged-joined.json", json);
    }
}
