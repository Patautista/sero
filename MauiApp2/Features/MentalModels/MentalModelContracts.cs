using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;
using MauiApp2.Features.Chat;

namespace MauiApp2.Features.MentalModels
{
    /// <summary>
    /// A single, self-contained piece of reasoning contributed by one mental model.
    /// Each model answers a specific class of question and returns its answer as a
    /// titled block of text that the Conversation Engine composes into the companion
    /// prompt. Keeping every model's output in this uniform shape is what lets new
    /// models be added later without touching the engine or the prompt builder.
    /// </summary>
    /// <param name="ModelName">The originating model (e.g. "Skill Model"), for traceability.</param>
    /// <param name="Title">Prompt section heading (e.g. "WHAT THE USER CAN DO").</param>
    /// <param name="Content">The rendered reasoning shown under the heading.</param>
    /// <param name="Order">Relative order of the section in the composed prompt (lower first).</param>
    public sealed record MentalModelInsight(string ModelName, string Title, string Content, int Order = 100);

    /// <summary>
    /// Immutable persona snapshot handed to the mental models so they never need to
    /// reach back into persistence for the companion's identity.
    /// </summary>
    public sealed record CompanionSnapshot(string Name, string Personality, CompanionMood Mood);

    /// <summary>
    /// Everything a mental model may need to reason about the current turn, materialised
    /// once by the Conversation Engine and shared with every model. Models should treat
    /// this as read-only; they own their own reasoning, not the shared snapshot.
    /// </summary>
    public sealed class MentalModelRequest
    {
        public required int UserProfileId { get; init; }
        public required int ConversationId { get; init; }
        public required string UserName { get; init; }
        public required string TargetLanguage { get; init; }
        public required string NativeLanguage { get; init; }
        public required DateTime OnboardedAt { get; init; }
        public required IReadOnlyList<string> Interests { get; init; }
        public required SkillProfile Skills { get; init; }
        public required CompanionSnapshot Companion { get; init; }
        public required IReadOnlyList<ChatMessage> History { get; init; }
        public required IReadOnlyList<ConversationMemory> RecentMemories { get; init; }
        public required IReadOnlyList<LearningActivity> EligibleActivities { get; init; }
        public required IReadOnlyList<PracticeChallenge> ActiveChallenges { get; init; }

        /// <summary>The learner's latest message, when this turn was triggered by one.</summary>
        public string? LatestUserMessage { get; init; }
    }

    /// <summary>
    /// A specialised, single-responsibility model of one aspect of the companion's
    /// world (the user's skills, who the user is, the pet's own identity, the
    /// relationship, or episodic memory). Each model infers and answers a specific
    /// class of question and contributes exactly one insight per turn. Models never
    /// call one another; they communicate only through the Conversation Engine.
    /// </summary>
    public interface IMentalModel
    {
        /// <summary>Human-readable model name, used for logging and insight attribution.</summary>
        string Name { get; }

        /// <summary>
        /// Reason about the current turn and return this model's contribution, or
        /// <c>null</c> when the model has nothing relevant to add right now.
        /// </summary>
        Task<MentalModelInsight?> ObserveAsync(MentalModelRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The planner's decision about what the very next companion turn should accomplish.
    /// Mirrors the Conversation Strategy model's output: a primary and secondary
    /// objective, a concrete suggested action, and the reasoning behind it.
    /// </summary>
    public sealed class ConversationStrategyPlan
    {
        public required string PrimaryObjective { get; init; }
        public string? SecondaryObjective { get; init; }
        public required string SuggestedAction { get; init; }
        public required string Reason { get; init; }
    }

    /// <summary>
    /// The Conversation Strategy model. Acts as the planner: it consumes the insights
    /// produced by every other mental model (via the engine, never directly) and
    /// decides the next conversational objective. Consulted before each response.
    /// </summary>
    public interface IConversationStrategist
    {
        Task<ConversationStrategyPlan> PlanAsync(
            MentalModelRequest request,
            IReadOnlyList<MentalModelInsight> insights,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The combined output of the Conversation Engine for a turn: every model's insight
    /// plus the strategy plan. Passed to the prompt builder, which renders it into the
    /// companion prompt so a single object carries all persistent reasoning.
    /// </summary>
    public sealed class MentalModelReasoning
    {
        public required IReadOnlyList<MentalModelInsight> Insights { get; init; }
        public required ConversationStrategyPlan Strategy { get; init; }

        public static MentalModelReasoning Empty { get; } = new()
        {
            Insights = Array.Empty<MentalModelInsight>(),
            Strategy = new ConversationStrategyPlan
            {
                PrimaryObjective = "Have a natural, encouraging conversation.",
                SuggestedAction = "Keep the conversation going.",
                Reason = "No specific signals were available for this turn."
            }
        };

        /// <summary>Insights in prompt order, strongest-priority first.</summary>
        public IEnumerable<MentalModelInsight> OrderedInsights =>
            Insights.OrderBy(i => i.Order).ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase);
    }
}
