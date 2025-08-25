using Domain;
using Infrastructure.Data;
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

builder.Services.AddDbContext<AnkiAppContext>(p => p.UseSqlite("Data Source=localdb.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/cards", async () =>
{
    string filePath = "sentences_it-pt.json";
    string json = await File.ReadAllTextAsync(filePath);
    var list = JsonSerializer.Deserialize<List<Sentence>>(json);
    var groups = list
    .GroupBy(x => x.MeaningId)
    .Where(g => g.Count() >= 2) // ensure at least 2 sentences
    .ToList();

    var cards = groups.Select(g => new Card
    {
        Id = g.Key,
        MeaningId = g.Key,
        NativeSentence = g.ElementAt(0),
        TargetSentence = g.ElementAt(1),
        Tags = g.ElementAt(0).Tags
    }).ToList();

    return JsonSerializer.Serialize(cards);
})
.WithName("GetCards");

app.MapDefaultEndpoints();

app.Run();

