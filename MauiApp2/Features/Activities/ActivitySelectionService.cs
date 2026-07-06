using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Shared.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// Proposes learning activities based on the user's current skill scores and
    /// applies score updates after an activity is completed. Selection prefers
    /// activities that train the user's weakest skills at a difficulty close to
    /// their current level, and only offers activities the user is eligible for.
    /// </summary>
    public class ActivitySelectionService
    {
        /// <summary>Default score gain applied to each trained skill on completion.</summary>
        public const int DefaultSkillGain = 5;

        private readonly PetDbContext _db;
        private readonly ILogger<ActivitySelectionService> _logger;

        public ActivitySelectionService(PetDbContext db, ILogger<ActivitySelectionService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>Activities the given profile currently meets the minimum requirements for.</summary>
        public IReadOnlyList<LearningActivity> GetEligibleActivities(SkillProfile skills) =>
            ActivityRecommender.GetEligible(skills, ActivityCatalog.GetActivities());

        /// <summary>The best next activity for the given profile, or null if none are eligible.</summary>
        public LearningActivity? RecommendActivity(SkillProfile skills) =>
            ActivityRecommender.Recommend(skills, ActivityCatalog.GetActivities());

        /// <summary>Loads the current user's skills and recommends the best next activity.</summary>
        public async Task<LearningActivity?> GetRecommendedActivityAsync()
        {
            var profile = await _db.UserProfiles.FirstOrDefaultAsync();
            if (profile is null)
            {
                _logger.LogWarning("No user profile found; cannot recommend an activity.");
                return null;
            }

            return RecommendActivity(SkillProfile.FromJson(profile.SkillsJson));
        }

        /// <summary>
        /// Applies skill gains for a completed activity to the current user profile and
        /// persists them. Returns the updated skill profile, or null if no profile exists.
        /// </summary>
        public async Task<SkillProfile?> RecordActivityCompletionAsync(
            LearningActivity activity,
            int gainPerSkill = DefaultSkillGain)
        {
            ArgumentNullException.ThrowIfNull(activity);

            var profileTable = await _db.UserProfiles.FirstOrDefaultAsync();
            if (profileTable is null)
            {
                _logger.LogWarning("No user profile found; cannot record activity completion.");
                return null;
            }

            var skills = SkillProfile.FromJson(profileTable.SkillsJson);
            foreach (var skill in activity.TrainedSkills)
            {
                skills.Adjust(skill, gainPerSkill);
            }

            profileTable.SkillsJson = skills.ToJson();
            profileTable.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Recorded completion of '{Activity}'; updated skills: {Skills}",
                activity.Name,
                profileTable.SkillsJson);

            return skills;
        }
    }
}
