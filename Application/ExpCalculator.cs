using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public static class ExpCalculator
    {
        public static int CalculateExp(Card card, int repetitions)
        {
            // --- 1. Repetition factor (slower decay) ---
            // Use an exponential decay with gentle slope
            // 1st repetition → ~1.0, 5th → ~0.67, 10th → ~0.45
            double k = 0.1; // decay rate, tweakable
            double repetitionFactor = Math.Exp(-k * (repetitions - 1));

            // --- 2. Difficulty factor ---
            double difficultyFactor = card.DifficultyLevel switch
            {
                DifficultyLevel.Beginner => 1.0,
                DifficultyLevel.Intermediate => 1.3,
                DifficultyLevel.Advanced => 1.6,
                _ => 1.0
            };

            // --- 3. Length factor ---
            double lengthFactor = Math.Log(card.TargetSentence.Text.Length + 1, 2); // log scaling

            // --- Base EXP ---
            double baseExp = 10;

            double totalExp = baseExp * repetitionFactor * difficultyFactor * lengthFactor;

            return (int)Math.Round(totalExp);
        }
    }
}
