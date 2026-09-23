using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;
using Infrastructure.Data.Repositories;
using MauiApp2.Features.Skills;
using MauiApp2.Services.AI;
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
        private readonly IPetDataStore _dataStore;
        private readonly ISkillAreaCatalogProvider _skillAreaCatalogProvider;
        private readonly ILogger<ActivityOrchestrator> _logger;

        public ActivityOrchestrator(
            IActivityInstanceStore store,
            ActivityAgent agent,
            ActivitySelectionService selection,
            IPetDataStore dataStore,
            ISkillAreaCatalogProvider skillAreaCatalogProvider,
            ILogger<ActivityOrchestrator> logger)
        {
            _store = store;
            _agent = agent;
            _selection = selection;
            _dataStore = dataStore;
            _skillAreaCatalogProvider = skillAreaCatalogProvider;
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

            var profile = await _dataStore.UserProfiles.FirstOrDefaultAsync(p => true);
            var skills = profile?.Skills ?? new SkillProfile();
            var learningContext = await BuildLearningContextAsync(profile?.Id, definition, skills, profile?.AreaProgress ?? new AreaProgress(), cancellationToken);

            var instance = new ActivityInstance
            {
                ConversationId = conversationId,
                Definition = definition,
                Stage = ActivityStages.FirstActiveStage,
                LearningContext = learningContext
            };
            _store.Set(instance);

            _logger.LogInformation(
                "Starting activity '{Activity}' for conversation {ConversationId}.",
                definition.Name, conversationId);

            // First turn introduces the activity in character; there is no user input yet.
            var result = await _agent.AdvanceAsync(
                instance, context, skills, recentConversation, userInput: null, cancellationToken);

            return await FinalizeIfCompletedAsync(instance, result);
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

            return await FinalizeIfCompletedAsync(instance, result);
        }

        /// <summary>
        /// When a turn completes the activity, applies the agent's structured skill
        /// adjustments to the user's profile (independently of the agent), clears the
        /// active instance so the conversation returns to normal flow, and (when any
        /// skill actually changed) returns a copy of the result carrying a human-readable
        /// summary the conversation engine can show the learner.
        /// </summary>
        private async Task<ActivityTurnResult> FinalizeIfCompletedAsync(ActivityInstance instance, ActivityTurnResult result)
        {
            if (!result.Completed)
            {
                return result;
            }

            string? skillUpdateSummary = null;
            if (result.Evaluation is { } evaluation)
            {
                var profile = await _dataStore.UserProfiles.FirstOrDefaultAsync(p => true);
                var successful = WasSuccessful(instance, evaluation);
                var multiplier = successful && profile is not null
                    ? await GetStreakMultiplierAsync(profile.Id)
                    : 1;
                var skillAdjustments = evaluation.SkillAdjustments.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value > 0
                        ? (int)Math.Min((long)kvp.Value * multiplier, SkillProfile.MaxScore)
                        : kvp.Value);

                var previousSkills = profile is null
                    ? null
                    : skillAdjustments.Keys.ToDictionary(skill => skill, skill => profile.Skills[skill]);
                var previousAreas = profile is null
                    ? null
                    : evaluation.AreaAdjustments.Keys.ToDictionary(areaId => areaId, areaId => profile.AreaProgress[areaId], StringComparer.OrdinalIgnoreCase);

                var updated = await _selection.ApplyActivityAdjustmentsAsync(skillAdjustments, evaluation.AreaAdjustments);
                _logger.LogInformation(
                    "Applied activity adjustments for completed activity '{Activity}' with {Multiplier}x skill gains: {Reasoning}",
                    instance.Definition.Name, multiplier, evaluation.Reasoning);

                if (updated is { } progress && previousSkills is not null && previousAreas is not null)
                {
                    var catalog = await _skillAreaCatalogProvider.GetCatalogAsync();
                    skillUpdateSummary = BuildSkillUpdateSummary(
                        previousSkills, progress.Skills, previousAreas, progress.AreaProgress, catalog, multiplier);
                }
            }

            await SaveCompletedActivityAsync(
                instance,
                result.Evaluation is { } completedEvaluation
                    ? WasSuccessful(instance, completedEvaluation)
                    : null);

            _store.Remove(instance.ConversationId);
            _logger.LogInformation(
                "Activity '{Activity}' completed for conversation {ConversationId}.",
                instance.Definition.Name, instance.ConversationId);

            return skillUpdateSummary is null
                ? result
                : new ActivityTurnResult
                {
                    Blocks = result.Blocks,
                    Stage = result.Stage,
                    Evaluation = result.Evaluation,
                    SkillUpdateSummary = skillUpdateSummary,
                    GeneratedContent = result.GeneratedContent
                };
        }

        /// <summary>
        /// Builds a summary of the skill and named area scores that actually changed.
        /// </summary>
        private static string? BuildSkillUpdateSummary(
            IReadOnlyDictionary<SkillType, int> previousSkills,
            SkillProfile skills,
            IReadOnlyDictionary<string, int> previousAreas,
            AreaProgress areaProgress,
            SkillAreaCatalog catalog,
            int multiplier)
        {
            var skillChanges = previousSkills
                .Select(kvp => (Skill: kvp.Key, Delta: skills[kvp.Key] - kvp.Value))
                .Where(change => change.Delta != 0)
                .OrderByDescending(change => change.Delta)
                .ToList();
            var areaChanges = previousAreas
                .Select(kvp => (Area: catalog.Find(kvp.Key), Delta: areaProgress[kvp.Key] - kvp.Value))
                .Where(change => change.Area is not null && change.Delta != 0)
                .ToList();
            if (skillChanges.Count == 0 && areaChanges.Count == 0)
            {
                return null;
            }

            static string FormatDelta(int delta) => $"{(delta > 0 ? "+" : string.Empty)}{delta}";

            var sections = new List<string>();
            if (skillChanges.Count > 0)
                sections.Add($"Skills: {string.Join(", ", skillChanges.Select(change => $"{change.Skill} {FormatDelta(change.Delta)}"))}");

            foreach (var kind in new[] { SkillAreaKind.Concept, SkillAreaKind.Topic })
            {
                var changes = areaChanges.Where(change => change.Area!.Kind == kind).ToList();
                if (changes.Count > 0)
                    sections.Add($"{kind}s: {string.Join(", ", changes.Select(change => $"{change.Area!.Name} {FormatDelta(change.Delta)}"))}");
            }

            if (multiplier > 1 && skillChanges.Any(change => change.Delta > 0))
                sections.Add($"{multiplier}× streak bonus on skill gains");

            return string.Join("; ", sections);
        }

        private static bool WasSuccessful(ActivityInstance instance, ActivityEvaluation evaluation) =>
            evaluation.SkillAdjustments.Any(kvp => instance.Definition.TrainedSkills.Contains(kvp.Key) && kvp.Value > 0)
            && evaluation.SkillAdjustments.Values.All(delta => delta >= 0)
            && evaluation.AreaAdjustments.Values.All(delta => delta >= 0);

        private async Task<int> GetStreakMultiplierAsync(int userProfileId)
        {
            var history = await _dataStore.CompletedLearningActivities.WhereAsync(activity => activity.UserProfileId == userProfileId);
            var consecutiveWins = history
                .OrderByDescending(activity => activity.CompletedAt)
                .ThenByDescending(activity => activity.Id)
                .Take(2)
                .TakeWhile(activity => activity.WasSuccessful == true)
                .Count();

            return 1 + consecutiveWins;
        }

        private async Task SaveCompletedActivityAsync(ActivityInstance instance, bool? wasSuccessful)
        {
            var profile = await _dataStore.UserProfiles.FirstOrDefaultAsync(p => true);
            if (profile is null)
            {
                return;
            }

            _dataStore.CompletedLearningActivities.Add(new Infrastructure.Data.CompletedLearningActivityTable
            {
                UserProfileId = profile.Id,
                ActivityId = instance.Definition.Id,
                TrainedSkillNames = instance.Definition.TrainedSkills.Select(skill => skill.ToString()).ToList(),
                TargetAreaIds = instance.Definition.TargetAreaIds.ToList(),
                GeneratedContent = string.Join(Environment.NewLine, instance.GeneratedContents.Distinct(StringComparer.Ordinal)),
                CompletedAt = instance.CompletedAt ?? DateTime.UtcNow,
                WasSuccessful = wasSuccessful
            });
            await _dataStore.SaveChangesAsync();
        }

        private async Task<ActivityLearningContext> BuildLearningContextAsync(
            int? userProfileId,
            LearningActivity definition,
            SkillProfile skills,
            AreaProgress areaProgress,
            CancellationToken cancellationToken)
        {
            var catalog = await _skillAreaCatalogProvider.GetCatalogAsync(cancellationToken);
            var history = userProfileId is { } id
                ? (await _dataStore.CompletedLearningActivities.WhereAsync(activity => activity.UserProfileId == id)).ToList()
                : new List<Infrastructure.Data.CompletedLearningActivityTable>();

            var trainedSkillNames = definition.TrainedSkills
                .Select(skill => skill.ToString())
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var targetAreaIds = definition.TargetAreaIds
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matchingHistory = history
                .Where(activity => activity.TrainedSkillNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).SequenceEqual(trainedSkillNames, StringComparer.OrdinalIgnoreCase)
                    && activity.TargetAreaIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).SequenceEqual(targetAreaIds, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(activity => activity.CompletedAt)
                .ToList();

            return new ActivityLearningContext
            {
                TrainedSkills = definition.TrainedSkills
                    .Select(skill => new ActivitySkillSnapshot(skill, skills[skill]))
                    .ToList(),
                TargetAreas = definition.TargetAreaIds
                    .Select(areaId =>
                    {
                        var area = catalog.Find(areaId);
                        var hasBeenPractised = history.Any(activity => activity.TargetAreaIds.Contains(areaId, StringComparer.OrdinalIgnoreCase));
                        return new ActivityAreaSnapshot(
                            areaId,
                            area?.Name ?? areaId,
                            area?.Kind ?? SkillAreaKind.Concept,
                            areaProgress[areaId],
                            !hasBeenPractised);
                    })
                    .ToList(),
                PriorGeneratedContent = matchingHistory
                    .Select(activity => activity.GeneratedContent)
                    .Where(content => !string.IsNullOrWhiteSpace(content))
                    .Take(6)
                    .ToList()
            };
        }
    }
}
