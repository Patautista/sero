using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Domain.Shared.Models
{
    /// <summary>
    /// A trainable language-proficiency dimension. New skills (e.g. Speaking or
    /// Pronunciation) can be added here without changing the surrounding system:
    /// storage, onboarding and activity selection all enumerate this type.
    /// </summary>
    public enum SkillType
    {
        Reading,
        Writing,
        Listening
    }

    /// <summary>
    /// Coarse self-rating a user gives for a skill during onboarding, used until
    /// an automatic placement test exists.
    /// </summary>
    public enum SelfAssessmentLevel
    {
        VeryWeak,
        Weak,
        Average,
        Good,
        VeryGood
    }

    /// <summary>
    /// Maps coarse self-assessment answers to concrete 0-100 skill scores.
    /// Intentionally simple so new test profiles can be created quickly.
    /// </summary>
    public static class SelfAssessment
    {
        /// <summary>All self-assessment options, weakest first.</summary>
        public static IReadOnlyList<SelfAssessmentLevel> Levels { get; } = Enum.GetValues<SelfAssessmentLevel>();

        public static int ToScore(SelfAssessmentLevel level) => level switch
        {
            SelfAssessmentLevel.VeryWeak => 10,
            SelfAssessmentLevel.Weak => 30,
            SelfAssessmentLevel.Average => 50,
            SelfAssessmentLevel.Good => 70,
            SelfAssessmentLevel.VeryGood => 90,
            _ => 0
        };

        public static string DisplayName(SelfAssessmentLevel level) => level switch
        {
            SelfAssessmentLevel.VeryWeak => "Very weak",
            SelfAssessmentLevel.Weak => "Weak",
            SelfAssessmentLevel.Average => "Average",
            SelfAssessmentLevel.Good => "Good",
            SelfAssessmentLevel.VeryGood => "Very good",
            _ => level.ToString()
        };
    }

    /// <summary>
    /// Independent skill scores (0-100) for a user. Backed by a dictionary keyed
    /// by <see cref="SkillType"/> so additional skills are supported automatically
    /// and scores are easy to update after each completed activity.
    /// </summary>
    public class SkillProfile
    {
        public const int MinScore = 0;
        public const int MaxScore = 100;

        private readonly Dictionary<SkillType, int> _scores;

        public SkillProfile()
        {
            _scores = new Dictionary<SkillType, int>();
            foreach (var skill in AllSkills)
            {
                _scores[skill] = MinScore;
            }
        }

        /// <summary>All skills currently modelled by the system.</summary>
        public static IReadOnlyList<SkillType> AllSkills { get; } = Enum.GetValues<SkillType>();

        public int this[SkillType skill]
        {
            get => _scores.TryGetValue(skill, out var score) ? score : MinScore;
            set => _scores[skill] = Clamp(value);
        }

        public IReadOnlyDictionary<SkillType, int> Scores => _scores;

        /// <summary>Average score across all modelled skills (the overall level).</summary>
        public double AverageScore => _scores.Count == 0 ? 0 : _scores.Values.Average();

        /// <summary>Skills ordered from weakest (lowest score) to strongest.</summary>
        public IReadOnlyList<SkillType> SkillsByWeakness =>
            _scores.OrderBy(kvp => kvp.Value)
                   .ThenBy(kvp => kvp.Key.ToString(), StringComparer.OrdinalIgnoreCase)
                   .Select(kvp => kvp.Key)
                   .ToList();

        /// <summary>The single weakest skill, or null when no skills are modelled.</summary>
        public SkillType? WeakestSkill => _scores.Count == 0 ? null : SkillsByWeakness[0];

        /// <summary>Increase (or decrease) a skill score, clamped to 0-100.</summary>
        public void Adjust(SkillType skill, int delta) => this[skill] = this[skill] + delta;

        /// <summary>True when this profile meets every minimum in <paramref name="minimums"/>.</summary>
        public bool Meets(IReadOnlyDictionary<SkillType, int>? minimums)
        {
            if (minimums is null)
                return true;

            foreach (var (skill, required) in minimums)
            {
                if (this[skill] < required)
                    return false;
            }

            return true;
        }

        public static int Clamp(int value) =>
            value < MinScore ? MinScore : value > MaxScore ? MaxScore : value;

        /// <summary>Builds a profile from onboarding self-assessment answers.</summary>
        public static SkillProfile FromSelfAssessment(IReadOnlyDictionary<SkillType, SelfAssessmentLevel>? answers)
        {
            var profile = new SkillProfile();
            if (answers is not null)
            {
                foreach (var (skill, level) in answers)
                {
                    profile[skill] = SelfAssessment.ToScore(level);
                }
            }

            return profile;
        }

        // --- JSON persistence helpers (keyed by skill name for forward-compatibility) ---

        /// <summary>Serialises to a name-keyed dictionary so new skills never break stored data.</summary>
        public Dictionary<string, int> ToStorage() =>
            _scores.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);

        public string ToJson() => JsonSerializer.Serialize(ToStorage());

        public static SkillProfile FromStorage(IReadOnlyDictionary<string, int>? stored)
        {
            var profile = new SkillProfile();
            if (stored is not null)
            {
                foreach (var (name, score) in stored)
                {
                    if (Enum.TryParse<SkillType>(name, ignoreCase: true, out var skill))
                    {
                        profile[skill] = score;
                    }
                }
            }

            return profile;
        }

        public static SkillProfile FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new SkillProfile();

            try
            {
                var stored = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                return FromStorage(stored);
            }
            catch (JsonException)
            {
                return new SkillProfile();
            }
        }
    }

    /// <summary>
    /// A reusable activity definition. Describes what an activity is, how it should
    /// be conducted, and how it is evaluated. The text fields (<see cref="Objective"/>,
    /// <see cref="Instructions"/>, <see cref="EvaluationCriteria"/>,
    /// <see cref="CompletionCriteria"/>) are meant to be injected into the Activity
    /// Agent prompt, so new activities can be added simply by creating new definitions
    /// without touching the conversation engine.
    /// </summary>
    public class LearningActivity
    {
        /// <summary>Stable identifier used to track instances and completions.</summary>
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        /// <summary>What the activity is trying to achieve for the learner.</summary>
        public string Objective { get; set; } = string.Empty;

        /// <summary>How the Activity Agent should run the activity, step by step.</summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>What the agent should look for when judging the learner's performance.</summary>
        public string EvaluationCriteria { get; set; } = string.Empty;

        /// <summary>The condition that marks the activity as finished.</summary>
        public string CompletionCriteria { get; set; } = string.Empty;

        /// <summary>Minimum skill scores required before this activity is offered.</summary>
        public Dictionary<SkillType, int> MinimumSkills { get; set; } = new();

        /// <summary>Skills this activity practises.</summary>
        public List<SkillType> TrainedSkills { get; set; } = new();

        /// <summary>Estimated difficulty, on the same 0-100 scale as skill scores.</summary>
        public int Difficulty { get; set; }
    }

    /// <summary>
    /// Pure, dependency-free activity selection. Chooses activities the user is
    /// eligible for, preferring those that train the user's weakest skills at a
    /// difficulty close to the user's current level.
    /// </summary>
    public static class ActivityRecommender
    {
        /// <summary>Relative weight of "trains weak skills" in the ranking (0-1).</summary>
        public const double WeaknessWeight = 0.6;

        /// <summary>Relative weight of "difficulty matches level" in the ranking (0-1).</summary>
        public const double DifficultyWeight = 0.4;

        /// <summary>Activities whose minimum-skill requirements the profile satisfies.</summary>
        public static IReadOnlyList<LearningActivity> GetEligible(
            SkillProfile profile,
            IEnumerable<LearningActivity> activities) =>
            activities.Where(a => profile.Meets(a.MinimumSkills)).ToList();

        /// <summary>The best activity for the profile, or null when none are eligible.</summary>
        public static LearningActivity? Recommend(
            SkillProfile profile,
            IEnumerable<LearningActivity> activities) =>
            GetEligible(profile, activities)
                .OrderByDescending(a => Score(profile, a))
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        /// <summary>
        /// Combined desirability score (higher is better): rewards training weak
        /// skills and a difficulty close to the user's overall level.
        /// </summary>
        public static double Score(SkillProfile profile, LearningActivity activity) =>
            WeaknessWeight * WeaknessScore(profile, activity) +
            DifficultyWeight * DifficultyScore(profile, activity);

        // Average "room to grow" (100 - score) across the trained skills: higher
        // when the activity trains skills the user is weak at.
        private static double WeaknessScore(SkillProfile profile, LearningActivity activity)
        {
            if (activity.TrainedSkills is null || activity.TrainedSkills.Count == 0)
                return 0;

            return activity.TrainedSkills.Average(skill => SkillProfile.MaxScore - profile[skill]);
        }

        // Closeness of the activity difficulty to the user's overall level: higher
        // when difficulty is neither far above nor far below the current level.
        private static double DifficultyScore(SkillProfile profile, LearningActivity activity) =>
            SkillProfile.MaxScore - Math.Abs(activity.Difficulty - profile.AverageScore);
    }
}
