using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// Coordinates the lifecycle of learning activities without leaking any Activity
    /// Agent internals into the conversation engine. The companion decides *when* to
    /// start an activity (by signalling an activity id); this orchestrator owns *how*
    /// the run is started, continued and completed, delegating the actual conduct and
    /// evaluation to the <see cref="ActivityAgent"/>.
    ///
    /// It is the single seam the conversation engine talks to, so the companion and the
    /// Activity Agent can evolve independently. On completion it applies the agent's
    /// structured skill adjustments through <see cref="ActivitySelectionService"/> and
    /// clears the active instance.
    /// </summary>
    public sealed class ActivityOrchestrator
    {
        private readonly IActivityInstanceStore _store;
        private readonly ActivityAgent _agent;
        private readonly ActivitySelectionService _selection;
        private readonly ILogger<ActivityOrchestrator> _logger;

        public ActivityOrchestrator(
            IActivityInstanceStore store,
            ActivityAgent agent,
            ActivitySelectionService selection,
            ILogger<ActivityOrchestrator> logger)
        {
            _store = store;
            _agent = agent;
            _selection = selection;
            _logger = logger;
        }

        /// <summary>True when an activity is currently running for the conversation.</summary>
        public bool IsActivityActive(int conversationId) => _store.HasActive(conversationId);

        /// <summary>
        /// Starts the activity with the given id for a conversation and runs its
        /// introduction turn. Returns the in-character messages to show, or null when
        /// the id is unknown or an activity is already running.
        /// </summary>
        public async Task<ActivityTurnResult?> StartActivityAsync(
            int conversationId,
            string activityId,
            ActivityAgentContext context,
            IReadOnlyList<string> recentConversation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            var definition = ActivityCatalog.FindById(activityId);
            if (definition is null)
            {
                _logger.LogWarning("Companion requested unknown activity id '{ActivityId}'.", activityId);
                return null;
            }

            if (_store.HasActive(conversationId))
            {
                _logger.LogInformation(
                    "Activity already active for conversation {ConversationId}; ignoring start request for '{ActivityId}'.",
                    conversationId, activityId);
                return null;
            }

            var skills = await _selection.GetCurrentSkillProfileAsync();

            var instance = new ActivityInstance
            {
                ConversationId = conversationId,
                Definition = definition,
                Stage = ActivityStages.FirstActiveStage
            };
            _store.Set(instance);

            _logger.LogInformation(
                "Starting activity '{Activity}' for conversation {ConversationId}.",
                definition.Name, conversationId);

            // First turn introduces the activity in character; there is no user input yet.
            var result = await _agent.AdvanceAsync(
                instance, context, skills, recentConversation, userInput: null, cancellationToken);

            await FinalizeIfCompletedAsync(instance, result);
            return result;
        }

        /// <summary>
        /// Advances the active activity with the learner's latest message. Returns the
        /// in-character messages to show, or null when no activity is active.
        /// </summary>
        public async Task<ActivityTurnResult?> ContinueActivityAsync(
            int conversationId,
            ActivityAgentContext context,
            string userInput,
            IReadOnlyList<string> recentConversation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!_store.TryGet(conversationId, out var instance) || instance.IsCompleted)
            {
                return null;
            }

            var skills = await _selection.GetCurrentSkillProfileAsync();

            var result = await _agent.AdvanceAsync(
                instance, context, skills, recentConversation, userInput, cancellationToken);

            await FinalizeIfCompletedAsync(instance, result);
            return result;
        }

        /// <summary>
        /// When a turn completes the activity, applies the agent's structured skill
        /// adjustments to the user's profile (independently of the agent) and clears
        /// the active instance so the conversation returns to normal flow.
        /// </summary>
        private async Task FinalizeIfCompletedAsync(ActivityInstance instance, ActivityTurnResult result)
        {
            if (!result.Completed)
            {
                return;
            }

            if (result.Evaluation is { SkillAdjustments.Count: > 0 } evaluation)
            {
                await _selection.ApplySkillAdjustmentsAsync(evaluation.SkillAdjustments);
                _logger.LogInformation(
                    "Applied skill adjustments for completed activity '{Activity}': {Reasoning}",
                    instance.Definition.Name, evaluation.Reasoning);
            }

            _store.Remove(instance.ConversationId);
            _logger.LogInformation(
                "Activity '{Activity}' completed for conversation {ConversationId}.",
                instance.Definition.Name, instance.ConversationId);
        }
    }
}
