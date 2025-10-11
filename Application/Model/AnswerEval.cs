using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model
{
    public record AnswerEvaluation
    {
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
    public record AnswerFeedback
    {
        public AnswerQuality Quality { get; init; }
        public string ClosestMatch { get; init; }
        public string MainMessage { get; set; }
        public string? ExpectedAnswer { get; init; }
        public string? Hint { get; init; }
    }
}
