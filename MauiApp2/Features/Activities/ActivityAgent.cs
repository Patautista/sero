using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;
using MauiApp2.Services.AI;
using MauiApp2.Services.AI.Schemas;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.Activities
{
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
        private readonly ICompanionPromptBuilder _promptBuilder;
        private readonly ILogger<ActivityAgent> _logger;

        public ActivityAgent(
            IChatClient chatClient,
            ICompanionPromptBuilder promptBuilder,
            ILogger<ActivityAgent> logger)
        {
            _chatClient = chatClient;
            _promptBuilder = promptBuilder;
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
                instructions: _promptBuilder.BuildActivitySystemInstructions(instance.Definition, context),
                name: AgentName);

            instance.Session ??= await agent.CreateSessionAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                instance.Turns.Add(ActivityTurn.FromUser(userInput));
            }

            var turnMessage = _promptBuilder.BuildActivityTurnMessage(instance, skills, instance.LearningContext, recentConversation, userInput);

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
                instance.GeneratedContents.Add(parsed.GeneratedContent);
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
                Evaluation = evaluation,
                GeneratedContent = string.IsNullOrWhiteSpace(parsed.GeneratedContent) ? null : parsed.GeneratedContent
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

            evaluation.AreaAdjustments = (schema.AreaAdjustments ?? new Dictionary<string, int>())
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

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
