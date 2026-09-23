using Domain.Shared.Models;
using System.Threading.Tasks;

namespace Infrastructure.Data.Repositories
{
    /// <summary>
    /// Application-facing contract for local pet data persistence.
    /// Provides access to all entity repositories and unified change persistence.
    /// </summary>
    public interface IPetDataStore
    {
        IRepository<UserProfileTable> UserProfiles { get; }
        IRepository<CompanionTable> Companions { get; }
        IRepository<ConversationTable> Conversations { get; }
        IRepository<MessageTable> Messages { get; }
        IRepository<LanguageMistakeTable> LanguageMistakes { get; }
        IRepository<ConversationMemoryTable> ConversationMemories { get; }
        IRepository<UserActivityTable> UserActivities { get; }

        /// <summary>
        /// Persists all pending changes (inserts, updates, deletes) across all repositories.
        /// </summary>
        Task SaveChangesAsync();

        /// <summary>
        /// Ensures database indexes are created for performance-critical queries.
        /// </summary>
        Task EnsureIndexesAsync();

        /// <summary>
        /// Drops all user-generated data (keeping companion seed intact).
        /// </summary>
        Task ClearUserDataAsync();
    }
}
