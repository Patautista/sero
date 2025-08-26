using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public static class DifficultyLevelExtensions
    {
        public static DifficultyLevel FromString(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "beginner" => DifficultyLevel.Beginner,
                "intermediate" => DifficultyLevel.Intermediate,
                "advanced" => DifficultyLevel.Advanced,
                _ => throw new ArgumentException($"Invalid difficulty: {value}")
            };
        }
    }
}
