using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MauiApp2.Services.AI.Schemas
{
    /// <summary>
    /// The structured payload the Activity Agent returns on every turn. It always
    /// stays in character (<see cref="Blocks"/>) but also reports the machine-readable
    /// lifecycle <see cref="Stage"/> and, when the activity finishes, a structured
    /// <see cref="Evaluation"/> the conversation system uses to update skills.
    /// </summary>
    public class ActivityAgentResponseSchema
    {
        /// <summary>In-character companion messages to show the learner (1-3 short paragraphs).</summary>
        [JsonPropertyName("blocks")]
        public List<string> Blocks { get; set; } = new();

        /// <summary>Lifecycle stage after this turn: INTRODUCTION, IN_PROGRESS, EVALUATING, or COMPLETED.</summary>
        [JsonPropertyName("stage")]
        public string Stage { get; set; } = "IN_PROGRESS";

        /// <summary>True when the activity is finished and <see cref="Evaluation"/> is present.</summary>
        [JsonPropertyName("completed")]
        public bool Completed { get; set; }

        /// <summary>Optional content generated for the learner this turn (a passage, an audio script, etc.).</summary>
        [JsonPropertyName("generatedContent")]
        public string? GeneratedContent { get; set; }

        /// <summary>Structured evaluation, only populated when <see cref="Completed"/> is true.</summary>
        [JsonPropertyName("evaluation")]
        public ActivityEvaluationSchema? Evaluation { get; set; }
    }

    /// <summary>
    /// Structured evaluation returned when an activity completes. Skill adjustments are
    /// per-skill deltas (may be negative) applied to the learner's profile independently.
    /// </summary>
    public class ActivityEvaluationSchema
    {
        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;

        [JsonPropertyName("skillAdjustments")]
        public ActivitySkillAdjustmentsSchema SkillAdjustments { get; set; } = new();

        [JsonPropertyName("areaAdjustments")]
        public Dictionary<string, int> AreaAdjustments { get; set; } = new();

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;
    }

    /// <summary>
    /// Per-skill score deltas. Mirrors the modelled skills so the JSON schema handed to
    /// the LLM is explicit about which adjustments are allowed.
    /// </summary>
    public class ActivitySkillAdjustmentsSchema
    {
        [JsonPropertyName("reading")]
        public int Reading { get; set; }

        [JsonPropertyName("writing")]
        public int Writing { get; set; }

        [JsonPropertyName("listening")]
        public int Listening { get; set; }
    }
}
