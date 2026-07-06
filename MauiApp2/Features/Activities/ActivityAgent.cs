using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;
using MauiApp2.Services.AI.Schemas;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// In-character context the Activity Agent needs to stay consistent with the
    /// companion while it temporarily guides the learner through an activity.
    /// </summary>
    public sealed class ActivityAgentContext
    {
        public string CompanionName { get; init; } = "your companion";
        public string Personality { get; init; } = string.Empty;
        public string UserName { get; init; } = "Friend";
        public string TargetLanguage { get; init; } = "es";
        public string NativeLanguage { get; init; } = "en";
    }

    /// <summary>
    /// The Activity Agent. It owns *how* an activity is conducted and evaluated, fully
    /// decoupled from the conversation engine. It builds a dedicated prompt from the
    /// activity definition, the current lifecycle stage, the activity's own history,
    /// the learner's skill scores, and recent conversation context, then runs a
    /// Microsoft Agent Framework <c>ChatClientAgent</c> and parses a structured result.
    /// </summary>
    public sealed class ActivityAgent
    {
        private const string AgentName = "ActivityAgent";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IChatClient _chatClient;
        private readonly ILogger<ActivityAgent> _logger;

        public ActivityAgent(IChatClient chatClient, ILogger<ActivityAgent> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        /// <summary>
        /// Advances the activity by one turn. Pass <paramref name="userInput"/> as null to
        /// start the activity (the introduction turn). Updates the instance's stage,
        /// turns, generated content and evaluation, and returns the messages to show.
        /// </summary>
        public async Task<ActivityTurnResult> AdvanceAsync(
            ActivityInstance instance,
            ActivityAgentContext context,
            SkillProfile skills,
            IReadOnlyList<string> recentConversation,
            string? userInput,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(skills);

            // Build a fresh agent each turn from the definition's instructions. The agent
            // itself is stateless; the activity's private history lives on the session.
            AIAgent agent = _chatClient.AsAIAgent(
                instructions: BuildSystemInstructions(instance.Definition, context),
                name: AgentName);

            instance.Session ??= await agent.CreateSessionAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                instance.Turns.Add(ActivityTurn.FromUser(userInput));
            }

            var turnMessage = BuildTurnMessage(instance, skills, recentConversation, userInput);

            var runOptions = new ChatClientAgentRunOptions(new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json
            });

            string rawResponse;
            try
            {
                var response = await agent.RunAsync(turnMessage, instance.Session, runOptions, cancellationToken);
                rawResponse = response.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Activity agent run failed for '{Activity}'", instance.Definition.Name);
                return BuildFallback(instance);
            }

            var parsed = ParseResponse(rawResponse);
            return Apply(instance, parsed);
        }

        // -------------------------------------------------------------------------
        // Prompt construction
        // -------------------------------------------------------------------------

        private static string BuildSystemInstructions(LearningActivity definition, ActivityAgentContext context)
        {
            var trainedSkills = definition.TrainedSkills.Count > 0
                ? string.Join(", ", definition.TrainedSkills)
                : "general practice";

            var sb = new StringBuilder();
            sb.AppendLine($"You are {context.CompanionName}, a {context.Personality} language-learning companion.");
            sb.AppendLine($"The learner is {context.UserName}, practising {context.TargetLanguage} (native language: {context.NativeLanguage}).");
            sb.AppendLine();
            sb.AppendLine("You are temporarily running a learning activity, but you must stay fully in character.");
            sb.AppendLine("Make the activity feel like a natural part of the conversation, not a separate lesson. Be warm, friendly and encouraging.");
            sb.AppendLine();
            sb.AppendLine("ACTIVITY DEFINITION");
            sb.AppendLine($"- Name: {definition.Name}");
            sb.AppendLine($"- Objective: {definition.Objective}");
            sb.AppendLine($"- Instructions: {definition.Instructions}");
            sb.AppendLine($"- Evaluation criteria: {definition.EvaluationCriteria}");
            sb.AppendLine($"- Completion criteria: {definition.CompletionCriteria}");
            sb.AppendLine($"- Trains skills: {trainedSkills}");
            sb.AppendLine($"- Difficulty (0-100): {definition.Difficulty}");
            sb.AppendLine();
            sb.AppendLine("HOW TO RUN THE ACTIVITY");
            sb.AppendLine($"- Speak mainly in {context.TargetLanguage}, at a level that matches the learner's skills.");
            sb.AppendLine("- Move through the lifecycle: INTRODUCTION -> IN_PROGRESS -> EVALUATING -> COMPLETED.");
            sb.AppendLine("- In INTRODUCTION, warmly introduce the activity and give the first prompt or content.");
            sb.AppendLine("- In IN_PROGRESS, react to the learner's answers and guide them one small step at a time.");
            sb.AppendLine("- Enter EVALUATING once the completion criteria are met, then COMPLETED with your evaluation.");
            sb.AppendLine("- Only judge performance yourself; never ask the learner to grade themselves.");
            sb.AppendLine();
            sb.AppendLine("RESPONSE FORMAT");
            sb.AppendLine("Reply with a single JSON object only (no markdown, no code fences) matching exactly:");
            sb.AppendLine("{");
            sb.AppendLine("  \"blocks\": [\"short in-character message\", \"optional second short message\"],");
            sb.AppendLine("  \"stage\": \"INTRODUCTION | IN_PROGRESS | EVALUATING | COMPLETED\",");
            sb.AppendLine("  \"completed\": false,");
            sb.AppendLine("  \"generatedContent\": \"any passage or prompt you generated this turn, or an empty string\",");
            sb.AppendLine("  \"evaluation\": {");
            sb.AppendLine("    \"feedback\": \"friendly feedback for the learner (only when completed)\",");
            sb.AppendLine("    \"skillAdjustments\": { \"reading\": 0, \"writing\": 0, \"listening\": 0 },");
            sb.AppendLine("    \"reasoning\": \"why you chose those adjustments\"");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine("Keep each block to 1-2 sentences. Set \"completed\" to true only when the activity is truly finished,");
            sb.AppendLine("and only then provide non-zero skillAdjustments (small deltas, roughly -5 to +5, can be negative).");
            sb.AppendLine("While the activity is still running, keep skillAdjustments at zero.");
            return sb.ToString();
        }

        private static string BuildTurnMessage(
            ActivityInstance instance,
            SkillProfile skills,
            IReadOnlyList<string> recentConversation,
            string? userInput)
        {
            var skillScores = string.Join(", ", SkillProfile.AllSkills.Select(s => $"{s} {skills[s]}"));

            var sb = new StringBuilder();
            sb.AppendLine($"CURRENT ACTIVITY STAGE: {ActivityStages.ToPromptToken(instance.Stage)}");
            sb.AppendLine($"LEARNER SKILL SCORES (0-100): {skillScores}");

            if (recentConversation is { Count: > 0 })
            {
                sb.AppendLine("RECENT CONVERSATION (for tone and continuity):");
                foreach (var line in recentConversation.TakeLast(6))
                {
                    sb.AppendLine($"  {line}");
                }
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                sb.AppendLine("The activity is starting now. Introduce it in character and give the learner the first step.");
            }
            else
            {
                sb.AppendLine("LEARNER'S LATEST MESSAGE:");
                sb.AppendLine($"\"{userInput}\"");
                sb.AppendLine("Respond in character and move the activity forward by one small step.");
            }

            return sb.ToString();
        }

        // -------------------------------------------------------------------------
        // Response handling
        // -------------------------------------------------------------------------

        private ActivityAgentResponseSchema ParseResponse(string json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                var cleaned = StripCodeFence(json);
                try
                {
                    var parsed = JsonSerializer.Deserialize<ActivityAgentResponseSchema>(cleaned, JsonOptions);
                    if (parsed is not null)
                        return parsed;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse activity agent response; using raw text as a single block.");
                }
            }

            return new ActivityAgentResponseSchema
            {
                Blocks = { StripCodeFence(json).Trim().Trim('"') },
                Stage = ActivityStages.ToPromptToken(ActivityStage.InProgress)
            };
        }

        private static ActivityTurnResult Apply(ActivityInstance instance, ActivityAgentResponseSchema parsed)
        {
            var stage = ActivityStages.FromPromptToken(parsed.Stage);
            if (parsed.Completed)
            {
                stage = ActivityStage.Completed;
            }

            var blocks = parsed.Blocks is { Count: > 0 }
                ? parsed.Blocks.Where(b => !string.IsNullOrWhiteSpace(b)).ToList()
                : new List<string> { "Let's keep going." };

            instance.Stage = stage;
            foreach (var block in blocks)
            {
                instance.Turns.Add(ActivityTurn.FromAgent(block));
            }

            if (!string.IsNullOrWhiteSpace(parsed.GeneratedContent))
            {
                instance.GeneratedContent = parsed.GeneratedContent;
            }

            ActivityEvaluation? evaluation = null;
            if (stage == ActivityStage.Completed)
            {
                evaluation = BuildEvaluation(parsed.Evaluation);
                instance.Evaluation = evaluation;
                instance.CompletedAt = DateTime.UtcNow;
            }

            return new ActivityTurnResult
            {
                Blocks = blocks,
                Stage = stage,
                Evaluation = evaluation
            };
        }

        private static ActivityEvaluation BuildEvaluation(ActivityEvaluationSchema? schema)
        {
            var evaluation = new ActivityEvaluation { Completed = true };
            if (schema is null)
                return evaluation;

            evaluation.Feedback = schema.Feedback;
            evaluation.Reasoning = schema.Reasoning;

            var adjustments = schema.SkillAdjustments ?? new ActivitySkillAdjustmentsSchema();
            evaluation.SkillAdjustments = new Dictionary<SkillType, int>
            {
                [SkillType.Reading] = adjustments.Reading,
                [SkillType.Writing] = adjustments.Writing,
                [SkillType.Listening] = adjustments.Listening
            };

            return evaluation;
        }

        private static ActivityTurnResult BuildFallback(ActivityInstance instance)
        {
            const string message = "Sorry, my circuits glitched for a second. Let's pick this up again in a moment.";
            instance.Turns.Add(ActivityTurn.FromAgent(message));
            return new ActivityTurnResult
            {
                Blocks = new List<string> { message },
                Stage = instance.Stage == ActivityStage.NotStarted ? ActivityStage.Introduction : instance.Stage
            };
        }

        private static string StripCodeFence(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var trimmed = text.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed[7..];
            else if (trimmed.StartsWith("```"))
                trimmed = trimmed[3..];

            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3];

            return trimmed.Trim();
        }
    }
}
