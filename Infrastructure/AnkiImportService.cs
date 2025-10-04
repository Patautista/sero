using Business.Model;
using Domain.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class AnkiImportService
    {
        public IEnumerable<CardDefinition> Import(string filePath, string nativeLanguageCode, string targetLanguageCode)
        {
            var lines = File.ReadAllLines(filePath);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 2) continue;

                var native = parts[1].Trim();
                var target = parts[0].Trim();

                // chave única
                var key = $"{native}||{target}";
                if (!seen.Add(key))
                    continue;
                if (!seen.Add($"{target}||{native}"))
                    continue; // já existe, pula

                yield return new CardDefinition
                {
                    NativeLanguageCode = nativeLanguageCode,
                    TargetLanguageCode = targetLanguageCode,
                    NativeSentence = native,
                    TargetSentence = target,
                    Tags = new List<Tag>(),
                    DifficultyLevel = DifficultyLevel.Unknown,
                };
            }
        }

        public string SaveToString(IEnumerable<CardDefinition> cards)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = new List<string>();

            foreach (var card in cards)
            {
                if (string.IsNullOrWhiteSpace(card.NativeSentence) || string.IsNullOrWhiteSpace(card.TargetSentence))
                    continue;

                var native = card.NativeSentence.Trim();
                var target = card.TargetSentence.Trim();

                var key = $"{native}||{target}";
                if (!seen.Add(key))
                    continue;
                if (!seen.Add($"{target}||{native}"))
                    continue; // skip duplicates

                // Export in the same format: target \t native
                lines.Add($"{target}\t{native}");
            }
            return string.Join(Environment.NewLine, lines);
        }

        public void Export(IEnumerable<CardDefinition> cards, string filePath)
        {
            File.WriteAllText(filePath, SaveToString(cards), Encoding.UTF8);
        }
    }
}
