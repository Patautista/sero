using System;

namespace MauiApp2.Features.Activities
{
    /// <summary>
    /// The lifecycle stages an activity moves through. Each activity behaves like a
    /// small state machine, and the backend always knows which stage it is in.
    ///
    ///   NotStarted -> Introduction -> InProgress -> Evaluating -> Completed
    /// </summary>
    public enum ActivityStage
    {
        NotStarted,
        Introduction,
        InProgress,
        Evaluating,
        Completed
    }

    /// <summary>
    /// Maps <see cref="ActivityStage"/> to and from the upper-snake-case tokens used
    /// in the Activity Agent prompt (NOT_STARTED, INTRODUCTION, ...). Keeping the
    /// mapping here means the prompt vocabulary and the C# enum can evolve together.
    /// </summary>
    public static class ActivityStages
    {
        public static string ToPromptToken(ActivityStage stage) => stage switch
        {
            ActivityStage.NotStarted => "NOT_STARTED",
            ActivityStage.Introduction => "INTRODUCTION",
            ActivityStage.InProgress => "IN_PROGRESS",
            ActivityStage.Evaluating => "EVALUATING",
            ActivityStage.Completed => "COMPLETED",
            _ => "NOT_STARTED"
        };

        public static ActivityStage FromPromptToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ActivityStage.InProgress;

            return token.Trim().ToUpperInvariant() switch
            {
                "NOT_STARTED" => ActivityStage.NotStarted,
                "INTRODUCTION" => ActivityStage.Introduction,
                "IN_PROGRESS" => ActivityStage.InProgress,
                "EVALUATING" => ActivityStage.Evaluating,
                "COMPLETED" => ActivityStage.Completed,
                _ => ActivityStage.InProgress
            };
        }

        /// <summary>The natural first stage once an activity has been started.</summary>
        public static ActivityStage FirstActiveStage => ActivityStage.Introduction;

        public static bool IsTerminal(ActivityStage stage) => stage == ActivityStage.Completed;
    }
}
