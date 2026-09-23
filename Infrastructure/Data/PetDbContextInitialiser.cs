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

        public Task InitialiseAsync()
        {
            try
            {
                _context.EnsureIndexes();
                if (_context.Companions.FindById(1) == null)
                    _context.Companions.Insert(PetDbContext.DefaultCompanion);
                _logger.LogInformation("Language Pet document database initialized successfully");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        public Task SeedAsync() => InitialiseAsync();

        /// <summary>
        /// Clears all user-generated data (profile, conversations, messages, memories, mistakes,
        /// and activities) while keeping the companion seed intact.
        /// </summary>
        public Task ClearUserDataAsync()
        {
            _logger.LogWarning("Clearing all user data...");

            try
            {
                _context.BeginTrans();
                _context.DropUserCollections();

                var companion = _context.Companions.FindById(1);
                if (companion != null)
                {
                    companion.CurrentMood = Domain.Shared.Models.CompanionMood.Curious;
                    companion.LastMoodChange = DateTime.UtcNow;
                    companion.EnergyLevel = 100;
                    companion.LastEnergyUpdate = DateTime.UtcNow;
                    _context.Companions.Update(companion);
                }

                _context.Commit();
                _logger.LogInformation("User data cleared successfully");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _context.Rollback();
                _logger.LogError(ex, "Error clearing user data");
                throw;
            }
        }

#if DEBUG
        /// <summary>
        /// Development-only: Completely resets the database.
        /// WARNING: This will delete ALL data!
        /// </summary>
        public Task ResetDatabaseAsync()
        {
            _logger.LogWarning("⚠️ RESETTING DATABASE - ALL DATA WILL BE LOST ⚠️");

            try
            {
                _context.BeginTrans();
                _context.DropUserCollections();
                _context.Companions.DeleteAll();
                _context.Companions.Insert(PetDbContext.DefaultCompanion);
                _context.Commit();
                _context.EnsureIndexes();

                _logger.LogInformation("✅ Database reset complete");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _context.Rollback();
                _logger.LogError(ex, "❌ Error during database reset");
                throw;
            }
        }
#endif
    }
}
