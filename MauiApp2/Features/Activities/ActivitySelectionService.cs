using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Shared.Models;
using Infrastructure.Data.Repositories;
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

        private readonly IPetDataStore _store;
        private readonly ILogger<ActivitySelectionService> _logger;

        public ActivitySelectionService(IPetDataStore store, ILogger<ActivitySelectionService> logger)
        {
            _store = store;
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
            var profile = await _store.UserProfiles.FirstOrDefaultAsync(p => true);
            if (profile is null)
            {
                _logger.LogWarning("No user profile found; cannot recommend an activity.");
                return null;
            }

            return RecommendActivity(SkillProfile.FromJson(profile.SkillsJson));
        }

        /// <summary>Loads the current user's skill profile, or an empty profile when none exists.</summary>
        public async Task<SkillProfile> GetCurrentSkillProfileAsync()
        {
            var profile = await _store.UserProfiles.FirstOrDefaultAsync(p => true);
            return SkillProfile.FromJson(profile?.SkillsJson);
        }

        /// <summary>
        /// Applies structured per-skill adjustments produced by the Activity Agent's
        /// evaluation to the current user profile and persists them. Deltas may be
        /// negative. Returns the updated skill profile, or null if no profile exists.
        /// This keeps skill persistence owned by the selection service, independent of
        /// how the Activity Agent decided the adjustments.
        /// </summary>
        public async Task<SkillProfile?> ApplySkillAdjustmentsAsync(
            IReadOnlyDictionary<SkillType, int> skillAdjustments)
        {
            ArgumentNullException.ThrowIfNull(skillAdjustments);

            var profileTable = await _store.UserProfiles.FirstOrDefaultAsync(p => true);
            if (profileTable is null)
            {
                _logger.LogWarning("No user profile found; cannot apply skill adjustments.");
                return null;
            }

            var skills = SkillProfile.FromJson(profileTable.SkillsJson);
            foreach (var (skill, delta) in skillAdjustments)
            {
                if (delta != 0)
                {
                    skills.Adjust(skill, delta);
                }
            }

            profileTable.SkillsJson = skills.ToJson();
            profileTable.LastActiveAt = DateTime.UtcNow;
            await _store.SaveChangesAsync();

            _logger.LogInformation(
                "Applied activity skill adjustments; updated skills: {Skills}",
                profileTable.SkillsJson);

            return skills;
        }
    }
}
