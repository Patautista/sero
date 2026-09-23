using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain.Shared.Models;
using MauiApp2.Features.Activities;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Services.AI
{
    /// <summary>
    /// Single source of truth for every prompt sent to the Activity Agent.
    /// Both system instructions and turn messages are built and logged here,
    /// centralizing prompt construction in one place for easier debugging and iteration.
    /// </summary>
    public interface IActivityPromptBuilder
    {
        /// <summary>
        /// Builds the system instructions for the activity agent, including activity definition,
        /// lifecycle guidance, and response format specification.
        /// </summary>
        string BuildSystemInstructions(LearningActivity definition, ActivityAgentContext context);

        /// <summary>
        /// Builds the turn-specific message context including current activity stage,
        /// learner skill scores, target skill areas, recent conversation, and user input.
        /// </summary>
        string BuildTurnMessage(
            ActivityInstance instance,
            SkillProfile skills,
            IReadOnlyList<string> recentConversation,
            string? userInput);
    }

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
    /// Builds prompts for the Activity Agent using the companion's persona and current
    /// activity context. Logs all generated prompts for debugging and analysis.
    /// </summary>
    public sealed class ActivityPromptBuilder : IActivityPromptBuilder
    {
        private readonly ILogger<ActivityPromptBuilder> _logger;

        public ActivityPromptBuilder(ILogger<ActivityPromptBuilder> logger)
        {
            _logger = logger;
        }

        public string BuildSystemInstructions(LearningActivity definition, ActivityAgentContext context)
        {
            var trainedSkills = definition.TrainedSkills.Count > 0
                ? string.Join(", ", definition.TrainedSkills)
                : "general practice";
            var targetAreas = definition.TargetAreaIds.Count > 0
                ? string.Join(", ", definition.TargetAreaIds)
                : "none";

            var isListeningActivity = definition.TrainedSkills.Contains(SkillType.Listening);

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
            sb.AppendLine($"- Target skill areas: {targetAreas}");
            sb.AppendLine($"- Difficulty (0-100): {definition.Difficulty}");
            sb.AppendLine();
            sb.AppendLine("HOW TO RUN THE ACTIVITY");
            sb.AppendLine($"- Speak mainly in {context.TargetLanguage}, at a level that matches the learner's skills.");
            sb.AppendLine("- Move through the lifecycle: INTRODUCTION -> IN_PROGRESS -> EVALUATING -> COMPLETED.");
            sb.AppendLine("- In INTRODUCTION, warmly introduce the activity in a couple of short sentences, then put the");
            sb.AppendLine("  actual passage, exercise text or prompt in \"generatedContent\" — do not repeat it in \"blocks\".");
            if (isListeningActivity)
            {
                sb.AppendLine("- This is a LISTENING activity: \"generatedContent\" will be converted to real speech audio and played");
                sb.AppendLine("  to the learner instead of shown as text, so it must contain ONLY the literal line to be spoken —");
                sb.AppendLine("  no narration, quotes, stage directions, or meta-commentary. Never restate or reveal that text in \"blocks\".");
            }
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
            sb.AppendLine("  \"generatedContent\": \"the passage, exercise text or prompt for this turn, shown to the learner");
            sb.AppendLine("    in its own highlighted box — do not restate it in blocks — or an empty string if this turn has none\",");
            sb.AppendLine("  \"evaluation\": {");
            sb.AppendLine("    \"feedback\": \"friendly feedback for the learner (only when completed)\",");
            sb.AppendLine("    \"skillAdjustments\": { \"reading\": 0, \"writing\": 0, \"listening\": 0 },");
            sb.AppendLine("    \"areaAdjustments\": { \"present-simple\": 0, \"daily-life\": 0 },");
            sb.AppendLine("    \"reasoning\": \"why you chose those adjustments\"");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine("Keep each block to 1-2 sentences. Set \"completed\" to true only when the activity is truly finished.");
            sb.AppendLine("Only when completed: provide non-zero skillAdjustments (small deltas, roughly -5 to +5, can be negative)");
            sb.AppendLine("and non-zero areaAdjustments for the TARGET SKILL AREAS listed above, using their exact ids.");
            sb.AppendLine("While the activity is still running, keep both skillAdjustments and areaAdjustments at zero.");

            var result = sb.ToString();
            _logger.LogDebug(
                "Built system instructions for activity '{ActivityName}':{NewLine}{Prompt}",
                definition.Name,
                Environment.NewLine,
                result);
            return result;
        }

        public string BuildTurnMessage(
            ActivityInstance instance,
            SkillProfile skills,
            IReadOnlyList<string> recentConversation,
            string? userInput)
        {
            var skillScores = string.Join(", ", SkillProfile.AllSkills.Select(s => $"{s} {skills[s]}"));
            var targetAreas = instance.Definition.TargetAreaIds.Count > 0
                ? string.Join(", ", instance.Definition.TargetAreaIds)
                : "none";

            var sb = new StringBuilder();
            sb.AppendLine($"CURRENT ACTIVITY STAGE: {ActivityStages.ToPromptToken(instance.Stage)}");
            sb.AppendLine($"LEARNER SKILL SCORES (0-100): {skillScores}");
            sb.AppendLine($"TARGET SKILL AREAS (use exact ids for areaAdjustments): {targetAreas}");

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

            var result = sb.ToString();
            _logger.LogDebug(
                "Built turn message for activity '{ActivityName}' stage {Stage}:{NewLine}{Prompt}",
                instance.Definition.Name,
                instance.Stage,
                Environment.NewLine,
                result);
            return result;
        }
    }
}
