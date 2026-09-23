using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.MentalModels.Models
{
    /// <summary>
    /// Represents the current relationship between the companion and the user — distinct
    /// from conversation history. It tracks closeness, trust and familiarity, and turns
    /// them into social permissions: whether the companion may joke, ask personal
    /// questions, and how formal it should be. In this version the metrics are derived
    /// deterministically from how much and how long the pair have interacted, so the
    /// relationship evolves consistently over time.
    /// </summary>
    public sealed class RelationshipModel : IMentalModel
    {
        private readonly IPetDataStore _store;
        private readonly ILogger<RelationshipModel> _logger;

        public RelationshipModel(IPetDataStore store, ILogger<RelationshipModel> logger)
        {
            _store = store;
            _logger = logger;
        }

        public string Name => "Relationship Model";

        public async Task<MentalModelInsight?> ObserveAsync(MentalModelRequest request, CancellationToken cancellationToken = default)
        {
            var userMessageCount = await CountUserMessagesAsync(request.UserProfileId, cancellationToken);
            var daysKnown = (DateTime.UtcNow - request.OnboardedAt).TotalDays;
            var state = RelationshipState.Derive(userMessageCount, daysKnown, request.Companion.Mood);

            var sb = new StringBuilder();
            sb.AppendLine($"Friendship: {state.StageDisplayName}");
            sb.AppendLine($"Trust: {state.Trust}/100");
            sb.AppendLine($"Familiarity: {state.Familiarity}/100");
            sb.AppendLine($"Conversation style: {state.ConversationStyle}");
            sb.AppendLine($"Emotional tone: {state.EmotionalTone}");
            sb.AppendLine($"Formality: {state.Formality}");
            sb.AppendLine($"You may joke: {(state.CanJoke ? "yes" : "not yet — keep it kind and sincere")}.");
            sb.AppendLine($"You may ask personal questions: {(state.CanAskPersonalQuestions ? "yes, gently" : "not yet — let trust build first")}.");

            return new MentalModelInsight(
                Name,
                "RELATIONSHIP MODEL — how close you are",
                sb.ToString().TrimEnd(),
                Order: 60);
        }

        private async Task<int> CountUserMessagesAsync(int userProfileId, CancellationToken cancellationToken)
        {
            try
            {
                var allMessages = await _store.Messages.GetAllAsync();
                return allMessages.Count(m => 
                    m.SenderType == SenderType.User && m.Conversation?.UserProfileId == userProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Relationship Model failed to count messages for user {UserProfileId}", userProfileId);
                return 0;
            }
        }
    }
}
