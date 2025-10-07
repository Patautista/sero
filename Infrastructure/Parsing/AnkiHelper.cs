using AnkiNet;
using Business.Model;
using Domain.Entity;
using Domain.Events;
using Infrastructure.Data.Mappers;
using Infrastructure.Data.Model;
using Lingua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lingua.Language;

namespace Infrastructure.Parsing
{
    public class AnkiHelper
    {
        public IEnumerable<CardDefinition> ImportTxt(string filePath, string nativeLanguageCode, string targetLanguageCode)
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

        
        public async Task<ICollection<CardTable>> ReadApkg(string filePath, string nativeLanguageCode, string targetLanguageCode)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tableModels = new List<CardTable>();

            try
            {

                AnkiCollection collection = await AnkiFileReader.ReadFromFileAsync(filePath);
                var deck = collection.Decks.FirstOrDefault(d => d.Name != "Default");
                var deckTable = new DeckTable { Name = deck.Name ?? "Default" };

                var targetLanguage = FromCode(targetLanguageCode);
                var nativeLanguage = FromCode(nativeLanguageCode);
                var detector = LanguageDetectorBuilder
                    .FromLanguages(targetLanguage, nativeLanguage)
                    .Build();

                CardTable cardTable = new();

                foreach (var card in deck.Cards)
                {
                    
                    if (card.Note.Fields.Count() < 2)
                        continue;

                    var CardRevisions = collection.RevisionLogs.Where(revlog => revlog.CardId == card.Id).ToList();
                    var lastRevision = CardRevisions.OrderByDescending(r => r.Id).FirstOrDefault();
                    var dateOfLastRevision = lastRevision != null 
                        ? DateTimeOffset.FromUnixTimeMilliseconds(lastRevision.Id).ToLocalTime().DateTime 
                        : DateTime.MinValue;

                    var nativeSentence = card.Note.Fields.FirstOrDefault(n => detector.DetectLanguageOf(n) == nativeLanguage)?.Trim();
                    var targetSentence = card.Note.Fields.FirstOrDefault(n => detector.DetectLanguageOf(n) == targetLanguage)?.Trim();

                    cardTable = new CardTable
                    {
                        Events = CardRevisions.Select(r =>
                            EventMapper.ToTable(AnkiAnswerEvent with
                            {
                                EllapsedMs = (int)r.TimeTookMs,
                                OccurredAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(lastRevision.Id).ToLocalTime().DateTime
                            }, 
                            Events.CardAnswered.Schemas.CardAnsweredV1))
                        .ToList(),
                        Meaning = new MeaningTable { Sentences = new List<SentenceTable> {
                            new SentenceTable { Language = nativeLanguageCode, Text = nativeSentence },
                            new SentenceTable { Language = targetLanguageCode, Text = targetSentence }
                        }},
                        UserCardState = new SrsCardStateTable { 
                            EaseFactor = lastRevision.Ease,
                            Interval = (int)lastRevision.Interval, 
                            LastReviewed = dateOfLastRevision,
                            NextReview = dateOfLastRevision.AddDays(lastRevision.Interval)
                        },
                        Deck = deckTable
                    };

                    // chave única
                    var key = $"{nativeSentence}||{targetSentence}";
                    if (!seen.Add(key))
                        continue;
                    if (!seen.Add($"{targetSentence}||{nativeSentence}"))
                        continue; // já existe, pula

                    if (string.IsNullOrWhiteSpace(nativeSentence) || string.IsNullOrWhiteSpace(targetSentence))
                        continue;

                    tableModels.Add(cardTable);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            return tableModels;
        }
         
        private Language FromCode(string code)
        {
            return code.ToLower() switch
            {
                "pt" or "por" or "portuguese" => Portuguese,
                "no" or "nob" or "norwegian" => Nynorsk,
                _ => throw new ArgumentException($"Unsupported language code: {code}")
            };
        }
        private string ToCode(Language language)
        {
            return language switch
            {
                Language.Portuguese => "pt",
                Language.Nynorsk => "no",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
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
        public static CardAnsweredEvent AnkiAnswerEvent = new CardAnsweredEvent(Guid.NewGuid(), "TextChallenge", 1, 1000, true);
    }
}
