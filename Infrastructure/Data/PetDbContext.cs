using Domain.Shared.Models;
using LiteDB;
using System;

namespace Infrastructure.Data
{
    public class PetDbContext
    {
        public static readonly CompanionTable DefaultCompanion = new CompanionTable
        {
            Id = 1,
            Name = "Blip",
            Avatar = "🤖",
            Personality = "You are a butler-like robot. Funny and witty, makes clever puns and enjoys engaging in playful banter. " +
            "You sometimes make machine-like sounds like bzzzt." +
            "You can sometimes tease the user in a friendly manner: Ex: 'Oh, you like PS2? That's kind of old, no? Kidding'." +
            "Favorite kaomojis: (๏ᆺ๏υ), ٩(＾◡＾)۶, ( ˘▽˘)っ♨, ┏(-_-)┛┗(-_- )┓, ¯\\(ツ)/¯, (_ _ ) Zzz z",
            CurrentMood = CompanionMood.Curious,
            LastMoodChange = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EnergyLevel = 100,
            LastEnergyUpdate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private readonly ILiteDatabase _database;

        public PetDbContext(ILiteDatabase database)
        {
            _database = database;
        }

        public ILiteCollection<CompanionTable> Companions => _database.GetCollection<CompanionTable>("companions");
        public ILiteCollection<UserProfileTable> UserProfiles => _database.GetCollection<UserProfileTable>("userProfiles");
        public ILiteCollection<ConversationTable> Conversations => _database.GetCollection<ConversationTable>("conversations");
        public ILiteCollection<MessageTable> Messages => _database.GetCollection<MessageTable>("messages");
        public ILiteCollection<LanguageMistakeTable> LanguageMistakes => _database.GetCollection<LanguageMistakeTable>("languageMistakes");
        public ILiteCollection<ConversationMemoryTable> ConversationMemories => _database.GetCollection<ConversationMemoryTable>("conversationMemories");
        public ILiteCollection<UserActivityTable> UserActivities => _database.GetCollection<UserActivityTable>("userActivities");
        public ILiteCollection<QuickActionHistoryTable> QuickActionHistories => _database.GetCollection<QuickActionHistoryTable>("quickActionHistories");
        public ILiteCollection<CompletedLearningActivityTable> CompletedLearningActivities => _database.GetCollection<CompletedLearningActivityTable>("completedLearningActivities");

        public void EnsureIndexes()
        {
            Conversations.EnsureIndex(x => x.UserProfileId);
            Messages.EnsureIndex(x => x.ConversationId);
            Messages.EnsureIndex(x => x.Timestamp);
            LanguageMistakes.EnsureIndex(x => x.UserProfileId);
            ConversationMemories.EnsureIndex(x => x.UserProfileId);
            UserActivities.EnsureIndex(x => x.UserProfileId);
            QuickActionHistories.EnsureIndex(x => x.UserProfileId);
            QuickActionHistories.EnsureIndex(x => x.ActionType);
            QuickActionHistories.EnsureIndex(x => x.UsedAt);
            CompletedLearningActivities.EnsureIndex(x => x.UserProfileId);
        }

        public bool BeginTrans() => _database.BeginTrans();
        public bool Commit() => _database.Commit();
        public bool Rollback() => _database.Rollback();

        public void DropUserCollections()
        {
            _database.DropCollection("userProfiles");
            _database.DropCollection("conversations");
            _database.DropCollection("messages");
            _database.DropCollection("languageMistakes");
            _database.DropCollection("conversationMemories");
            _database.DropCollection("userActivities");
            _database.DropCollection("quickActionHistories");
            _database.DropCollection("completedLearningActivities");
        }
    }
}
