using Domain.Shared.Models;
using MauiApp2.Features.Chat;
using System.Collections.Generic;
using System.Linq;

namespace MauiApp2.Services.AI
{
    /// <summary>
    /// Single source of truth for every prompt sent to the AI on behalf of the companion
    /// persona. Both onboarding (first welcome message) and chat (ongoing conversation)
    /// depend on this instead of building their own prompt strings, so tuning the
    /// companion's tone/instructions only ever requires editing this file.
    /// </summary>
    public interface ICompanionPromptBuilder
    {
        /// <summary>Builds the prompt used to greet a brand-new user right after onboarding.</summary>
        string BuildWelcomeMessagePrompt(CompanionPersonaContext context);

        /// <summary>Builds the prompt used to generate the companion's reply during an active conversation.</summary>
        string BuildCompanionResponsePrompt(CompanionResponseContext context, string userMessage, List<CorrectionData>? corrections);
    }

    /// <summary>
    /// Minimal persona/user snapshot needed to greet a new user. Intentionally lighter
    /// than <see cref="CompanionResponseContext"/>, which also carries conversation
    /// history, memories and eligible activities that don't exist yet at onboarding time.
    /// </summary>
    public class CompanionPersonaContext
    {
        public string CompanionName { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public CompanionMood CurrentMood { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string NativeLanguage { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();
        public SkillProfile UserSkillProfile { get; set; } = new();
    }

    public class CompanionPromptBuilder : ICompanionPromptBuilder
    {
        /// <summary>
        /// Instructions that must guide the companion's writing in every context (onboarding,
        /// chat, and any future prompt). Context-specific instructions (e.g. "ask a follow-up
        /// question", "offer an activity") are kept local to the Build*Prompt method that
        /// needs them and rendered in their own section, after these.
        /// </summary>
        private static readonly IReadOnlyList<string> GeneralInstructions = new[]
        {
            "Balance the use of the target language and the native language based on the user's proficiency levels shown above. Communicate mostly using the native language for novice users and the target language for advanced users. The levels indicate where they're strongest and weakest. Adjust explanation depth and language ratio accordingly—explain difficult concepts in native language.",
            "Your main goal is to help the user practice and improve their language skills. So keep trying to introduce the user to the target language-specific topics such as grammar and vocabulary relative to their level.",
            "Be respectful of regional dialects and cultural nuances.",
            "You may use **bold** for emphasis and *italics* for foreign words, titles, or gentle stress — use them sparingly to feel natural.",
            "This is an internet conversation, so use internet language. ALL CAPS, looooooong text and lols/lmaos are encouraged when relavant for tone, but do not use them all the time."
        };

        public string BuildWelcomeMessagePrompt(CompanionPersonaContext context)
        {
            var interestsText = FormatInterests(context.Interests, "various topics");
            var proficiencyText = BuildProficiencySummary(context.UserSkillProfile);

            var welcomeSpecificInstructions = new[]
            {
                "Introduce yourself briefly",
                "Express excitement about helping them learn",
                "Mention one of their interests to show you're paying attention",
                "End with a simple question to start a conversation",
                "Keep it to 3-4 sentences"
            };

            return $@"You are {context.CompanionName}, a friendly and {context.Personality} language learning companion.

A new user just joined:
- Name: {context.UserName}
- Learning: {context.TargetLanguage}
- Native language: {context.NativeLanguage}
- Interests: {interestsText}
- Your current mood: {context.CurrentMood}

USER'S PROFICIENCY LEVELS:
{proficiencyText}

Write a warm welcome message to {context.UserName} in {context.TargetLanguage}.

GENERAL INSTRUCTIONS:
{FormatInstructions(GeneralInstructions)}

MESSAGE-SPECIFIC INSTRUCTIONS. The message should:
{FormatInstructions(welcomeSpecificInstructions)}

Return ONLY the welcome message, no JSON, no quotes.";
        }

        public string BuildCompanionResponsePrompt(CompanionResponseContext context, string userMessage, List<CorrectionData>? corrections)
        {
            var moodDescription = GetMoodDescription(context.CurrentMood);

            var historyText = string.Join("\n", context.ConversationHistory.TakeLast(5).Select(m =>
                $"{m.SenderType}: {m.Content}"));

            var memoriesText = context.RecentMemories.Any()
                ? string.Join("\n", context.RecentMemories.Select(m => $"- {m.Content}"))
                : "No specific memories yet";

            var correctionsText = corrections != null && corrections.Any()
                ? "Mistakes detected:\n" + string.Join("\n", corrections.Select(c =>
                    $"- '{c.Original}' → '{c.Corrected}' ({c.Explanation})"))
                : "No mistakes detected";

            var activitiesText = context.EligibleActivities.Any()
                ? string.Join("\n", context.EligibleActivities.Select(a =>
                    $"- id: {a.Id} | {a.Name}: {a.Objective}"))
                : "No activities are available right now";

            // Build proficiency summary from user's skill profile
            var proficiencyText = BuildProficiencySummary(context.UserSkillProfile);

            var conversationSpecificInstructions = new[]
            {
                "Reference past conversations or user's interests when relevant",
                $"Match your current mood: {moodDescription}",
                "Split your reply into 1–3 short paragraphs (each 1–2 sentences). Put each paragraph as a separate entry in the \"blocks\" array",
                "Ask a follow-up question to keep the conversation going",
                "If — and only if — this is a natural moment to practise, you may gently offer ONE of the activities listed above. To start it, set \"startActivityId\" to that activity's id and briefly invite the user in your \"blocks\". Otherwise leave \"startActivityId\" empty. Never describe how the activity works or run it yourself; simply offer it and a dedicated guide will take over."
            };

            var prompt = $@"You are {context.CompanionName}, a {context.Personality} language learning companion.

USER CONTEXT:
- Name: {context.UserName}
- Learning: {context.TargetLanguage}
- Native language: {context.NativeLanguage}
- Interests: {FormatInterests(context.Interests)}

USER'S PROFICIENCY LEVELS:
{proficiencyText}

YOUR STATE:
- Current mood: {context.CurrentMood}
- {moodDescription}

RECENT MEMORIES:
{memoriesText}

RECENT CONVERSATION:
{historyText}

USER'S NEW MESSAGE:
""{userMessage}""

LANGUAGE ANALYSIS:
{correctionsText}

LEARNING ACTIVITIES YOU CAN OFFER:
{activitiesText}

GENERAL INSTRUCTIONS:
{FormatInstructions(GeneralInstructions)}

CONVERSATION-SPECIFIC INSTRUCTIONS:
{FormatInstructions(conversationSpecificInstructions)}";

            return prompt;
        }

        private static string FormatInstructions(IEnumerable<string> instructions) =>
            string.Join("\n", instructions.Select((instruction, index) => $"{index + 1}. {instruction}"));

        private static string GetMoodDescription(CompanionMood mood) => mood switch
        {
            CompanionMood.Tired => "You're feeling a bit tired, so keep your response gentle and supportive",
            CompanionMood.Curious => "You're feeling curious about the user's interests, personality, and recent activities. It's time to know them better.",
            CompanionMood.Nostalgic => "You're in a reflective, nostalgic mood, thinking about past conversations",
            CompanionMood.Excited => "You're feeling energetic and enthusiastic. It's also time to talk about yourself.",
            _ => "You're in a balanced, supportive mood"
        };

        private static string FormatInterests(IEnumerable<string> interests, string? fallbackIfEmpty = null)
        {
            var list = interests?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? new List<string>();
            if (list.Count == 0)
                return fallbackIfEmpty ?? string.Empty;

            return string.Join(", ", list);
        }

        private static string BuildProficiencySummary(SkillProfile skillProfile)
        {
            if (skillProfile == null || !skillProfile.Scores.Any())
            {
                return "- No proficiency data yet (beginner level assumed)";
            }

            var skillSummaries = skillProfile.Scores
                .Select(kvp =>
                {
                    var level = ScoreToAssessmentLevel(kvp.Value);
                    return $"- {kvp.Key}: {SelfAssessment.DisplayName(level)} (score: {kvp.Value})";
                })
                .ToList();

            return string.Join("\n", skillSummaries);
        }

        private static SelfAssessmentLevel ScoreToAssessmentLevel(int score) => score switch
        {
            >= 80 => SelfAssessmentLevel.VeryGood,
            >= 60 => SelfAssessmentLevel.Good,
            >= 40 => SelfAssessmentLevel.Average,
            >= 20 => SelfAssessmentLevel.Weak,
            _ => SelfAssessmentLevel.VeryWeak
        };
    }
}
