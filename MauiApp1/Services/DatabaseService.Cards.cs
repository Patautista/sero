using Business;
using Business.ViewModel;
using Domain.Entity;
using Domain.Entity.Specification;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
    public partial class DatabaseService
    {
        public async Task<ICollection<CardWithState>> GetDueCards(ReviewSessionMode sessionMode, int exp, CancellationToken cancellationToken = default)
        {
            var cards = new List<CardTable>();

            try
            {
                var unlockedSections = db.CurriculumSections.Where(s => s.RequiredExp <= exp).ToList();
                foreach (var sectionTable in unlockedSections)
                {
                    var tagSpecJson = sectionTable.TagsSpecificationJson;
                    var tagPredicate = SpecificationExpressionFactory.FromJson<TagTable>(tagSpecJson);

                    var meaningSpecJson = sectionTable.DifficultySpecificationJson;
                    var meaningPredicate = SpecificationExpressionFactory.FromJson<MeaningTable>(meaningSpecJson);

                    var sectionCards = db.Tags.Where(tagPredicate)
                        .SelectMany(t => t.Meanings)
                        .Where(meaningPredicate)
                        .SelectMany(m => m.Cards)
                        .Include(c => c.Meaning.Tags)
                        .Include(c => c.Meaning.Sentences)
                        .Include(c => c.UserCardState)
                        .ToList();

                    cards.AddRange(sectionCards);
                }

                foreach (var card in cards)
                {
                    if (card.UserCardState == null)
                    {
                        card.UserCardState = new Infrastructure.Data.Model.UserCardStateTable()
                        {
                            CardId = card.Id,
                            UserId = Infrastructure.Data.Model.UserTable.Default.Id,
                        };
                        await db.UserCardStates.AddAsync(card.UserCardState);
                    }
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // Now project to domain
            var cardWithStates = cards.Select(x => new CardWithState
            {
                Card = new Card
                {
                    DifficultyLevel = DifficultyLevelExtensions.FromString(x.Meaning.DifficultyLevel),
                    SentencesInNativeLanguage = x.Meaning.Sentences.Where(s => s.Language == "pt").Select(s => s.ToDomain()).ToList(),
                    SentencesInTargetLanguage = x.Meaning.Sentences.Where(s => s.Language == "it").Select(s => s.ToDomain()).ToList(),
                    Tags = x.Meaning.Tags.Select(t => t.ToDomain()).ToList(),
                },
                State = x.UserCardState.ToDomain()
            }).OrderBy(c => c.Card.DifficultyLevel).ToList();

            var userDifficulty = settingsService.StudyConfig.Value?.DifficultyLevel ?? DifficultyLevel.Advanced;
            var filtered = cardWithStates.Where(c => c.Card.SuitsDifficulty(userDifficulty)).ToList();

            // Separate new vs review
            var newCards = filtered.Where(c => c.State.Repetitions == 0).ToList();
            var reviewCards = filtered.Where(c => c.State.Repetitions > 0).ToList();

            // Session-specific limits
            (int newLimit, int reviewLimit) = sessionMode switch
            {
                ReviewSessionMode.Quick => (5, 15),
                ReviewSessionMode.Regular => (10, 30),
                ReviewSessionMode.Grind => (20, 60),
                _ => (10, 30)
            };

            // Pick cards
            var sessionCards = new List<CardWithState>();
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
    }
}
