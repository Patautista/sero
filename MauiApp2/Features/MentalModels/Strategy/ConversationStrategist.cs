using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;

namespace MauiApp2.Features.MentalModels.Strategy
{
    /// <summary>
    /// The Conversation Strategy model — the planner. It consumes the reasoning produced
    /// by every other mental model (surfaced by the engine as <see cref="MentalModelInsight"/>s
    /// and materialised in the shared request) and decides what the next companion turn
    /// should accomplish. This deterministic implementation prioritises practising the
    /// user's weakest skill while weaving in their strongest interest, and degrades
    /// gracefully to getting to know the user or casual conversation when signals are thin.
    /// </summary>
    public sealed class ConversationStrategist : IConversationStrategist
    {
        /// <summary>Below this score a skill is considered worth actively practising.</summary>
        public const int PracticeThreshold = 60;

        public Task<ConversationStrategyPlan> PlanAsync(
            MentalModelRequest request,
            IReadOnlyList<MentalModelInsight> insights,
            CancellationToken cancellationToken = default)
        {
            var plan = BuildPlan(request);
            return Task.FromResult(plan);
        }

        private static ConversationStrategyPlan BuildPlan(MentalModelRequest request)
        {
            var skills = request.Skills;
            var weakest = skills.WeakestSkill;
            var topInterest = request.Interests.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
            var recommendedActivity = RecommendActivityFor(weakest, request.EligibleActivities);
            var topChallenge = request.ActiveChallenges.FirstOrDefault();

            if (topChallenge is not null)
            {
                var challengeActivity = RecommendActivityFor(SkillType.Writing, request.EligibleActivities);
                var primary = $"Help {request.UserName} master '{topChallenge.Concept}' — {topChallenge.ConsecutiveCorrectCount}/{PracticeChallenge.MasteryThreshold} correct uses in a row.";
                var secondary = topInterest is not null
                    ? $"Keep the practice grounded in the user's interest in {topInterest}."
                    : "Keep the practice feeling natural and encouraging.";

                var action = $"Naturally steer the conversation toward a reply that requires {topChallenge.Concept} ({topChallenge.MistakeType})"
                    + (topInterest is not null ? $" while talking about {topInterest}." : ".");

                if (topChallenge.OccurrenceCount >= 4 && challengeActivity is not null)
                {
                    action += $" If the learner still struggles, offer the '{challengeActivity.Name}' activity ({challengeActivity.Objective}) as a more focused intervention.";
                }

                var reason = topChallenge.OccurrenceCount >= 4
                    ? $"{topChallenge.Concept} has kept recurring, so the next turn should both exercise it naturally and be ready to escalate into a focused activity."
                    : $"{topChallenge.Concept} is the highest-priority active challenge, so reinforcing it now gives the learner the clearest path to mastery.";

                return new ConversationStrategyPlan
                {
                    PrimaryObjective = primary,
                    SecondaryObjective = secondary,
                    SuggestedAction = action,
                    Reason = reason
                };
            }

            // Primary objective: practise the weakest skill when it needs work.
            if (weakest is { } weakSkill && skills[weakSkill] < PracticeThreshold)
            {
                var primary = $"Practise {weakSkill.ToString().ToLowerInvariant()} — currently the weakest skill ({skills[weakSkill]}/100).";
                var secondary = topInterest is not null
                    ? $"Learn more about the user's interest in {topInterest}."
                    : "Get to know one new thing about the user.";

                var action = recommendedActivity is not null
                    ? $"When it feels natural, offer the '{recommendedActivity.Name}' activity ({recommendedActivity.Objective})"
                        + (topInterest is not null ? $", ideally themed around {topInterest}." : ".")
                    : $"Steer the exchange toward a light {weakSkill.ToString().ToLowerInvariant()} challenge"
                        + (topInterest is not null ? $" using {topInterest} as the topic." : ".");

                var reason = topInterest is not null
                    ? $"{weakSkill} is the weakest skill and {topInterest} is a high-interest topic to keep motivation high."
                    : $"{weakSkill} is the weakest skill, so gentle practice there yields the most progress.";

                return new ConversationStrategyPlan
                {
                    PrimaryObjective = primary,
                    SecondaryObjective = secondary,
                    SuggestedAction = action,
                    Reason = reason
                };
            }

            // Secondary path: skills are solid but we barely know the user yet.
            var knowsUser = request.Interests.Any() || request.RecentMemories.Any();
            if (!knowsUser)
            {
                return new ConversationStrategyPlan
                {
                    PrimaryObjective = "Learn something new about the user.",
                    SecondaryObjective = "Reinforce recent vocabulary through natural use.",
                    SuggestedAction = "Ask one open, friendly question about their life, work or hobbies.",
                    Reason = "There is little stored knowledge about the user, so building the User Model comes first."
                };
            }

            // Default: keep a warm, casual conversation going, anchored on an interest.
            return new ConversationStrategyPlan
            {
                PrimaryObjective = "Have a warm, casual conversation that keeps the user engaged.",
                SecondaryObjective = topInterest is not null
                    ? $"Continue exploring {topInterest}."
                    : "Continue the previous discussion or introduce a light new topic.",
                SuggestedAction = topInterest is not null
                    ? $"Pick up the thread on {topInterest} and ask a follow-up question."
                    : "React to the user's last message and ask a follow-up question.",
                Reason = "Skills are in good shape and the user is known, so maintaining engagement matters most right now."
            };
        }

        private static LearningActivity? RecommendActivityFor(SkillType? weakest, IReadOnlyList<LearningActivity> eligible)
        {
            if (eligible.Count == 0)
                return null;

            if (weakest is { } skill)
            {
                var forWeakest = eligible.FirstOrDefault(a => a.TrainedSkills.Contains(skill));
                if (forWeakest is not null)
                    return forWeakest;
            }

            return eligible[0];
        }
    }
}
