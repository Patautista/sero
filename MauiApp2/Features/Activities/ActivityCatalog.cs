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
                Name = "Warm-up Reading",
                MinimumSkills = new(),
                TrainedSkills = new() { SkillType.Reading },
                Difficulty = 10
            },
            new LearningActivity
            {
                Name = "Listen and Answer",
                MinimumSkills = new() { [SkillType.Listening] = 10 },
                TrainedSkills = new() { SkillType.Listening },
                Difficulty = 25
            },
            new LearningActivity
            {
                Name = "Describe Picture",
                MinimumSkills = new() { [SkillType.Writing] = 20 },
                TrainedSkills = new() { SkillType.Writing },
                Difficulty = 35
            },
            new LearningActivity
            {
                Name = "Read a Short Story",
                MinimumSkills = new() { [SkillType.Reading] = 25 },
                TrainedSkills = new() { SkillType.Reading },
                Difficulty = 40
            },
            new LearningActivity
            {
                Name = "Transcribe Audio",
                MinimumSkills = new() { [SkillType.Listening] = 30 },
                TrainedSkills = new() { SkillType.Listening, SkillType.Writing },
                Difficulty = 45
            },
            new LearningActivity
            {
                Name = "Read and Summarize",
                MinimumSkills = new() { [SkillType.Reading] = 40, [SkillType.Writing] = 30 },
                TrainedSkills = new() { SkillType.Reading, SkillType.Writing },
                Difficulty = 60
            },
            new LearningActivity
            {
                Name = "Write a Journal Entry",
                MinimumSkills = new() { [SkillType.Writing] = 45 },
                TrainedSkills = new() { SkillType.Writing },
                Difficulty = 65
            }
        };
    }
}
