using System.Collections.Generic;
using Domain.Shared.Models;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// The set of learning activities the app can propose. Each entry declares the
    /// minimum skills required to attempt it, the skills it trains, and an estimated
    /// difficulty (0-100). Add new activities here; the selection logic adapts
    /// automatically as more skills are introduced.
    /// </summary>
    public static class ActivityCatalog
    {
        public static IReadOnlyList<LearningActivity> GetActivities() => new List<LearningActivity>
        {
            new LearningActivity
            {
                Id = "warmup-reading",
                Name = "Warm-up Reading",
                Objective = "Ease the learner into reading with a short, friendly sentence and check they understood it.",
                Instructions = "Share one very short, everyday sentence in the target language. Ask the learner what they think it means. Offer a gentle hint if they hesitate, then confirm the meaning warmly.",
                EvaluationCriteria = "Whether the learner grasped the overall meaning of the sentence, even if their wording is imperfect.",
                CompletionCriteria = "The learner has given their understanding of the sentence and received confirmation or a correction.",
                MinimumSkills = new(),
                TrainedSkills = new() { SkillType.Reading },
                TargetAreaIds = new() { "introductions", "present-simple" },
                Difficulty = 10
            },
            new LearningActivity
            {
                Id = "listen-and-answer",
                Name = "Listen and Answer",
                Objective = "Practise listening comprehension by answering a simple question about a short spoken line.",
                Instructions = "Put ONLY the literal line to be spoken aloud in \"generatedContent\" (a short, natural, everyday sentence in the target language - no narration, quotes, or stage directions). In \"blocks\", ask one simple comprehension question about that line, without repeating its text. Encourage the learner to answer in the target language and react to their answer.",
                EvaluationCriteria = "Whether the learner correctly understood the spoken line and answered the question appropriately.",
                CompletionCriteria = "The learner has answered the comprehension question and received feedback.",
                MinimumSkills = new() { [SkillType.Listening] = 10 },
                TrainedSkills = new() { SkillType.Listening },
                TargetAreaIds = new() { "questions", "daily-life" },
                Difficulty = 25
            },
            new LearningActivity
            {
                Id = "describe-picture",
                Name = "Describe Picture",
                Objective = "Build descriptive writing by having the learner describe a simple scene in a few sentences.",
                Instructions = "Describe a simple everyday scene in words and ask the learner to write two or three sentences describing it in the target language. Gently model richer vocabulary where helpful.",
                EvaluationCriteria = "Sentence correctness, relevant vocabulary use, and how well the description matches the scene.",
                CompletionCriteria = "The learner has written at least two descriptive sentences and received feedback.",
                MinimumSkills = new() { [SkillType.Writing] = 20 },
                TrainedSkills = new() { SkillType.Writing },
                TargetAreaIds = new() { "adjectives", "colors" },
                Difficulty = 35
            },
            new LearningActivity
            {
                Id = "read-short-story",
                Name = "Read a Short Story",
                Objective = "Strengthen reading comprehension with a short story followed by a comprehension check.",
                Instructions = "Generate a short, level-appropriate story (3-5 sentences) in the target language. Ask one or two questions about it and discuss the answers with the learner.",
                EvaluationCriteria = "How accurately the learner understood the story's events and details.",
                CompletionCriteria = "The learner has answered the comprehension questions and the answers have been discussed.",
                MinimumSkills = new() { [SkillType.Reading] = 25 },
                TrainedSkills = new() { SkillType.Reading },
                TargetAreaIds = new() { "past", "daily-life" },
                Difficulty = 40
            },
            new LearningActivity
            {
                Id = "read-and-summarize",
                Name = "Read and Summarize",
                Objective = "Develop reading and writing together by summarizing a short passage in the learner's own words.",
                Instructions = "Provide a short passage (4-6 sentences) in the target language, then ask the learner to summarize it in one or two sentences. Give feedback on both comprehension and phrasing.",
                EvaluationCriteria = "Whether the summary captures the main idea and is expressed with reasonably correct writing.",
                CompletionCriteria = "The learner has produced a summary and received feedback on it.",
                MinimumSkills = new() { [SkillType.Reading] = 40, [SkillType.Writing] = 30 },
                TrainedSkills = new() { SkillType.Reading, SkillType.Writing },
                TargetAreaIds = new() { "facts", "science" },
                Difficulty = 60
            },
            new LearningActivity
            {
                Id = "write-journal-entry",
                Name = "Write a Journal Entry",
                Objective = "Encourage free expressive writing through a short personal journal entry.",
                Instructions = "Suggest a simple journal prompt (e.g. their day, a favourite meal) and ask the learner to write a few sentences in the target language. Respond encouragingly and offer one or two refinements.",
                EvaluationCriteria = "Fluency, correctness, and expressiveness of the journal entry relative to the learner's level.",
                CompletionCriteria = "The learner has written a short journal entry and received encouraging feedback.",
                MinimumSkills = new() { [SkillType.Writing] = 45 },
                TrainedSkills = new() { SkillType.Writing },
                TargetAreaIds = new() { "past", "feelings", "daily-life" },
                Difficulty = 65
            }
        };

        /// <summary>Finds an activity definition by its stable id, or null when unknown.</summary>
        public static LearningActivity? FindById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (var activity in GetActivities())
            {
                if (string.Equals(activity.Id, id, System.StringComparison.OrdinalIgnoreCase))
                    return activity;
            }

            return null;
        }
    }
}
