using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class PetDbContextInitialiser
    {
        private readonly PetDbContext _context;
        private readonly ILogger<PetDbContextInitialiser> _logger;

        public PetDbContextInitialiser(PetDbContext context, ILogger<PetDbContextInitialiser> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Language Pet database...");

                // Check if database exists
                var canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    _logger.LogInformation("Database does not exist. Creating...");
                    // For new databases, use EnsureCreated (faster, no migrations needed for MAUI)
                    await _context.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Database created successfully");
                }
                else
                {
                    _logger.LogInformation("Database already exists. Checking for migrations...");
                    // For existing databases, apply any pending migrations
                    var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        _logger.LogInformation($"Applying {pendingMigrations.Count()} pending migration(s)...");
                        await _context.Database.MigrateAsync();
                        _logger.LogInformation("Migrations applied successfully");
                    }
                    else
                    {
                        _logger.LogInformation("Database is up to date");
                    }
                }

                _logger.LogInformation("Language Pet database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private async Task TrySeedAsync()
        {
            // Seed data is already handled by OnModelCreating in PetDbContext
            // This method can be used for additional runtime seeding if needed

            // Check if companion exists
            var companionExists = await _context.Companions.AnyAsync();
            if (!companionExists)
            {
                _logger.LogInformation("Seeding default companion...");
                // EnsureCreated should have already seeded it, but log in case
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Clears all user-generated data (profile, conversations, messages, memories, mistakes,
        /// and activities) while keeping the companion seed intact.
        /// </summary>
        public async Task ClearUserDataAsync()
        {
            _logger.LogWarning("Clearing all user data...");

            try
            {
                _context.UserProfiles.RemoveRange(_context.UserProfiles);
                await _context.SaveChangesAsync();

                // Reset companion mood to default
                var companion = await _context.Companions.FindAsync(1);
                if (companion != null)
                {
                    companion.CurrentMood = Domain.Shared.Models.CompanionMood.Curious;
                    companion.LastMoodChange = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User data cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user data");
                throw;
            }
        }

#if DEBUG
        /// <summary>
        /// Development-only: Completely resets the database.
        /// WARNING: This will delete ALL data!
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            _logger.LogWarning("⚠️ RESETTING DATABASE - ALL DATA WILL BE LOST ⚠️");

            try
            {
                // Delete the database
                await _context.Database.EnsureDeletedAsync();
                _logger.LogInformation("Database deleted");

                // Recreate it
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database recreated with fresh schema");

                _logger.LogInformation("✅ Database reset complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during database reset");
                throw;
            }
        }
#endif
    }
}
