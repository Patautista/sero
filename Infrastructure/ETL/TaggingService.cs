using Domain;
using Infrastructure.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Text;

namespace Infrastructure.ETL
{
    public class TaggingService
    {
        private readonly IPromptClient _api;
        private readonly string _defaultModel;

        public TaggingService(string defaultModel, IPromptClient api)
        {
            _api = api;
            _defaultModel = defaultModel;
        }

        public async Task RunAITagging(string batchPath, List<Tag> tags, List<Card> cards)
        {
            string preffix = "tagged-cards-batch";

            int count = Directory
                .EnumerateFiles(batchPath, "*", SearchOption.TopDirectoryOnly)
                .Count(file => Path.GetFileName(file).Contains(preffix, StringComparison.OrdinalIgnoreCase));

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

            var cardBatch = cards
                .Where(c => c.NativeSentence.Text.Split(" ").Count() > 1)
                .Skip(count * 30)
                .Take(30)
                .ToList();

            var stopwatch = Stopwatch.StartNew();

            foreach (var card in cardBatch)
            {
                await TagCard(card, tags);
            }

            stopwatch.Stop();

            var batchResult = new TaggingBatchResult
            {
                Schema = "anki-tagging-v1",
                FinishTime = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                BatchSize = cardBatch.Count,
                Cards = cardBatch
            };

            var json = JsonSerializer.Serialize(batchResult, options: options);

            File.WriteAllText($"{batchPath}\\{preffix} {count}.json", json);
        }

        /// <summary>
        /// Tags a single card using the LLM.
        /// </summary>
        public async Task TagCard(Card card, List<Tag> tags)
        {
            var sb = new StringBuilder();

            if (_api is OllamaClient)
            {
                var tagPool = string.Join(", ", tags.Select(t => t.Name));

                sb.AppendLine("You are a tagging assistant.");
                sb.AppendLine("Extract only relevant tags from the following sentence.");
                sb.AppendLine($"Use **only** tags from this pool: {tagPool}");
                sb.AppendLine("Do NOT invent any tags outside this pool.");
                sb.AppendLine("Output the tags as a comma-separated list.");
                sb.AppendLine("Do NOT provide explanations or commentary.");
                sb.AppendLine("Ignore the sentence's language; just match tags literally.");
                sb.AppendLine();
                sb.AppendLine($"Sentence: \"{card.NativeSentence.Text}\"");
            }
            else if(_api is GeminiClient)
            {
                sb.AppendLine($"Return only relevant tags to the highlighted sentence from this tag pool: {string.Join(", ", tags.Select(t => t.Name))}.");
                sb.AppendLine();
                sb.AppendLine($"Sentence: \"{card.NativeSentence.Text}\"");
                sb.AppendLine();
                sb.AppendLine("Give no more explanation.");
            }

            var res = await _api.GenerateAsync(sb.ToString(), model: _defaultModel);

            if (!string.IsNullOrEmpty(res))
            {
                var selectedTags = tags.AsParallel().Where(t => res.Contains(t.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                card.Tags = card.Tags.Union(selectedTags).ToList();

                Console.WriteLine(sb.ToString());
                Console.WriteLine("\n");
                Console.WriteLine(string.Join(", ", selectedTags.Select(t => t.Name)));
                Console.WriteLine("\n");
            }
        }
    }

    /// <summary>
    /// Wraps a tagging batch with metadata.
    /// </summary>
    public class TaggingBatchResult
    {
        public string Schema { get; set; } = "";
        public DateTime FinishTime { get; set; }
        public long DurationMs { get; set; }
        public int BatchSize { get; set; }
        public List<Card> Cards { get; set; } = new();
    }

}
