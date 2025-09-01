using AppLogic.Web;
using Business;
using Business.Model;
using Business.ViewModel;
using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace MauiApp1.Services;

public class MemoryService(AnkiDbContext db, ISettingsService settingsService)
{
    public async Task<ICollection<CardWithState>> GetCards(ReviewSessionMode sessionMode, CancellationToken cancellationToken = default)
    {
        var cards = new List<Infrastructure.Data.Model.Card>();

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
                    await db.UserCardStates.AddAsync(new Infrastructure.Data.Model.UserCardState()
                    {
                        CardId = card.Id,
                        UserId = Infrastructure.Data.Model.User.Default.Id,
                    });
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
            State = x.UserCardState?.ToDomain()
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
                oldState = new Infrastructure.Data.Model.UserCardState() {
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
        var user = await db.Users.FindAsync(Infrastructure.Data.Model.User.Default.Id);
        return user.Exp;
    }
}
