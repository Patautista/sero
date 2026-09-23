using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared.Models;

namespace MauiApp2.Features.MentalModels.Models
{
    /// <summary>
    /// Maintains hypotheses about the user's language abilities rather than storing
    /// isolated facts. It reports per-skill scores, infers qualitative observations
    /// from active mistake-driven practice challenges, and highlights what should be
    /// practised next and which activities are appropriate. Answers: "What can this
    /// user comfortably do?", "What should be practised next?", "Which activities fit?".
    /// </summary>
    public sealed class SkillModel : IMentalModel
    {
        public string Name => "Skill Model";

        public Task<MentalModelInsight?> ObserveAsync(MentalModelRequest request, CancellationToken cancellationToken = default)
        {
            var skills = request.Skills;
            var sb = new StringBuilder();

            if (skills.Scores.Any())
            {
                foreach (var (skill, score) in skills.Scores.OrderByDescending(s => s.Value))
                {
                    sb.AppendLine($"- {skill}: {DescribeLevel(score)} ({score}/100)");
                }
            }
            else
            {
                sb.AppendLine("- No proficiency data yet (assume an early beginner).");
            }

            if (request.ActiveChallenges.Count > 0)
            {
                sb.AppendLine("ACTIVE PRACTICE CHALLENGES:");
                foreach (var challenge in request.ActiveChallenges)
                {
                    sb.AppendLine(
                        $"- {challenge.Concept} ({challenge.MistakeType}): {challenge.ConsecutiveCorrectCount}/{PracticeChallenge.MasteryThreshold} correct in a row — keep weaving in natural prompts or questions that require this construction, without explicitly announcing a test.");
                }
            }

            if (skills.WeakestSkill is { } weakest)
            {
                sb.AppendLine($"Weakest skill to challenge next: {weakest}.");

                var appropriate = request.EligibleActivities
                    .Where(a => a.TrainedSkills.Contains(weakest))
                    .Select(a => a.Name)
                    .ToList();

                if (appropriate.Count > 0)
                    sb.AppendLine($"Appropriate activities for it: {string.Join(", ", appropriate)}.");
            }

            return Task.FromResult<MentalModelInsight?>(new MentalModelInsight(
                Name,
                "SKILL MODEL — what the user can do and should practise",
                sb.ToString().TrimEnd(),
                Order: 20));
        }

        private static string DescribeLevel(int score) => score switch
        {
            >= 80 => "Very comfortable",
            >= 60 => "Comfortable",
            >= 40 => "Developing",
            >= 20 => "Shaky",
            _ => "Just starting"
        };
    }
}
