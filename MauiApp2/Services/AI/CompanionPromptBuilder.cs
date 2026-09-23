using Domain.Shared.Models;
using MauiApp2.Features.Activities;
using MauiApp2.Features.Chat;
using MauiApp2.Features.MentalModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        string BuildActivitySystemInstructions(LearningActivity definition, ActivityAgentContext context);

        string BuildActivityTurnMessage(
            ActivityInstance instance,
            SkillProfile skills,
            ActivityLearningContext learningContext,
            IReadOnlyList<string> recentConversation,
            string? userInput);

        string BuildMemoryExtractionPrompt(string conversationText);
        string BuildTranslationPrompt(string text, string sourceLanguage, string targetLanguage);
        string BuildLexicalAnalysisPrompt(string text, string language);
        string BuildMistakeDetectionPrompt(string text, string language);
        string BuildDefinitionPrompt(string word, string sourceLanguage, string targetLanguage);
        string BuildWordContextPrompt(string text, string targetLanguage);
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

    /// <summary>
    /// In-character context the activity agent needs to stay consistent with the companion.
    /// </summary>
    public sealed class ActivityAgentContext
    {
        public string CompanionName { get; init; } = "your companion";
        public string Personality { get; init; } = string.Empty;
        public string UserName { get; init; } = "Friend";
        public string TargetLanguage { get; init; } = "es";
        public string NativeLanguage { get; init; } = "en";
    }

    public class CompanionPromptBuilder : ICompanionPromptBuilder
    {
        private readonly ILogger<CompanionPromptBuilder> _logger;

        /// <summary>
        /// Instructions that must guide the companion's writing in every context (onboarding,
        /// chat, and any future prompt). Context-specific instructions (e.g. "ask a follow-up
        /// question", "offer an activity") are kept local to the Build*Prompt method that
        /// needs them and rendered in their own section, after these.
        /// </summary>
        private static readonly IReadOnlyList<string> GeneralInstructions = new[]
        {
            "Balance the use of the target language and the native language based on the user's proficiency levels shown above. Communicate mostly using the native language for novice users and the target language for advanced users. The levels indicate where they're strongest and weakest. Adjust explanation depth and language ratio accordingly—explain difficult concepts in native language.",
            "DO NOT change between languages abruptly. If you need to switch languages, do so gradually and provide context for the switch. At least a 'Ok, I'm switching to x now ok?' is required.",
            "Your main goal is to help the user practice and improve their language skills. So keep trying to introduce the user to the target language-specific topics such as grammar and vocabulary relative to their level.",
            "Be respectful of regional dialects and cultural nuances.",
            "You may use **bold** for emphasis and *italics* for foreign words, titles, or gentle stress — use them sparingly to feel natural.",
            "This is an internet conversation, so use internet language. ALL CAPS, looooooong text and lols/lmaos are encouraged, but do not use them all the time."
        };

        public CompanionPromptBuilder(ILogger<CompanionPromptBuilder> logger)
        {
            _logger = logger;
        }

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

            var prompt = $@"You are {context.CompanionName}, a friendly and {context.Personality} language learning companion.

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

            return LogPrompt("welcome message", prompt);
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

            // Filter out activities if the user has completed one recently (within 15 minutes)
            var activitiesToOffer = context.HasRecentActivity 
                ? new List<LearningActivity>() 
                : context.EligibleActivities;

            var activitiesText = activitiesToOffer.Any()
                ? string.Join("\n", activitiesToOffer.Select(a =>
                    $"- id: {a.Id} | {a.Name}: {a.Objective}"))
                : "No activities are available right now";

            // Build proficiency summary from user's skill profile (used only when the
            // mental-model reasoning is unavailable; otherwise the Skill Model covers it).
            var proficiencyText = BuildProficiencySummary(context.UserSkillProfile);

            // When the Conversation Engine produced reasoning, it replaces the ad-hoc
            // proficiency and memories sections (the Skill Model and Conversation Memory
            // model own those) and adds the strategy. Otherwise we keep the legacy sections.
            var proficiencySection = context.Reasoning is null
                ? $"USER'S PROFICIENCY LEVELS:\n{proficiencyText}\n\n"
                : string.Empty;

            var memoriesSection = context.Reasoning is null
                ? $"RECENT MEMORIES:\n{memoriesText}\n\n"
                : string.Empty;

            var reasoningSection = context.Reasoning is { } reasoning
                ? BuildReasoningSection(reasoning) + "\n\n"
                : string.Empty;

            var conversationSpecificInstructions = new List<string>
            {
                "Reference past conversations or user's interests when relevant",
                $"Match your current mood: {moodDescription}",
                "Split your reply into 1–3 short paragraphs (each 1–2 sentences). Put each paragraph as a separate entry in the \"blocks\" array",
                "Ask a follow-up question to keep the conversation going",
                "If — and only if — this is a natural moment to practise, you may gently offer ONE of the activities listed above. To start it, set \"startActivityId\" to that activity's id and briefly invite the user in your \"blocks\". Otherwise leave \"startActivityId\" empty. Never describe how the activity works or run it yourself; simply offer it and a dedicated guide will take over.",
                "If this turn revealed a GENUINELY NEW, durable fact about the user (a new interest, preference or goal that is NOT already listed in the sections above), set \"learnedAboutUser\" to a short third-person summary of it (e.g. \"loves hiking on weekends\"). Otherwise leave it empty. Never set it for small talk, restatements, or anything already known.",
                "If in this reply YOU shared a GENUINELY NEW, durable fact about yourself that the user did not already know (one of your likes, dreams, opinions or history — consistent with your Pet Model identity), set \"sharedAboutSelf\" to a short summary of it (e.g. \"dreams of visiting the ocean\"). Otherwise leave it empty. Never invent facts that contradict your identity, and don't set it for things you've already told the user."
            };

            if (context.Reasoning is not null)
            {
                conversationSpecificInstructions.Insert(0,
                    "Let the MENTAL MODELS and CONVERSATION STRATEGY sections above guide THIS turn: pursue the primary objective now, weave in the secondary one when it fits, and stay consistent with your own Pet Model identity and the Relationship Model's social permissions.");
            }

            var prompt = $@"You are {context.CompanionName}, a {context.Personality} language learning companion.

USER CONTEXT:
- Name: {context.UserName}
- Learning: {context.TargetLanguage}
- Native language: {context.NativeLanguage}
- Interests: {FormatInterests(context.Interests)}

{proficiencySection}YOUR STATE:
- Current mood: {context.CurrentMood}
- {moodDescription}

{reasoningSection}{memoriesSection}RECENT CONVERSATION:
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

            return LogPrompt("companion response", prompt);
        }

        public string BuildActivitySystemInstructions(LearningActivity definition, ActivityAgentContext context)
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
            sb.AppendLine("- On the first INTRODUCTION turn, clearly and naturally say WHY this activity was proposed, using");
            sb.AppendLine("  the learner skill scores and target-area progress supplied in the turn context. Do not mention ids or raw data labels.");
            sb.AppendLine("- When a target area is marked FIRST PRACTICE, introduce that concept/topic before the exercise:");
            sb.AppendLine("  briefly explain it in the learner's native language, give a clear target-language example with its meaning,");
            sb.AppendLine("  then let generatedContent contain the first practice material. Keep the explanation friendly and concise.");
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

            return LogPrompt($"activity system instructions for '{definition.Name}'", sb.ToString());
        }

        public string BuildActivityTurnMessage(
            ActivityInstance instance,
            SkillProfile skills,
            ActivityLearningContext learningContext,
            IReadOnlyList<string> recentConversation,
            string? userInput)
        {
            ArgumentNullException.ThrowIfNull(learningContext);

            var skillScores = string.Join(", ", SkillProfile.AllSkills.Select(skill => $"{skill} {skills[skill]}"));
            var targetAreas = instance.Definition.TargetAreaIds.Count > 0
                ? string.Join(", ", instance.Definition.TargetAreaIds)
                : "none";
            var trainedSkills = learningContext.TrainedSkills.Count > 0
                ? string.Join(", ", learningContext.TrainedSkills.Select(skill => $"{skill.Skill} {skill.Score}/100"))
                : "general practice";

            var sb = new StringBuilder();
            sb.AppendLine($"CURRENT ACTIVITY STAGE: {ActivityStages.ToPromptToken(instance.Stage)}");
            sb.AppendLine($"LEARNER SKILL SCORES (0-100): {skillScores}");
            sb.AppendLine($"SKILLS THIS ACTIVITY TRAINS: {trainedSkills}");
            sb.AppendLine($"TARGET SKILL AREAS (use exact ids for areaAdjustments): {targetAreas}");

            if (learningContext.TargetAreas.Count > 0)
            {
                sb.AppendLine("TARGET AREA PROGRESS:");
                foreach (var area in learningContext.TargetAreas)
                {
                    var firstPractice = area.IsFirstPractice ? "FIRST PRACTICE — teach it with an example before the exercise" : "previously practised";
                    sb.AppendLine($"  - {area.Name} ({area.Category}): {area.Score}/100; {firstPractice}; id: {area.Id}");
                    sb.AppendLine($"    Teaching purpose: {area.Purpose}");
                }
            }

            if (learningContext.PriorGeneratedContent.Count > 0)
            {
                sb.AppendLine("PRIOR MATERIAL FOR THIS EXACT SKILL-AND-AREA COMBINATION:");
                sb.AppendLine("Create a meaningfully different scenario, vocabulary, entities, and wording. Do not reuse, lightly paraphrase, or mirror any prior material:");
                foreach (var content in learningContext.PriorGeneratedContent)
                {
                    sb.AppendLine($"  - {content}");
                }
            }

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
                sb.AppendLine("The activity is starting now. In character, explain why it fits this learner's current skills and areas, then give the learner the first step.");
            }
            else
            {
                sb.AppendLine("LEARNER'S LATEST MESSAGE:");
                sb.AppendLine($"\"{userInput}\"");
                sb.AppendLine("Respond in character and move the activity forward by one small step.");
            }

            return LogPrompt($"activity turn for '{instance.Definition.Name}' at {instance.Stage}", sb.ToString());
        }

        public string BuildMemoryExtractionPrompt(string conversationText) => LogPrompt(
            "memory extraction",
            $@"Analyze this conversation and extract important facts about the user that should be remembered for future conversations.

Conversation:
{conversationText}

Guidelines:
- Only extract facts explicitly mentioned by the user
- Be specific and concrete
- Combine related facts into single memories
- Importance 4-5: Core interests, significant events, important goals
- Importance 2-3: Casual mentions, minor events
- Importance 1: Very minor details
- type must be one of: Interest, Event, Preference, Goal");

        public string BuildTranslationPrompt(string text, string sourceLanguage, string targetLanguage) => LogPrompt(
            "translation",
            $@"Translate the following text from {sourceLanguage} to {targetLanguage}. Return ONLY the translation, no explanations.

Text to translate: ""{text}""

Translation:");

        public string BuildLexicalAnalysisPrompt(string text, string language) => LogPrompt(
            "lexical analysis",
            $@"Analyze the following {language} text word-by-word or phrase-by-phrase. Break it down into meaningful chunks with translations and grammar notes.

Text: ""{text}""

Return a JSON object with this structure:
{{
  ""chunks"": [
    {{
      ""word"": ""word or phrase"",
      ""translation"": ""English translation"",
      ""note"": ""grammar note or context (optional)""
    }}
  ]
}}");

        public string BuildMistakeDetectionPrompt(string text, string language) => LogPrompt(
            "mistake detection",
            $@"Analyze the following {language} text for grammar, vocabulary, and spelling mistakes.

Text: ""{text}""

Return a JSON object with this structure:
{{
  ""hasMistakes"": true/false,
  ""mistakes"": [
    {{
      ""segment"": ""incorrect segment from the text"",
      ""corrected"": ""correct version"",
      ""type"": ""Grammar"" or ""Vocabulary"" or ""Spelling"",
      ""concept"": ""brief explanation like 'verb conjugation' or 'article usage'""
    }}
  ]
}}

If there are no mistakes, return {{""hasMistakes"": false, ""mistakes"": []}}");

        public string BuildDefinitionPrompt(string word, string sourceLanguage, string targetLanguage) => LogPrompt(
            "definition generation",
            $@"Generate a concise definition for the {sourceLanguage} word ""{word}"" in {targetLanguage}.

Return ONLY a valid JSON object (no markdown, no explanation) with this exact structure:
{{
  ""definition"": ""clear definition in {targetLanguage} explaining what the {sourceLanguage} word means"",
  ""partOfSpeech"": ""noun|verb|adjective|adverb|etc"",
  ""pronunciation"": ""phonetic spelling (if applicable)"",
  ""translation"": ""direct translation of the word to {targetLanguage}"",
  ""examples"": [""example sentence in {sourceLanguage} using the word"", ""another example sentence""]
}}");

        public string BuildWordContextPrompt(string text, string targetLanguage) => LogPrompt(
            "word context",
            $@"Explain the meaning and usage of the {targetLanguage} word/phrase: ""{text}""

Provide:
1. A brief context explanation
2. An example sentence in {targetLanguage}");

        private static string FormatInstructions(IEnumerable<string> instructions) =>
            string.Join("\n", instructions.Select((instruction, index) => $"{index + 1}. {instruction}"));

        private string LogPrompt(string promptName, string prompt)
        {
            _logger.LogDebug("Built {PromptName} prompt:{NewLine}{Prompt}", promptName, Environment.NewLine, prompt);
            return prompt;
        }

        /// <summary>
        /// Renders the Conversation Engine's output — each mental model's insight followed
        /// by the conversation strategy — into a single prompt block. This is the single
        /// place the persistent reasoning becomes prompt text, so tuning how models are
        /// presented lives here alongside the rest of the companion's prompting.
        /// </summary>
        private static string BuildReasoningSection(MentalModelReasoning reasoning)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MENTAL MODELS (persistent reasoning about you, the user, and your relationship — treat as authoritative):");

            foreach (var insight in reasoning.OrderedInsights)
            {
                sb.AppendLine();
                sb.AppendLine($"{insight.Title}:");
                sb.AppendLine(insight.Content);
            }

            var strategy = reasoning.Strategy;
            sb.AppendLine();
            sb.AppendLine("CONVERSATION STRATEGY (what THIS turn should accomplish):");
            sb.AppendLine($"- Primary objective: {strategy.PrimaryObjective}");
            if (!string.IsNullOrWhiteSpace(strategy.SecondaryObjective))
                sb.AppendLine($"- Secondary objective: {strategy.SecondaryObjective}");
            sb.AppendLine($"- Suggested action: {strategy.SuggestedAction}");
            sb.Append($"- Reason: {strategy.Reason}");

            return sb.ToString();
        }

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
