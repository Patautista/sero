using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Unknown
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
                "unknown" => DifficultyLevel.Unknown,
                _ => throw new ArgumentException($"Invalid difficulty: {value}")
            };
        }
    }
}
