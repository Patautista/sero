using AnkiNet;
using Business.Model;
using Domain.Entity;
using Domain.Events;
using Infrastructure.Data.Mappers;
using Infrastructure.Data.Model;
using Infrastructure.Services;
using Lingua;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lingua.Language;

namespace Infrastructure.Parsing
{
    public class AnkiHelper
    {

        public async Task<ICollection<CardTable>> ReadApkg(string filePath, string nativeLanguageCode, string targetLanguageCode)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tableModels = new List<CardTable>();

            try
            {

                AnkiCollection collection = await AnkiFileReader.ReadFromFileAsync(filePath);

                var deck = collection.Decks.FirstOrDefault(d => d.Name != "Default");

                var deckTable = new DeckTable { 
                    Name = deck.Name ?? "Default",
                    Description = "Anki",
                    TargetLanguage = targetLanguageCode 
                };


                var targetLanguage = FromCode(targetLanguageCode);
                var nativeLanguage = FromCode(nativeLanguageCode);

                Directory.SetCurrentDirectory(AppContext.BaseDirectory);

                var detector = LanguageDetectorBuilder
                    .FromLanguages(targetLanguage, nativeLanguage)
                    .WithPreloadedLanguageModels()
                    .Build();

                foreach (var card in deck.Cards)
                {
                    
                    if (card.Note.Fields.Count() < 2)
                        continue;

                    var CardRevisions = collection.RevisionLogs.Where(revlog => revlog.CardId == card.Id).ToList();
                    var lastRevision = CardRevisions.OrderByDescending(r => r.Id).FirstOrDefault();
                    var dateOfLastRevision = lastRevision != null 
                        ? DateTimeOffset.FromUnixTimeMilliseconds(lastRevision.Id).ToLocalTime().DateTime 
                        : DateTime.MinValue;

                    string nativeSentence = string.Empty;
                    foreach (var n in card.Note.Fields)
                    {
                        var lang = detector.DetectLanguageOf(n);
                        if (lang == nativeLanguage)
                        {
                            nativeSentence = n.Trim();
                            break;
                        }
                    }

                    string targetSentence = string.Empty;
                    foreach (var n in card.Note.Fields)
                    {
                        var lang = detector.DetectLanguageOf(n);
                        if (lang == targetLanguage)
                        {
                            targetSentence = n.Trim();
                            break;
                        }
                    }

                    var cardTable = new CardTable
                    {
                        Events = CardRevisions.Select(r =>
                         EventMapper.ToTable(CreateAnkiEvent(r.CardId, r.Id, r.TimeTookMs), 
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
        public void ExportTxt(IEnumerable<CardDefinition> cards, string filePath)
        {
            File.WriteAllText(filePath, SaveToString(cards), Encoding.UTF8);
        }
        private static CardAnsweredEvent CreateAnkiEvent(long cardId, long timestamp, long elapsedMs)
            => new(Guid.NewGuid(), "TextChallenge", (int)cardId, (int)elapsedMs, true)
            {
                OccurredAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
            };

        private Language FromCode(string code)
        {
            return code.ToLower() switch
            {
                AvailableCodes.Portuguese or "por" or "portuguese" => Portuguese,
                AvailableCodes.Norwegian or "nob" or "norwegian" => Nynorsk,
                AvailableCodes.Italian or "ita" or "italian" => Italian,
                _ => throw new ArgumentException($"Unsupported language code: {code}")
            };
        }
        private string ToCode(Language language)
        {
            return language switch
            {
                Language.Portuguese => AvailableCodes.Portuguese,
                Language.Nynorsk => AvailableCodes.Norwegian,
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }

        public async Task ExportApkg(DeckTable deck, string outputPath)
        {
            if (deck?.Cards == null)
                throw new ArgumentNullException(nameof(deck));

            var collection = new AnkiCollection();
            var ankiDeck = collection.CreateDeck(deck.Name);

            var cardTypes = new[]
            {
                new AnkiCardType(
                    "Forwards",
                    0,
                    "{{Front}}<br/>",
                    "{{Front}}<hr id=\"answer\">{{Back}}"
                )
            };

            var noteType = new AnkiNoteType(
                "Basic",
                cardTypes,
                new[] { "Front", "Back" }
            );

            var noteTypeId = collection.CreateNoteType(noteType);

            foreach (var cardTable in deck.Cards)
            {
                if (cardTable.Meaning?.Sentences == null || cardTable.Meaning.Sentences.Count < 2)
                    continue;

                var nativeSentence = cardTable.Meaning.Sentences
                    .FirstOrDefault(s => s.Language != deck.TargetLanguage)?.Text;
                var targetSentence = cardTable.Meaning.Sentences
                    .FirstOrDefault(s => s.Language == deck.TargetLanguage)?.Text;

                if (string.IsNullOrWhiteSpace(nativeSentence) || string.IsNullOrWhiteSpace(targetSentence))
                    continue;

                var ids = collection.CreateNote(ankiDeck, noteTypeId, nativeSentence, targetSentence);

                collection.TryGetDeckById(ankiDeck, out var ankiDeckToUse);

                var card = ankiDeckToUse.Cards.Where(c => c.Id == ids.Skip(1).First()).First();

                // If we have SRS state, transfer it
                if (cardTable.UserCardState != null && cardTable.Events != null)
                {
                    foreach(var cardAnswerEvent in cardTable.Events.Where(e => e.Name == nameof(CardAnsweredEvent)).ToArray())
                    {
                        collection.CreateRevisionLog(
                        cardId: card.Id,
                        ease: (long)cardTable.UserCardState.EaseFactor,
                        interval: cardTable.UserCardState.Interval,
                        factor: 2,
                        lastInterval: cardTable.UserCardState.Interval,
                        revisionType: AnkiNet.CollectionFile.Model.RevisionType.Review,
                        timeTookMs: new DateTimeOffset(cardTable.UserCardState.LastReviewed).ToUnixTimeMilliseconds(),
                        createdAtMs: ((DateTimeOffset)cardAnswerEvent.OccurredAtUtc).ToUnixTimeMilliseconds()
                    );
                    }
                }
            }

            await AnkiFileWriter.WriteToFileAsync(outputPath, collection);
        }
    }
}
