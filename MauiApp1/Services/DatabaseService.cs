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
