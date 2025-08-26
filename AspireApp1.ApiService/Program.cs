using ApplicationL.ViewModel;
using Domain;
using Infrastructure.Data;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// 🔹 Configure JSON globally
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<AnkiDbContext>(p => p.UseSqlite("Data Source=localdb.db").EnableSensitiveDataLogging());
builder.Services.AddScoped<DbContextInitialiser>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetService<DbContextInitialiser>();
    if (init != null)
    {
        await init.InitialiseAsync();
        await init.SeedAsync();
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/cards", async () =>
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetService<AnkiDbContext>();

        var cards = new List<Infrastructure.Data.Model.Card>();

        try
        {
            cards = await db.Cards
                .Include(c => c.NativeSentence)
                .Include(c => c.TargetSentence)
                .Include(c => c.Meaning).ThenInclude(m => m.Tags)
                .Include(c => c.UserCardState)
                .ToListAsync();

            foreach(var card in cards)
            {
                if(card.UserCardState == null)
                {
                    card.UserCardState = new UserCardState();
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
            State = x.UserCardState?.ToDomain() ?? new UserCardState
            {
                CardId = x.UserCardState.Id,
                Repetitions = 0
            }.ToDomain()
        }).ToList();

        return cardWithStates;

    }
})
.WithName("GetCards");

app.MapPut("/cards", async (UserCardState request) =>
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetService<AnkiDbContext>();
        var oldState = await db.UserCardStates.FindAsync(request.Id);
        oldState = request;
        db.Update(oldState);
        await db.SaveChangesAsync();
    }
});

app.MapDefaultEndpoints();

app.Run();

