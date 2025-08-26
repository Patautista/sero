using ApplicationL.Model;
using ApplicationL.ViewModel;
using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace AppLogic.Web;

public class CardService(AnkiDbContext db)
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
            Card = new Domain.Card
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

        return cardWithStates;
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
