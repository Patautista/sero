using Domain;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/sentences", async () =>
{
    string filePath = "sentences_it-pt.json";
    string json = await File.ReadAllTextAsync(filePath);
    return JsonSerializer.Deserialize<List<Sentence>>(json);
})
.WithName("GetCards");

app.MapDefaultEndpoints();

app.Run();

