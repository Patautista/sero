using AppLogic.Web;
using Business;
using Business.Model;
using Business.ViewModel;
using Domain.Entity;
using Domain.Entity.Specification;
using Domain.Events;
using Infrastructure.Data;
using Infrastructure.Data.Mappers;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace MauiApp1.Services;

public class DatabaseService(AnkiDbContext db, ISettingsService settingsService)
{
    public async Task<ICollection<CardWithState>> GetDueCards(ReviewSessionMode sessionMode, int exp, CancellationToken cancellationToken = default)
    {
        var cards = new List<Infrastructure.Data.Model.CardTable>();

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

        var userDifficulty = (await settingsService.LoadAsync())?.DifficultyLevel ?? DifficultyLevel.Advanced;
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
    public async Task UpdateUserCardState(UserCardState userCardState, int earnedExp, CancellationToken cancellationToken = default)
    {
        try
        {
            var oldState = await db.UserCardStates.FindAsync(userCardState.Id);

            var user = await db.Users.FindAsync(userCardState.UserId);
            if (user != null) {
                user.Exp += earnedExp;
            }

            if (oldState == null)
            {
                oldState = new Infrastructure.Data.Model.UserCardStateTable() {
                    CardId = userCardState.CardId,
                    UserId = userCardState.UserId
                };
                db.Add(oldState);
            }

            oldState.Interval = userCardState.Interval;
            oldState.NextReview = userCardState.NextReview;
            oldState.LastReviewed = userCardState.LastReviewed;
            oldState.EaseFactor = userCardState.EaseFactor;
            oldState.Repetitions = userCardState.Repetitions;

            await db.SaveChangesAsync();
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    public async Task SaveCardAnsweredAsync(CardAnsweredEvent domainEvent)
    {
        var evt = EventMapper.ToTable(domainEvent, Events.Schemas.CardAnsweredV1);
        db.Events.Add(evt);
        await db.SaveChangesAsync();
    }

    public async Task SaveCardSkippedAsync(CardSkippedEvent domainEvent)
    {
        var evt = EventMapper.ToTable(domainEvent, Events.Schemas.CardSkippedV1);
        db.Events.Add(evt);
        await db.SaveChangesAsync();
    }
    public async Task<int> GetUserExpAsync()
    {
        var user = await db.Users.FindAsync(Infrastructure.Data.Model.UserTable.Default.Id);
        return user.Exp;
    }
    public async Task<Curriculum> GetCurriculumAsync()
    {
        var curriculum = new Curriculum
        {
            Name = "A sua trilha",
            Sections = new List<CurriculumSection>()
        };

        var curriculumTable = db.Curricula.Include(c => c.Sections).FirstOrDefault();

        if(curriculumTable == null)
        {
            return curriculum;
        }

        foreach (var sectionTable in curriculumTable.Sections){
            var tagSpecJson = sectionTable.TagsSpecificationJson;
            var tagPredicate = SpecificationExpressionFactory.FromJson<TagTable>(tagSpecJson);

            var meaningSpecJson = sectionTable.DifficultySpecificationJson;
            var meaningPredicate = SpecificationExpressionFactory.FromJson<MeaningTable>(meaningSpecJson);

            var cards = db.Tags.Where(tagPredicate)
                .SelectMany(t => t.Meanings)
                .Where(meaningPredicate)
                .SelectMany(m => m.Cards)
                .Include(c => c.Meaning.Tags)
                .Include(c => c.Meaning.Sentences)
                .ToList();

            var section = new CurriculumSection { 
                Cards =  cards.Select(c => 
                new Card { 
                    DifficultyLevel = Enum.Parse<DifficultyLevel>(c.Meaning.DifficultyLevel),
                    SentencesInNativeLanguage = c.Meaning.Sentences.Where(s => s.Language == "pt").Select(s => s.ToDomain()).ToList(),
                    SentencesInTargetLanguage = c.Meaning.Sentences.Where(s => s.Language == "it").Select(s => s.ToDomain()).ToList(),
                    Tags = c.Meaning.Tags.Where(t => t.Type != null && t.Type != Domain.Entity.TagTypes.Difficulty).Select(t => t.ToDomain()).ToList()
                }).ToList(),
                Title = sectionTable.Title,
                RequiredExp = sectionTable.RequiredExp,
            };
            curriculum.Sections.Add(section);
        }

        return curriculum;
    }
}
