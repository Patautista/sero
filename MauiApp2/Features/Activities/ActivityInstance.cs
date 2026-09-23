using System;
using System.Collections.Generic;
using Domain.Shared.Models;
using Microsoft.Agents.AI;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// A single execution of a <see cref="LearningActivity"/> definition. Holds the
    /// runtime state that lets the same definition be run many times independently:
    /// the current stage, the turns exchanged so far, any generated content, and the
    /// final evaluation. The live <see cref="Session"/> keeps the Activity Agent's own
    /// message history, separate from the main conversation engine.
    /// </summary>
    public class ActivityInstance
    {
        /// <summary>Unique id for this run (distinct from the definition id).</summary>
        public string InstanceId { get; } = Guid.NewGuid().ToString("N");

        /// <summary>The conversation this activity is embedded in.</summary>
        public int ConversationId { get; init; }

        /// <summary>The reusable definition being executed.</summary>
        public required LearningActivity Definition { get; init; }

        /// <summary>Where the activity currently sits in its lifecycle.</summary>
        public ActivityStage Stage { get; set; } = ActivityStage.NotStarted;

        /// <summary>Turns exchanged during the activity (user responses + agent output).</summary>
        public List<ActivityTurn> Turns { get; } = new();

        /// <summary>Any content the agent produced for the learner (a passage, a prompt, etc.).</summary>
        public string? GeneratedContent { get; set; }

        /// <summary>True once the activity has reached <see cref="ActivityStage.Completed"/>.</summary>
        public bool IsCompleted => Stage == ActivityStage.Completed;

        /// <summary>The structured evaluation, populated when the activity completes.</summary>
        public ActivityEvaluation? Evaluation { get; set; }

        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// The Agent Framework session that carries this activity's private message
        /// history. Kept in memory for the duration of the run so the agent remembers
        /// prior turns without re-injecting them into every prompt. It is created lazily
        /// by the <see cref="ActivityAgent"/> on the first turn.
        /// </summary>
        public AgentSession? Session { get; set; }
    }

    /// <summary>A single message exchanged inside an activity.</summary>
    public class ActivityTurn
    {
        public ActivityTurnRole Role { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public static ActivityTurn FromUser(string content) =>
            new() { Role = ActivityTurnRole.User, Content = content };

        public static ActivityTurn FromAgent(string content) =>
            new() { Role = ActivityTurnRole.Agent, Content = content };
    }

    public enum ActivityTurnRole
    {
        User,
        Agent
    }

    /// <summary>
    /// Structured result the Activity Agent returns when it finishes evaluating.
    /// The conversation system uses <see cref="SkillAdjustments"/> to update the
    /// user's skill profile independently of the agent.
    /// </summary>
    public class ActivityEvaluation
    {
        public bool Completed { get; set; }

        /// <summary>Friendly, in-character feedback for the learner.</summary>
        public string Feedback { get; set; } = string.Empty;

        /// <summary>Per-skill score deltas (can be negative), keyed by skill.</summary>
        public Dictionary<SkillType, int> SkillAdjustments { get; set; } = new();

        /// <summary>Per-area progress deltas (can be negative), keyed by skill-area id.</summary>
        public Dictionary<string, int> AreaAdjustments { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The agent's private rationale for the adjustments.</summary>
        public string Reasoning { get; set; } = string.Empty;
    }

    /// <summary>
    /// The outcome of advancing an activity by one turn: the in-character messages to
    /// show the learner, the resulting stage, and (when finished) the evaluation.
    /// </summary>
    public class ActivityTurnResult
    {
        public IReadOnlyList<string> Blocks { get; init; } = Array.Empty<string>();
        public ActivityStage Stage { get; init; }
        public bool Completed => Stage == ActivityStage.Completed;
        public ActivityEvaluation? Evaluation { get; init; }

        /// <summary>
        /// Friendly, human-readable summary of any skill score changes applied when the
        /// activity completed (e.g. "Reading +5, Writing -2"), or null when the activity
        /// has not finished or no skill changed. Populated by the
        /// <see cref="ActivityOrchestrator"/> after it persists the evaluation's skill
        /// adjustments, so the conversation engine can inform the learner without needing
        /// to know how skills are stored or evaluated.
        /// </summary>
        public string? SkillUpdateSummary { get; init; }

        /// <summary>
        /// Any passage, exercise text or prompt the Activity Agent generated this turn
        /// (e.g. a reading passage or a specific writing prompt), or null when nothing was
        /// generated. Kept separate from <see cref="Blocks"/> so the conversation engine
        /// can show it as its own visually distinct message instead of plain companion
        /// chit-chat.
        /// </summary>
        public string? GeneratedContent { get; init; }
    }
}
