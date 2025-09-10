using Domain.Entity;
using Infrastructure.AI;
using Infrastructure.ETL.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.ETL.Services
{
    public class TaggingService
    {
        private readonly IPromptClient _api;
        private readonly string _defaultModel;
        public static string Prefix = "tagged-cards-batch";

        public TaggingService(string defaultModel, IPromptClient api)
        {
            _api = api;
            _defaultModel = defaultModel;
        }

        public async Task RunAITagging(string batchPath, List<Tag> tags, List<CardSeed1> cards)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

            // find the most recent batch file
            var latestFile = Directory
                .EnumerateFiles(batchPath, $"{Prefix}*.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetCreationTimeUtc)
                .FirstOrDefault();

            TaggingBatchResult? latestBatch = null;
            if (latestFile != null)
            {
                var content = File.ReadAllText(latestFile);
                latestBatch = JsonSerializer.Deserialize<TaggingBatchResult>(content, options);
            }

            List<CardSeed1> cardBatch;

            if (latestBatch != null && latestBatch.Status.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Resuming incomplete batch...");

                // Use the same cards as before
                cardBatch = latestBatch.Cards;

                // Filter only those without tags
                cardBatch = cardBatch.Where(c => c.Tags == null || !c.Tags.Any()).ToList();
            }
            else
            {
                // Count how many complete files exist
                int completeCount = Directory
                    .EnumerateFiles(batchPath, $"{Prefix}*.json", SearchOption.TopDirectoryOnly)
                    .Count();

                // pick next 30 cards
                cardBatch = cards
                    .Where(c => c.NativeSentence.Text.Split(" ").Length > 1)
                    .Skip(completeCount * 30)
                    .Take(30)
                    .ToList();
            }

            if (cardBatch.Count == 0)
            {
                Console.WriteLine("No cards to tag.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            foreach (var (card, index) in cardBatch.Select((c, i) => (c, i + 1)))
            {
                Console.WriteLine($"Card #{index} is being tagged at {DateTime.Now.ToString("H:mm")}...");
                var res = await TagCard(card, tags);
                if (!res)
                {
                    var processedCards = cardBatch.Where((c, i) => i < cardBatch.IndexOf(card)).ToList();
                    if(processedCards.Count() > 0)
                    {
                        var incompleteBatchResult = new TaggingBatchResult
                        {
                            Schema = "anki-tagging-v1",
                            Status = "incomplete",
                            FinishTime = DateTime.UtcNow,
                            DurationMs = stopwatch.ElapsedMilliseconds,
                            BatchSize = cardBatch.Count,
                            Cards = processedCards
                        };

                        var incompleteJsonOut = JsonSerializer.Serialize(incompleteBatchResult, options);
                        var incompleteFileIndex = Directory
                            .EnumerateFiles(batchPath, $"{Prefix}*.json", SearchOption.TopDirectoryOnly)
                            .Count();

                        File.WriteAllText(Path.Combine(batchPath, $"{Prefix} {incompleteFileIndex}.json"), incompleteJsonOut);
                    }
                }
            }

            stopwatch.Stop();

            var batchResult = new TaggingBatchResult
            {
                Schema = "anki-tagging-v1",
                Status = "complete",
                FinishTime = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                BatchSize = cardBatch.Count,
                Cards = cardBatch
            };

            var jsonOut = JsonSerializer.Serialize(batchResult, options);
            var fileIndex = Directory
                .EnumerateFiles(batchPath, $"{Prefix}*.json", SearchOption.TopDirectoryOnly)
                .Count();

            File.WriteAllText(Path.Combine(batchPath, $"{Prefix} {fileIndex}.json"), jsonOut);
        }

        /// <summary>
        /// Tags a single card using the LLM.
        /// </summary>
        public async Task<bool> TagCard(CardSeed1 card, List<Tag> tags)
        {
            var sb = new StringBuilder();
            tags = FilterRelevantTags(tags, card);

            if (_api is OllamaClient || _api is LlamafileClient)
            {
                var tagPool = string.Join(", ", tags.Select(t => t.Name));

                sb.AppendLine("You are a tagging assistant.");
                sb.AppendLine("Extract only relevant tags from the following sentence.");
                sb.AppendLine($"Use **only** tags from this pool: {tagPool}");
                sb.AppendLine("Output the tags as a comma-separated list.");
                sb.AppendLine("Do NOT provide explanations or commentary.");
                sb.AppendLine("Think briefly (max 3–4 steps) before answering.");
                sb.AppendLine("Ignore the sentence's language; just match tags literally.");
                sb.AppendLine();
                sb.AppendLine($"Sentence: \"{card.NativeSentence.Text}\"");
            }
            else if (_api is GeminiClient)
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
                return true;
            }
            else {
                return false;
            }
        }

        public List<Tag> FilterRelevantTags(List<Tag> tags, CardSeed1 card)
        {
            var cardlimitIndex = tags.IndexOf(new Tag { Name = (card.DifficultyLevel + 1).ToString().ToLower() });
            if (card.DifficultyLevel == DifficultyLevel.Advanced)
            {
                cardlimitIndex = tags.Count;
            }

            return tags
                .Where((tag, i) => tag.Type.Equals(TagTypes.LearningTopic, StringComparison.OrdinalIgnoreCase) || i < cardlimitIndex)
                .Where(t => !t.Type.Equals(TagTypes.Difficulty, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// Wraps a tagging batch with metadata.
    /// </summary>
    public class TaggingBatchResult
    {
        public string Schema { get; set; } = "";
        public string Status { get; set; } = "incomplete";
        public DateTime FinishTime { get; set; }
        public long DurationMs { get; set; }
        public int BatchSize { get; set; }
        public List<CardSeed1> Cards { get; set; } = new();
    }
}
