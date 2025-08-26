using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationL
{
    public class AnswerEvaluator
    {
        
        public static AnswerEvaluation Evaluate(string answer, ICollection<string> possibleAnswers)
        {
            // Determine correctness (fuzzy match)
            var isCorrect = possibleAnswers.Any(ans => FuzzyMatch(answer, ans));
            var isPerfect = possibleAnswers.Any(ans => LevenshteinDistance(answer, ans) == 0);


            if (isPerfect) {
                return new AnswerEvaluation(AnswerQuality.Perfect);
            }
            else if (isCorrect) {
                var closestMatch = possibleAnswers.OrderBy(ans => LevenshteinDistance(answer, ans)).First();
                return new AnswerEvaluation(AnswerQuality.Ok, closestMatch);
            }
            else
            {
                var closestMatch = possibleAnswers.OrderBy(ans => LevenshteinDistance(answer, ans)).First();
                return new AnswerEvaluation(AnswerQuality.Wrong, closestMatch);
            }
        }
        // Simple fuzzy matching based on Levenshtein distance
        private static bool FuzzyMatch(string a, string b, double threshold = 0.7)
        {
            a = a.Trim().ToLower();
            b = b.Trim().ToLower();

            int distance = LevenshteinDistance(a, b);
            double score = 1.0 - (double)distance / Math.Max(a.Length, b.Length);

            return score >= threshold;
        }

        // Levenshtein distance algorithm
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }
            return d[n, m];
        }
    }
    public record AnswerEvaluation
    {
        public AnswerEvaluation(AnswerQuality quality)
        {
            Quality = quality;
        }
        public AnswerEvaluation(AnswerQuality quality, string closestMatch)
        {
            Quality = quality;
            ClosestMatch = closestMatch;
        }
        public readonly string ClosestMatch; 
        public readonly AnswerQuality Quality;
    }
    public enum AnswerQuality
    {
        Wrong = 2,
        Hard = 3,
        Ok = 4,
        Perfect = 5
    }
}
