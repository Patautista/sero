using Business;
using Business.Model;
using Business.ViewModel;
using Domain.Entity;
using Domain.Entity.Specification;
using Domain.Events;
using Infrastructure.Data.Mappers;
using Infrastructure.Data.Model;
using Infrastructure.Parsing;
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
        
        public async Task<(int newCards, int reviewCards)> GetDeckCardCountsAsync(int deckId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cards = await db.Cards
                    .Where(c => c.DeckId == deckId)
                    .Include(c => c.UserCardState)
                    .Include(c => c.Events)
                    .ToListAsync(cancellationToken);

                // Ensure UserCardState exists for all cards
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

                // Count new vs review cards
                var newCards = cards.Count(c => !c.Events.Any(e => e.Name == nameof(CardAnsweredEvent)));
                var reviewCards = cards.Count(c => c.Events.Any(e => e.Name == nameof(CardAnsweredEvent)));

                return (newCards, reviewCards);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (0, 0);
            }
        }
        
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
                CreatedIn = x.CreatedIn
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

        /// <summary>
        /// Gets existing tags from the database or creates new ones if they don't exist.
        /// Returns tracked TagTable entities ready to be associated with a card.
        /// </summary>
        private async Task<List<TagTable>> GetOrCreateTagsAsync(ICollection<Tag> tags)
        {
            if (tags == null || !tags.Any())
                return new List<TagTable>();

            var result = new List<TagTable>();
            var tagNames = tags.Select(t => t.Name.Trim().ToLower()).Distinct().ToList();

            // Get existing tags from database
            var existingTags = await db.Tags
                .Where(t => tagNames.Contains(t.Name))
                .ToListAsync();

            var existingTagNames = new HashSet<string>(existingTags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);

            // Add existing tags
            result.AddRange(existingTags);

            // Create new tags for those that don't exist
            foreach (var tag in tags)
            {
                var normalizedName = tag.Name.Trim().ToLower();
                if (!existingTagNames.Contains(normalizedName))
                {
                    var newTag = new TagTable
                    {
                        Name = normalizedName,
                        Type = string.IsNullOrWhiteSpace(tag.Type) ? "learningTopic" : tag.Type
                    };
                    db.Tags.Add(newTag);
                    result.Add(newTag);
                    existingTagNames.Add(normalizedName); // Prevent duplicates in the same batch
                }
            }

            return result;
        }

        public async Task<SrsCard> CreateCard(CardDefinition cardDefinition, int deckId)
        {
            var tagTables = await GetOrCreateTagsAsync(cardDefinition.Tags);

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
                    Tags = tagTables,
                },
                Events = new List<EventTable>(),
                UserCardState = new(),
                DeckId = deckId
            };
            db.Cards.Add(cardTable);
            await db.SaveChangesAsync();

            return new SrsCard
            {
                CardId = cardTable.Id,
                StateId = cardTable.UserCardState.Id,
                DifficultyLevel = DifficultyLevelExtensions.FromString(cardTable.Meaning.DifficultyLevel),
                SentencesInNativeLanguage = cardTable.Meaning.Sentences.Where(s => s.Language == _nativeCode).Select(s => s.ToDomain()).ToList(),
                SentencesInTargetLanguage = cardTable.Meaning.Sentences.Where(s => s.Language == _targetCode).Select(s => s.ToDomain()).ToList(),
                Tags = cardTable.Meaning.Tags?.Select(t => t.ToDomain()).ToList(),
                Repetitions = cardTable.Events.Where(e => e.Name == nameof(CardAnsweredEvent)).Count(),
                EaseFactor = cardTable.UserCardState.EaseFactor,
                Interval = cardTable.UserCardState.Interval,
                NextReview = cardTable.UserCardState.NextReview,
                LastReviewed = cardTable.UserCardState.LastReviewed
            };
        }

        public async Task<List<string>> GetAllForeignSentencesAsync(string langCode)
        {
            var sentences = await db.Sentences.Where(sentence => sentence.Language == langCode).ToListAsync();
            return sentences.Select(s => s.Text).Distinct().ToList();
        }

        public async Task<bool> DeleteCardAsync(int cardId)
        {
            try
            {
                var card = await db.Cards
                    .Include(c => c.Meaning)
                    .Include(c => c.UserCardState)
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.Id == cardId);

                if (card == null)
                    return false;

                // Remove related entities if needed
                db.Cards.Remove(card);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
        public async Task<bool> UpdateCardAsync(int cardId, CardDefinition updatedDefinition)
        {
            try
            {
                var card = await db.Cards
                    .Include(c => c.Meaning)
                    .Include(c => c.Meaning.Sentences)
                    .Include(c => c.Meaning.Tags)
                    .FirstOrDefaultAsync(c => c.Id == cardId);

                if (card == null)
                    return false;

                // Update sentences
                var nativeSentence = card.Meaning.Sentences.FirstOrDefault(s => s.Language == updatedDefinition.NativeLanguageCode);
                var targetSentence = card.Meaning.Sentences.FirstOrDefault(s => s.Language == updatedDefinition.TargetLanguageCode);

                if (nativeSentence != null)
                    nativeSentence.Text = updatedDefinition.NativeSentence;
                if (targetSentence != null)
                    targetSentence.Text = updatedDefinition.TargetSentence;

                // Update tags - get or create tags
                var tagTables = await GetOrCreateTagsAsync(updatedDefinition.Tags);
                card.Meaning.Tags.Clear();
                foreach (var tag in tagTables)
                {
                    card.Meaning.Tags.Add(tag);
                }

                // Update difficulty
                card.Meaning.DifficultyLevel = updatedDefinition.DifficultyLevel.ToString();

                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public async Task<ICollection<SrsCard>> QuerySrsCardsAsync(
            int deckId,
            Func<SrsCard, bool>? filter = null,
            CancellationToken cancellationToken = default)
        {
            var cards = new List<CardTable>();

            try
            {
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
            }).OrderBy(c => c.DifficultyLevel);

            // Apply filter if provided
            var filtered = filter != null
                ? cardWithStates.Where(filter).ToList()
                : cardWithStates.ToList();

            return filtered;
        }
    }
}
