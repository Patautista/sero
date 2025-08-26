using Infrastructure.Data;
using Infrastructure.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MauiApp1.Services.Seed
{
    public static class InitialiserExtensions
    {
        public static async Task InitialiseDatabaseAsync(this IHost app)
        {
            using var scope = app.Services.CreateScope();
             
            var initialiser = scope.ServiceProvider.GetRequiredService<MobileDbContextInitialiser>();

            await initialiser.InitialiseAsync();

            //await initialiser.SeedAsync();
        }
    }

    public class MobileDbContextInitialiser
    {
        private readonly ILogger<MobileDbContextInitialiser> _logger;
        private readonly AnkiDbContext _context;

        public MobileDbContextInitialiser(ILogger<MobileDbContextInitialiser> logger, AnkiDbContext context)
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
            var json = await SeedHelper.LoadMauiAsset("sentences_it-pt.json");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sentences = JsonSerializer.Deserialize<List<SentenceSeed>>(json, options) ?? new();

            var grouped = sentences.GroupBy(s => s.MeaningId);

            foreach (var group in grouped)
            {
                var meaning = new Meaning { Id = group.Key };

                // Sentences
                foreach (var s in group)
                {
                    meaning.Sentences.Add(new Sentence
                    {
                        Id = s.Id,
                        Meaning = meaning,
                        Text = s.Text,
                        Language = s.Language
                    });
                }

                // Tags (attach existing or create new)
                var tagNames = group.SelectMany(s => s.Tags).DistinctBy(t => t.Name);

                foreach (var tagSeed in tagNames)
                {
                    var tag = await _context.Tags.FindAsync(tagSeed.Name);
                    if (tag == null)
                    {
                        tag = new Tag { Name = tagSeed.Name, Type = tagSeed.Type };
                        _context.Tags.Add(tag);
                    }

                    meaning.Tags.Add(tag);
                }

                _context.Meanings.Add(meaning);

                // Optional: build PT->IT card
                var pt = meaning.Sentences.FirstOrDefault(s => s.Language == "pt");
                var it = meaning.Sentences.FirstOrDefault(s => s.Language == "it");

                if (pt != null && it != null)
                {
                    _context.Cards.Add(new Card
                    {
                        Meaning = meaning,
                        NativeSentence = pt,
                        TargetSentence = it
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

    }

    public class SentenceSeed
    {
        public int Id { get; set; }
        public int MeaningId { get; set; }
        public string Text { get; set; } = "";
        public string Language { get; set; } = "pt";
        public List<TagSeed> Tags { get; set; } = new();
    }

    public class TagSeed
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

}
