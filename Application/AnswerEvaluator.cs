using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business
{
    public class AnswerEvaluator
    {

        public static AnswerEvaluation Evaluate(string userAnswer, ICollection<string> possibleAnswers)
        {
            // Normalize the user's answer
            var normalizedUserAnswer = NormalizeString(userAnswer);

            // Find the best possible match based on the normalized strings
            var bestMatch = possibleAnswers
                .Select(ans => new { Answer = ans, Normalized = NormalizeString(ans) })
                .OrderBy(match => LevenshteinDistance(normalizedUserAnswer, match.Normalized))
                .First();

            var distance = LevenshteinDistance(normalizedUserAnswer, bestMatch.Normalized);
            var isPerfect = distance == 0;
            var isCorrect = FuzzyMatch(normalizedUserAnswer, bestMatch.Normalized);

            if (isPerfect)
            {
                return new AnswerEvaluation(AnswerQuality.Perfect);
            }
            else if (isCorrect)
            {
                return new AnswerEvaluation(AnswerQuality.Ok, bestMatch.Answer);
            }
            else
            {
                return new AnswerEvaluation(AnswerQuality.Wrong, bestMatch.Answer);
            }
        }

        // --- Helper Functions ---

        public static string BuildFeedbackMessage(AnswerEvaluation evaluation, string userAnswer)
        {
            var sb = new StringBuilder();

            if (evaluation.Quality == AnswerQuality.Perfect)
                sb.Append("✅ Correto!");
            else if (evaluation.Quality == AnswerQuality.Ok)
                sb.Append("✅ Correto, mas com pequenos erros. ");
            else if (evaluation.Quality == AnswerQuality.Wrong)
                sb.Append("❌ Tente de novo.");

            if (evaluation.Quality < AnswerQuality.Perfect)
            {
                sb.Append($"\nResposta esperada: {evaluation.ClosestMatch}");

                // Optional: Provide detailed feedback
                var userMistake = FindDifferences(userAnswer, evaluation.ClosestMatch);
                sb.Append($"\n\nDica: {userMistake}");
            }

            return sb.ToString();
        }

        private static string NormalizeString(string input)
        {
            // Remove punctuation and extra whitespace, and convert to lowercase
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                {
                    sb.Append(char.ToLower(c));
                }
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static string FindDifferences(string userAnswer, string closestMatch)
        {
            // Let's implement a word-based diff for clarity.
            var userWords = NormalizeString(userAnswer).Split(' ');
            var correctWords = NormalizeString(closestMatch).Split(' ');

            var diff = new StringBuilder();
            int maxLen = Math.Max(userWords.Length, correctWords.Length);

            for (int i = 0; i < maxLen; i++)
            {
                var userWord = i < userWords.Length ? userWords[i] : "";
                var correctWord = i < correctWords.Length ? correctWords[i] : "";

                if (userWord != correctWord)
                {
                    if (userWord == "")
                        diff.Append($"(Faltou '{correctWord}')");
                    else if (correctWord == "")
                        diff.Append($"(Extra '{userWord}')");
                    else
                        diff.Append($"('{userWord}' deveria ser '{correctWord}')");

                    return diff.ToString(); // Return the first significant difference
                }
            }
            return "Nenhum erro significativo encontrado.";
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
