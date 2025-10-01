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
using MauiApp1.Components.Pages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace MauiApp1.Services;

public partial class DatabaseService(MobileDbContext db, ISettingsService settingsService)
{
    public async Task<Dashboard.Model> GetDashboardModelAsync()
    {
        var cardAnsweredEvents = (await db.Events
            .Where(e => e.Name == nameof(CardAnsweredEvent)).ToListAsync())
            .Select(e => JsonSerializer.Deserialize<CardAnsweredEvent>(e.DomainEventJson))
            .Join(db.Cards.Include(c => c.Meaning.Sentences), e => e.CardId, c => c.Id, (cardEvent, card) => new { cardEvent, card })
            .Select(x => new Dashboard.Model.CardAnswered( 
                ReviewSessionId: Guid.Empty,
                CardId: x.card.Id, 
                CardNativeText: x.card.Meaning.Sentences.First().Text,
                EllapsedMs: x.cardEvent.EllapsedMs,
                Correct: x.cardEvent.Correct))
            .Where(e => e != null)
            .ToList();
        var cardSkippedEvents = (await db.Events
            .Where(e => e.Name == nameof(CardSkippedEvent)).ToListAsync())
            .Select(e => JsonSerializer.Deserialize<CardSkippedEvent>(e.DomainEventJson))
            .Join(db.Cards.Include(c => c.Meaning.Sentences), e => e.CardId, c => c.Id, (cardEvent, card) => new { cardEvent , card })
            .Select(x => new Dashboard.Model.CardSkipped(x.cardEvent.Id, x.card.Meaning.Sentences.First().Text, x.card.Id))
            .Where(e => e != null)
            .ToList();

        var model = new Dashboard.Model();
        model.CardAnsweredData = cardAnsweredEvents;
        model.CardSkippedData = cardSkippedEvents;
        return model;
    }
    public async Task<DeckTable?> GetFirstDeckAsync()
    {
        if (!await db.Decks.AnyAsync())
        {
            var defaultDeck = new DeckTable
            {
                Name = "Default Deck",
                Description = "This is the default deck.",
            };
            db.Decks.Add(defaultDeck);
            await db.SaveChangesAsync();
        }
        return await db.Decks.Include(d => d.Cards)
                             .ThenInclude(c => c.Meaning)
                             .ThenInclude(m => m.Tags)
                             .Include(d => d.Cards)
                             .ThenInclude(c => c.Meaning)
                             .ThenInclude(m => m.Sentences)
                             .FirstOrDefaultAsync();
    }
    public async Task<List<DeckTable>> GetAllDecksAsync()
    {
        return await db.Decks.OrderBy(d => d.Name).ToListAsync();
    }
}
