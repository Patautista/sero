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
        private const int BaseExp = 100; // tweakable: base scaling factor
        private const double GrowthFactor = 1.5; // tweakable: how fast difficulty scales
        public static int CalculateEarnedExp(Card card, int repetitions)
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
        // Returns the total exp required to reach a specific level
        // --- Total EXP required to *reach* a given level (cumulative) ---
        public static int ExpForLevel(int level)
        {
            if (level <= 1) return 0; // Level 1 starts at 0 XP

            int exp = 0;
            for (int i = 2; i <= level; i++)
            {
                exp += (int)(BaseExp * Math.Pow(i - 1, GrowthFactor));
            }
            return exp;
        }

        // --- Given total EXP, calculate current level ---
        public static int GetLevel(int totalExp)
        {
            int level = 1;
            while (totalExp >= ExpForLevel(level + 1))
            {
                level++;
            }
            return level;
        }

        // --- Progress toward next level (for progress bar) ---
        public static (int currentLevel, int expIntoLevel, int expForNextLevel) GetLevelProgress(int totalExp)
        {
            int level = GetLevel(totalExp);

            int expForCurrent = ExpForLevel(level);
            int expForNext = ExpForLevel(level + 1);

            int expIntoLevel = totalExp - expForCurrent;
            int expNeeded = expForNext - expForCurrent;

            return (level, expIntoLevel, expNeeded);
        }
    }
}
