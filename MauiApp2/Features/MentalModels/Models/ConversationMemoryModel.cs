using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;

namespace MauiApp2.Features.MentalModels.Models
{
    /// <summary>
    /// Stores and surfaces episodic memories — the "what have we talked about" history,
    /// as opposed to the stable identity facts owned by the User Model. It reconstructs
    /// recent context from remembered events (and, when none exist yet, from the tail of
    /// the current conversation). Answers: "What have we talked about recently?",
    /// "Have we already discussed this topic?".
    /// </summary>
    public sealed class ConversationMemoryModel : IMentalModel
    {
        public string Name => "Conversation Memory";

        public Task<MentalModelInsight?> ObserveAsync(MentalModelRequest request, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();

            var episodic = request.RecentMemories
                .Where(m => m.FactType.Equals("Event", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.LastReferencedAt)
                .Select(m => m.Content)
                .Take(5)
                .ToList();

            if (episodic.Count > 0)
            {
                sb.AppendLine("Recent shared moments:");
                foreach (var moment in episodic)
                    sb.AppendLine($"- {moment}");
            }

            var recentTopics = SummariseRecentTopics(request.History);
            if (!string.IsNullOrWhiteSpace(recentTopics))
                sb.AppendLine($"Most recent exchange touched on: {recentTopics}");

            if (sb.Length == 0)
                sb.AppendLine("No shared history yet — this is effectively a fresh start.");

            return Task.FromResult<MentalModelInsight?>(new MentalModelInsight(
                Name,
                "CONVERSATION MEMORY — what we've discussed",
                sb.ToString().TrimEnd(),
                Order: 40));
        }

        private static string SummariseRecentTopics(IReadOnlyList<Chat.ChatMessage> history)
        {
            var lastUserLine = history
                .Where(m => m.SenderType == SenderType.User && !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => m.Content.Trim())
                .LastOrDefault();

            if (string.IsNullOrWhiteSpace(lastUserLine))
                return string.Empty;

            // Keep it short — a compact reminder, not a transcript.
            return lastUserLine.Length <= 140 ? lastUserLine : lastUserLine[..140] + "…";
        }
    }
}
