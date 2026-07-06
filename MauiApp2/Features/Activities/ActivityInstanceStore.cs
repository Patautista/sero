using System.Collections.Concurrent;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// Holds the active <see cref="ActivityInstance"/> for each conversation. Only one
    /// activity runs per conversation at a time. Kept as a small abstraction so the
    /// backing store (in-memory today, a table later) can change without touching the
    /// agent or the conversation engine.
    /// </summary>
    public interface IActivityInstanceStore
    {
        bool TryGet(int conversationId, out ActivityInstance instance);
        void Set(ActivityInstance instance);
        void Remove(int conversationId);
        bool HasActive(int conversationId);
    }

    /// <summary>
    /// Thread-safe in-memory implementation keyed by conversation id. Suitable for the
    /// single-user MAUI app; completed activities are persisted separately by the
    /// orchestrator, so nothing important is lost if the process restarts.
    /// </summary>
    public sealed class InMemoryActivityInstanceStore : IActivityInstanceStore
    {
        private readonly ConcurrentDictionary<int, ActivityInstance> _instances = new();

        public bool TryGet(int conversationId, out ActivityInstance instance)
        {
            var found = _instances.TryGetValue(conversationId, out var value);
            instance = value!;
            return found && instance is not null;
        }

        public void Set(ActivityInstance instance) =>
            _instances[instance.ConversationId] = instance;

        public void Remove(int conversationId) =>
            _instances.TryRemove(conversationId, out _);

        public bool HasActive(int conversationId) =>
            _instances.TryGetValue(conversationId, out var instance) && !instance.IsCompleted;
    }
}
