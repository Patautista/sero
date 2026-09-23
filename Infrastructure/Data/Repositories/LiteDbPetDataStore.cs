using Domain.Shared.Models;
using LiteDB;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Data.Repositories
{
    /// <summary>
    /// LiteDB-backed implementation of the pet data store abstraction.
    /// Owns the ILiteDatabase connection and exposes repositories for each entity type.
    /// </summary>
    public class LiteDbPetDataStore : IPetDataStore
    {
        private readonly ILiteDatabase _database;
        private readonly Lazy<IRepository<UserProfileTable>> _userProfiles;
        private readonly Lazy<IRepository<CompanionTable>> _companions;
        private readonly Lazy<IRepository<ConversationTable>> _conversations;
        private readonly Lazy<IRepository<MessageTable>> _messages;
        private readonly Lazy<IRepository<LanguageMistakeTable>> _languageMistakes;
        private readonly Lazy<IRepository<ConversationMemoryTable>> _conversationMemories;
        private readonly Lazy<IRepository<UserActivityTable>> _userActivities;

        public LiteDbPetDataStore(ILiteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));

            // Lazily initialize repositories to avoid unnecessary collection lookups
            _userProfiles = new Lazy<IRepository<UserProfileTable>>(
                () => new LiteDbRepository<UserProfileTable>(_database.GetCollection<UserProfileTable>("userProfiles")));
            _companions = new Lazy<IRepository<CompanionTable>>(
                () => new LiteDbRepository<CompanionTable>(_database.GetCollection<CompanionTable>("companions")));
            _conversations = new Lazy<IRepository<ConversationTable>>(
                () => new LiteDbRepository<ConversationTable>(_database.GetCollection<ConversationTable>("conversations")));
            _messages = new Lazy<IRepository<MessageTable>>(
                () => new LiteDbRepository<MessageTable>(_database.GetCollection<MessageTable>("messages")));
            _languageMistakes = new Lazy<IRepository<LanguageMistakeTable>>(
                () => new LiteDbRepository<LanguageMistakeTable>(_database.GetCollection<LanguageMistakeTable>("languageMistakes")));
            _conversationMemories = new Lazy<IRepository<ConversationMemoryTable>>(
                () => new LiteDbRepository<ConversationMemoryTable>(_database.GetCollection<ConversationMemoryTable>("conversationMemories")));
            _userActivities = new Lazy<IRepository<UserActivityTable>>(
                () => new LiteDbRepository<UserActivityTable>(_database.GetCollection<UserActivityTable>("userActivities")));
        }

        public IRepository<UserProfileTable> UserProfiles => _userProfiles.Value;
        public IRepository<CompanionTable> Companions => _companions.Value;
        public IRepository<ConversationTable> Conversations => _conversations.Value;
        public IRepository<MessageTable> Messages => _messages.Value;
        public IRepository<LanguageMistakeTable> LanguageMistakes => _languageMistakes.Value;
        public IRepository<ConversationMemoryTable> ConversationMemories => _conversationMemories.Value;
        public IRepository<UserActivityTable> UserActivities => _userActivities.Value;

        public Task SaveChangesAsync()
        {
            // LiteDB commits changes immediately on insert/update/delete,
            // but we wrap it in a Task for consistency with the abstraction.
            // If switching to EF Core or another provider, this would coordinate
            // a transaction across all repositories.
            _database.Commit();
            return Task.CompletedTask;
        }

        public Task EnsureIndexesAsync()
        {
            var conversationsColl = _database.GetCollection<ConversationTable>("conversations");
            conversationsColl.EnsureIndex(x => x.UserProfileId);

            var messagesColl = _database.GetCollection<MessageTable>("messages");
            messagesColl.EnsureIndex(x => x.ConversationId);
            messagesColl.EnsureIndex(x => x.Timestamp);

            var mistakesColl = _database.GetCollection<LanguageMistakeTable>("languageMistakes");
            mistakesColl.EnsureIndex(x => x.UserProfileId);

            var memoriesColl = _database.GetCollection<ConversationMemoryTable>("conversationMemories");
            memoriesColl.EnsureIndex(x => x.UserProfileId);

            var activitiesColl = _database.GetCollection<UserActivityTable>("userActivities");
            activitiesColl.EnsureIndex(x => x.UserProfileId);

            return Task.CompletedTask;
        }

        public Task ClearUserDataAsync()
        {
            _database.DropCollection("userProfiles");
            _database.DropCollection("conversations");
            _database.DropCollection("messages");
            _database.DropCollection("languageMistakes");
            _database.DropCollection("conversationMemories");
            _database.DropCollection("userActivities");
            return Task.CompletedTask;
        }
    }
}
