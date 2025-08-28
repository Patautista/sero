using Domain;
using Infrastructure.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.ETL
{
    public class TaggingService
    {
        public static async Task RunAITagging(string batchPath, GeminiClient api, List<Tag> tags, List<Card> cards)
        {
            string preffix = "tagged-cards-batch"; // the string to look for in file names

            int count = Directory
                .EnumerateFiles(batchPath, "*", SearchOption.TopDirectoryOnly)
                .Count(file => Path.GetFileName(file).Contains(preffix, StringComparison.OrdinalIgnoreCase));

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

            var cardBatch = cards.Where(c => c.NativeSentence.Text.Split(" ").Count() > 1).Skip(count * 30).Take(30);

            var sb = new StringBuilder();
            foreach (var card in cardBatch)
            {
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
}
