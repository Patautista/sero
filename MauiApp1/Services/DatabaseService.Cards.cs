using Business;
using Business.Model;
using Business.ViewModel;
using Domain.Entity;
using Domain.Entity.Specification;
using Domain.Events;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
    public partial class DatabaseService
    {
        private string _nativeCode => settingsService.StudyConfig.Value.SelectedLanguage.Source.TwoLetterISOLanguageName;
        private string _targetCode => settingsService.StudyConfig.Value.SelectedLanguage.Target.TwoLetterISOLanguageName;
        public async Task<ICollection<SrsCard>> GetDueCards(int deckId, ReviewSessionMode sessionMode, int exp, CancellationToken cancellationToken = default)
        {
            var cards = new List<CardTable>();

            try
            {
                // Get all cards from the specified deck
                cards = await db.Cards
                    .Where(c => c.DeckId == deckId)
                    .Include(c => c.Meaning.Tags)
                    .Include(c => c.Meaning.Sentences)
                    .Include(c => c.UserCardState)
                    .Include(c => c.Events)
                    .ToListAsync(cancellationToken);

                // Ensure UserCardState exists
                foreach (var card in cards)
                {
                    if (card.UserCardState == null)
                    {
                        card.UserCardState = new SrsCardStateTable()
                        {
                            CardId = card.Id,
                            UserId = UserTable.Default.Id,
                        };
                        await db.UserCardStates.AddAsync(card.UserCardState, cancellationToken);
                    }
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // Project to domain
            var cardWithStates = cards.Select(x => new SrsCard
            {
                CardId = x.Id,
                StateId = x.UserCardState.Id,
                DifficultyLevel = DifficultyLevelExtensions.FromString(x.Meaning.DifficultyLevel),
                SentencesInNativeLanguage = x.Meaning.Sentences.Where(s => s.Language == _nativeCode).Select(s => s.ToDomain()).ToList(),
                SentencesInTargetLanguage = x.Meaning.Sentences.Where(s => s.Language == _targetCode).Select(s => s.ToDomain()).ToList(),
                Tags = x.Meaning.Tags.Select(t => t.ToDomain()).ToList(),
                Repetitions = x.Events.Where(e => e.Name == nameof(CardAnsweredEvent)).Count(),
                EaseFactor = x.UserCardState.EaseFactor,
                Interval = x.UserCardState.Interval,
                NextReview = x.UserCardState.NextReview,
                LastReviewed = x.UserCardState.LastReviewed,
            }).OrderBy(c => c.DifficultyLevel).ToList();

            var userDifficulty = settingsService.StudyConfig.Value?.DifficultyLevel ?? DifficultyLevel.Advanced;
            var filtered = cardWithStates.ToList();

            // Separate new vs review
            var newCards = filtered.Where(c => c.Repetitions == 0).ToList();
            var reviewCards = filtered.Where(c => c.Repetitions > 0).ToList();

            // Session-specific limits
            (int newLimit, int reviewLimit) = sessionMode switch
            {
                ReviewSessionMode.Quick => (5, 15),
                ReviewSessionMode.Regular => (10, 30),
                ReviewSessionMode.Grind => (20, 60),
                _ => (10, 30)
            };

            // Pick cards
            var sessionCards = new List<SrsCard>();
            sessionCards.AddRange(reviewCards.Take(reviewLimit));
            sessionCards.AddRange(newCards.Take(newLimit));

            return sessionCards.ToList();
        }

        public async Task AddAlternativeSentence(Sentence domainSentence)
        {
            try
            {
                var sentence = new SentenceTable
                {
                    Language = domainSentence.Language,
                    MeaningId = domainSentence.MeaningId,
                    Text = domainSentence.Text
                };
                // Should sanitize/tidy first
                db.Sentences.Add(sentence);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public async Task<ICollection<Tag>> GetTagsAsync()
        {
            var tags = await db.Tags.ToListAsync();
            return tags.Select(t => t.ToDomain()).ToList();
        }
        public async Task CreateCard(CardDefinition cardDefinition, int deckId)
        {
            var cardTable = new CardTable
            {
                Meaning = new MeaningTable
                {
                    DifficultyLevel = cardDefinition.DifficultyLevel.ToString(),
                    Sentences =
                        new List<SentenceTable> {
                            new SentenceTable {
                                Language = cardDefinition.NativeLanguageCode,
                                Text = cardDefinition.NativeSentence
                            },
                            new SentenceTable {
                                Language = cardDefinition.TargetLanguageCode,
                                Text = cardDefinition.TargetSentence
                            },
                        },
                    Tags = cardDefinition.Tags.Select(tag => new TagTable { Name = tag.Name, Type = tag.Type }).ToList()
                },
                DeckId = deckId
            };
            db.Cards.Add(cardTable);
        }
    }
}
