using AppLogic.Web;
using Business;
using Business.Model;
using Business.ViewModel;
using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace MauiApp1.Services;

public class CardService(AnkiDbContext db, ISettingsService settingsService)
{
    public async Task<ICollection<CardWithState>> GetCards(CancellationToken cancellationToken = default)
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
                    card.UserCardState = new Infrastructure.Data.Model.UserCardState();
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
                NativeSentence = x.NativeSentence.ToDomain(),
                Tags = x.Meaning.Tags.Select(t => t.ToDomain()).ToList(),
                TargetSentence = x.TargetSentence.ToDomain(),
            },
            State = x.UserCardState?.ToDomain() ?? new Infrastructure.Data.Model.UserCardState
            {
                CardId = x.UserCardState.Id,
                Repetitions = 0
            }.ToDomain()
        }).ToList();

        var userDifficulty = (await settingsService.LoadAsync())?.DifficultyLevel ?? DifficultyLevel.Advanced;
        var filtered = cardWithStates.Where(c => c.Card.SuitsDifficulty(userDifficulty)).ToList();

        return filtered;
    }
    public async Task UpdateUserCardState(UserCardState userCardState,CancellationToken cancellationToken = default)
    {
        var oldState = await db.UserCardStates.FindAsync(userCardState.Id);

        if (oldState == null)
        {
            oldState = new Infrastructure.Data.Model.UserCardState();
        }

        oldState.Interval = userCardState.Interval;
        oldState.NextReview = userCardState.NextReview;
        oldState.LastReviewed = userCardState.LastReviewed;
        oldState.EaseFactor = userCardState.EaseFactor;
        oldState.Repetitions = userCardState.Repetitions;

        db.Update(oldState);
        await db.SaveChangesAsync();
    }
}
