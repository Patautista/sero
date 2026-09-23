using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;

namespace MauiApp2.Features.MentalModels.Models
{
    /// <summary>
    /// Represents who the user is: their relatively stable personal information
    /// (interests, goals, preferences, facts) gathered naturally through conversation.
    /// Backed by the profile's interests plus the durable facts captured in memory,
    /// deliberately excluding episodic events (owned by the Conversation Memory model)
    /// so the two models do not duplicate information. Answers: "What topics interest
    /// the user?", "What motivates them?", "What shouldn't be asked repeatedly?".
    /// </summary>
    public sealed class UserModel : IMentalModel
    {
        // Durable "who the user is" facts, as opposed to episodic "what happened" events.
        private static readonly HashSet<string> StableFactTypes =
            new(new[] { "Interest", "Preference", "Goal" }, System.StringComparer.OrdinalIgnoreCase);

        public string Name => "User Model";

        public Task<MentalModelInsight?> ObserveAsync(MentalModelRequest request, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();

            var interests = request.Interests.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            var stableFacts = request.RecentMemories
                .Where(m => StableFactTypes.Contains(m.FactType))
                .ToList();

            var interestFacts = stableFacts
                .Where(m => m.FactType.Equals("Interest", System.StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Content);

            var allInterests = interests
                .Concat(interestFacts)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            sb.AppendLine(allInterests.Count > 0
                ? $"Interests: {string.Join(", ", allInterests)}"
                : "Interests: not yet known — worth discovering.");

            AppendGroup(sb, "Goals", stableFacts, "Goal");
            AppendGroup(sb, "Preferences & facts", stableFacts, "Preference");

            sb.AppendLine("Use these as conversation hooks and examples; don't ask again about what is already known here.");

            return Task.FromResult<MentalModelInsight?>(new MentalModelInsight(
                Name,
                "USER MODEL — who the user is",
                sb.ToString().TrimEnd(),
                Order: 30));
        }

        private static void AppendGroup(StringBuilder sb, string label, IEnumerable<ConversationMemory> facts, string factType)
        {
            var items = facts
                .Where(m => m.FactType.Equals(factType, System.StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Content)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (items.Count > 0)
                sb.AppendLine($"{label}: {string.Join("; ", items)}");
        }
    }
}
