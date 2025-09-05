using AppLogic.Web;
using Business;
using Business.Model;
using Business.ViewModel;
using Domain.Entity;
using Domain.Entity.Specification;
using Infrastructure.Data;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace MauiApp1.Services;

public class DatabaseService(AnkiDbContext db, ISettingsService settingsService)
{
    public async Task<ICollection<CardWithState>> GetDueCards(ReviewSessionMode sessionMode, CancellationToken cancellationToken = default)
    {
        var cards = new List<Infrastructure.Data.Model.CardTable>();

        try
        {
            cards = await db.Cards
                .Include(c => c.NativeSentence)
                .Include(c => c.TargetSentence)
                .Include(c => c.Meaning).ThenInclude(m => m.Tags)
                .Include(c => c.UserCardState)
                .ToListAsync();

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
                NativeSentence = x.NativeSentence.ToDomain(),
                Tags = x.Meaning.Tags.Select(t => t.ToDomain()).ToList(),
                TargetSentence = x.TargetSentence.ToDomain(),
            },
            State = x.UserCardState.ToDomain()
        }).OrderBy(c => c.Card.NativeSentence.Text.Length).ToList();

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
        var curriculumTable = new CurriculumTable { Id = 0, Name = "it-pt", 
            Sections = new List<CurriculumSectionTable> {
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "introduction").ToJson(),
                    Title = "Apresentações",
                    RequiredExp = ExpCalculator.ExpForLevel(1)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "time").ToJson(),
                    Title = "Tempo",
                    RequiredExp = ExpCalculator.ExpForLevel(4)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "food").ToJson(),
                    Title = "Comida",
                    RequiredExp = ExpCalculator.ExpForLevel(3)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "family").ToJson(),
                    DifficultySpecificationJson = 
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner")
                        .Or(new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Intermediate"))
                        .ToJson(),
                    Title = "Família 1",
                    RequiredExp = ExpCalculator.ExpForLevel(2)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "want").ToJson(),
                    Title = "Querer",
                    RequiredExp = ExpCalculator.ExpForLevel(10)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "like").ToJson(),
                    Title = "Gostar",
                    RequiredExp = ExpCalculator.ExpForLevel(7)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "need").ToJson(),
                    Title = "Precisar",
                    RequiredExp = ExpCalculator.ExpForLevel(9)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "past").ToJson(),
                    Title = "Passado",
                    RequiredExp = ExpCalculator.ExpForLevel(14)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "future").ToJson(),
                    Title = "Futuro",
                    RequiredExp = ExpCalculator.ExpForLevel(18)
                },
            }
        };
        foreach(var sectionTable in curriculumTable.Sections){
            var tagSpecJson = sectionTable.TagsSpecificationJson;
            var tagPredicate = SpecificationExpressionFactory.FromJson<TagTable>(tagSpecJson);

            var meaningSpecJson = sectionTable.DifficultySpecificationJson;
            var meaningPredicate = SpecificationExpressionFactory.FromJson<MeaningTable>(meaningSpecJson);

            var cards = db.Tags.Where(tagPredicate)
                .SelectMany(t => t.Meanings)
                .Where(meaningPredicate)
                .SelectMany(m => m.Cards)
                .Include(c => c.Meaning.Tags)
                .Include(c => c.NativeSentence)
                .Include(c => c.TargetSentence)
                .ToList();

            var section = new CurriculumSection { 
                Cards =  cards.Select(c => 
                new Card { 
                    DifficultyLevel = Enum.Parse<DifficultyLevel>(c.Meaning.DifficultyLevel),
                    NativeSentence = c.NativeSentence.ToDomain(),
                    TargetSentence = c.TargetSentence.ToDomain(),
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
