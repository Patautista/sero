using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Shared.Models;
using Infrastructure.Data.Repositories;
using MauiApp2.Features.Skills;
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
        private readonly ISkillAreaCatalogProvider _skillAreaCatalogProvider;
        private readonly ILogger<ActivitySelectionService> _logger;

        public ActivitySelectionService(
            IPetDataStore store,
            ISkillAreaCatalogProvider skillAreaCatalogProvider,
            ILogger<ActivitySelectionService> logger)
        {
            _store = store;
            _skillAreaCatalogProvider = skillAreaCatalogProvider;
            _logger = logger;
        }

        /// <summary>Activities the given profile currently meets the minimum requirements for.</summary>
        public IReadOnlyList<LearningActivity> GetEligibleActivities(SkillProfile skills) =>
            ActivityRecommender.GetEligible(skills, ActivityCatalog.GetActivities());

        /// <summary>
        /// Eligible activities ordered to favour the learner's current frontier skill areas.
        /// </summary>
        public IReadOnlyList<LearningActivity> GetEligibleActivities(
            SkillProfile skills,
            AreaProgress areaProgress,
            SkillAreaCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(areaProgress);
            ArgumentNullException.ThrowIfNull(catalog);

            var frontierIds = areaProgress.GetFrontier(catalog)
                .Select(a => a.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return ActivityRecommender.GetEligible(skills, ActivityCatalog.GetActivities())
                .OrderByDescending(a => ActivityRecommender.Score(skills, a, frontierIds))
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>The best next activity for the given profile, or null if none are eligible.</summary>
        public LearningActivity? RecommendActivity(SkillProfile skills) =>
            ActivityRecommender.Recommend(skills, ActivityCatalog.GetActivities());

        public LearningActivity? RecommendActivity(
            SkillProfile skills,
            AreaProgress areaProgress,
            SkillAreaCatalog catalog) =>
            ActivityRecommender.Recommend(skills, areaProgress, catalog, ActivityCatalog.GetActivities());

        /// <summary>Loads the current user's skills and recommends the best next activity.</summary>
        public async Task<LearningActivity?> GetRecommendedActivityAsync()
        {
            var profile = await _store.UserProfiles.FirstOrDefaultAsync(p => true);
            if (profile is null)
            {
                _logger.LogWarning("No user profile found; cannot recommend an activity.");
                return null;
            }

            var catalog = await _skillAreaCatalogProvider.GetCatalogAsync();
            return RecommendActivity(
                profile.Skills,
                profile.AreaProgress,
                catalog);
        }

        /// <summary>Loads the current user's skill profile, or an empty profile when none exists.</summary>
        public async Task<SkillProfile> GetCurrentSkillProfileAsync()
        {
            var profile = await _store.UserProfiles.FirstOrDefaultAsync(p => true);
            return profile?.Skills ?? new SkillProfile();
        }

        /// <summary>
        /// Applies structured skill and area-progress adjustments produced by the Activity
        /// Agent's evaluation to the current user profile and persists them. Area ids are
        /// validated against the packaged skill-area catalog before they are accepted.
        /// </summary>
        public async Task<(SkillProfile Skills, AreaProgress AreaProgress)?> ApplyActivityAdjustmentsAsync(
            IReadOnlyDictionary<SkillType, int> skillAdjustments,
            IReadOnlyDictionary<string, int>? areaAdjustments)
        {
            ArgumentNullException.ThrowIfNull(skillAdjustments);

            var profileTable = await _store.UserProfiles.FirstOrDefaultAsync(p => true);
            if (profileTable is null)
            {
                _logger.LogWarning("No user profile found; cannot apply activity adjustments.");
                return null;
            }

            var skills = profileTable.Skills;
            foreach (var (skill, delta) in skillAdjustments)
            {
                if (delta != 0)
                {
                    skills.Adjust(skill, delta);
                }
            }

            var areaProgress = profileTable.AreaProgress;
            if (areaAdjustments is { Count: > 0 })
            {
                var catalog = await _skillAreaCatalogProvider.GetCatalogAsync();
                foreach (var (areaId, delta) in areaAdjustments)
                {
                    if (delta == 0 || string.IsNullOrWhiteSpace(areaId) || !catalog.Contains(areaId))
                    {
                        continue;
                    }

                    areaProgress.Adjust(areaId, delta);
                }
            }

            profileTable.LastActiveAt = DateTime.UtcNow;
            _store.UserProfiles.Update(profileTable);
            await _store.SaveChangesAsync();

            _logger.LogInformation(
                "Applied activity adjustments; updated skills: {Skills}; updated area progress: {AreaProgress}",
                profileTable.Skills.ToStorage(),
                profileTable.AreaProgress.ToStorage());

            return (skills, areaProgress);
        }
    }
}
