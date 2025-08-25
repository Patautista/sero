using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Data
{
    public static class InitialiserExtensions
    {
        public static async Task InitialiseDatabaseAsync(this IHost app)
        {
            using var scope = app.Services.CreateScope();
             
            var initialiser = scope.ServiceProvider.GetRequiredService<DbContextInitialiser>();

            await initialiser.InitialiseAsync();

            //await initialiser.SeedAsync();
        }
    }

    public class DbContextInitialiser
    {
        private readonly ILogger<DbContextInitialiser> _logger;
        private readonly AnkiAppContext _context;

        public DbContextInitialiser(ILogger<DbContextInitialiser> logger, AnkiAppContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                await _context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initialising the database.");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                if(_context.Cards.Count() == 0)
                {
                    await TrySeedAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        public async Task TrySeedAsync()
        {
            // 1) Load JSON
            var json = await File.ReadAllTextAsync("sentences_it-pt.json");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sentences = JsonSerializer.Deserialize<List<Domain.Sentence>>(json, options) ?? new();

            // 2) Group by meaning (ensure at least 2 sentences)
            var groups = sentences
                .GroupBy(s => s.MeaningId)
                .Where(g => g.Count() >= 2)
                .ToList();

            // 3) Collect all tag names we’ll need
            var allTagNames = sentences
                .SelectMany(s => s.Tags)
                .Select(t => t.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 4) Preload existing Tag instances (TRACKED)
            //    Important: do not use AsNoTracking here, we want tracked instances.
            var tagMap = await _context.Tags
                .Where(t => allTagNames.Contains(t.Name))
                .ToDictionaryAsync(t => t.Name, StringComparer.OrdinalIgnoreCase);

            // 5) Create any missing Tag instances ONCE and track them
            //    We’ll pick the first observed Type for that name (or default).
            var firstTagByName = sentences
                .SelectMany(s => s.Tags)
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var name in allTagNames)
            {
                if (!tagMap.ContainsKey(name))
                {
                    var src = firstTagByName[name];
                    var newTag = new Tag { Name = name, Type = src.Type ?? "general" };
                    _context.Tags.Add(newTag);         // tracked now
                    tagMap[name] = newTag;             // reuse this instance everywhere
                }
            }

            //persist tags first so the join table can reference them safely
            await _context.SaveChangesAsync();

            
        }
    }
}
