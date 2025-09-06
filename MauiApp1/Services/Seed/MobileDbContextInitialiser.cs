using Business;
using Domain.Entity;
using Domain.Entity.Specification;
using Infrastructure.Data;
using Infrastructure.Data.Model;
using Infrastructure.ETL.Models;
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
            var json = await SeedHelper.LoadMauiAsset("backup_it-pt.json");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cards = JsonSerializer.Deserialize<List<CardSeed>>(json, options) ?? new();

            // Group by meaning
            var grouped = cards.GroupBy(c => c.NativeSentence.MeaningId);

            // Ensure default user
            await _context.Users.AddAsync(UserTable.Default);

            // Collect all unique tag names upfront
            var allTagSeeds = cards.SelectMany(c => c.Tags).DistinctBy(t => t.Name).ToList();
            var existingTags = await _context.Tags
                .Where(t => allTagSeeds.Select(s => s.Name).Contains(t.Name))
                .ToDictionaryAsync(t => t.Name);

            var newTags = new List<TagTable>();

            foreach (var group in grouped)
            {
                var meaning = new MeaningTable
                {
                    Id = group.Key,
                    DifficultyLevel = group.First().DifficultyLevel.ToString()
                };

                // Sentences (native + target)
                foreach (var s in group)
                {
                    meaning.Sentences.Add(new SentenceTable
                    {
                        Meaning = meaning,
                        Text = s.NativeSentence.Text,
                        Language = s.NativeSentence.Language
                    });

                    meaning.Sentences.Add(new SentenceTable
                    {
                        Meaning = meaning,
                        Text = s.TargetSentence.Text,
                        Language = s.TargetSentence.Language
                    });
                }

                // Tags
                foreach (var tagSeed in group.SelectMany(s => s.Tags).DistinctBy(t => t.Name))
                {
                    if (!existingTags.TryGetValue(tagSeed.Name, out var tag))
                    {
                        tag = new TagTable
                        {
                            Name = tagSeed.Name,
                            Type = tagSeed.Type ?? "learningTopic"
                        };
                        newTags.Add(tag);
                        existingTags[tag.Name] = tag; // cache for reuse
                    }

                    meaning.Tags.Add(tag);
                }

                _context.Meanings.Add(meaning);

                _context.Cards.Add(new CardTable
                {
                    Meaning = meaning
                });
            }

            if (newTags.Count > 0)
                await _context.Tags.AddRangeAsync(newTags);

            await _context.SaveChangesAsync();

            var curriculumTable = new CurriculumTable
            {
                Id = 0,
                Name = "it-pt",
                Sections = new List<CurriculumSectionTable> {
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "introduction").ToJson(),
                    Title = "Apresentações",
                    RequiredExp = ExpCalculator.ExpForLevel(1)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "family").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner")
                        .ToJson(),
                    Title = "Família 1",
                    RequiredExp = ExpCalculator.ExpForLevel(2)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "food").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Comida 1",
                    RequiredExp = ExpCalculator.ExpForLevel(5)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = 
                        new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "time").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Tempo 1",
                    RequiredExp = ExpCalculator.ExpForLevel(8)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "like").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Gostar 1",
                    RequiredExp = ExpCalculator.ExpForLevel(10)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "want").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Querer 1",
                    RequiredExp = ExpCalculator.ExpForLevel(10)
                },
                
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "need").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Precisar 1",
                    RequiredExp = ExpCalculator.ExpForLevel(9)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "past").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Passado 1",
                    RequiredExp = ExpCalculator.ExpForLevel(14)
                },
                new CurriculumSectionTable
                {
                    CurriculumId = 0,
                    TagsSpecificationJson = new PropertySpecificationDto(nameof(TagTable.Name), MatchOperator.Equals, "future").ToJson(),
                    DifficultySpecificationJson =
                        new PropertySpecificationDto(nameof(MeaningTable.DifficultyLevel), MatchOperator.Equals, "Beginner").ToJson(),
                    Title = "Futuro 1",
                    RequiredExp = ExpCalculator.ExpForLevel(18)
                },
            }};
            _context.Curricula.Add(curriculumTable);
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
