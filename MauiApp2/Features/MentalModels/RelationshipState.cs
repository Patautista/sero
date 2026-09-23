using System;
using Domain.Shared.Models;

namespace MauiApp2.Features.MentalModels
{
    /// <summary>How close the companion and user have become.</summary>
    public enum FriendshipStage
    {
        NewAcquaintance,
        Acquaintance,
        Friend,
        CloseFriend
    }

    /// <summary>
    /// The current relationship between the companion and the user. Distinct from
    /// conversation history: it captures closeness, trust and the resulting social
    /// permissions (whether to joke, ask personal questions, be informal). In this
    /// first version the metrics are derived deterministically from how much and how
    /// long the pair have interacted, so the relationship evolves consistently without
    /// extra storage; the derivation is isolated here so it can be replaced later.
    /// </summary>
    public sealed class RelationshipState
    {
        public FriendshipStage Stage { get; init; }

        /// <summary>0-100: how much the companion trusts/opens up to the user.</summary>
        public int Trust { get; init; }

        /// <summary>0-100: how well the pair know each other.</summary>
        public int Familiarity { get; init; }

        /// <summary>Preferred conversation style, e.g. "Gentle", "Warm", "Playful".</summary>
        public string ConversationStyle { get; init; } = "Warm";

        /// <summary>Current emotional tone, taken from the companion's mood.</summary>
        public string EmotionalTone { get; init; } = "Balanced";

        public bool CanJoke { get; init; }
        public bool CanAskPersonalQuestions { get; init; }

        /// <summary>How formal the companion should be, e.g. "Informal", "Relaxed".</summary>
        public string Formality { get; init; } = "Friendly";

        public string StageDisplayName => Stage switch
        {
            FriendshipStage.NewAcquaintance => "New Acquaintance",
            FriendshipStage.Acquaintance => "Acquaintance",
            FriendshipStage.Friend => "Friend",
            FriendshipStage.CloseFriend => "Close Friend",
            _ => Stage.ToString()
        };

        /// <summary>
        /// Derives the relationship from interaction volume and tenure. More exchanges
        /// and more days known raise familiarity and trust, which in turn unlock joking,
        /// personal questions and a more informal, playful style.
        /// </summary>
        public static RelationshipState Derive(int userMessageCount, double daysKnown, CompanionMood mood)
        {
            var messageCount = Math.Max(0, userMessageCount);
            var tenure = Math.Max(0, daysKnown);

            // Familiarity grows mostly with the number of exchanges.
            var familiarity = Clamp((int)Math.Round(Math.Min(100, messageCount * 2.0)));

            // Trust grows more slowly and blends exchanges with time known.
            var trust = Clamp((int)Math.Round(Math.Min(100, messageCount * 1.2 + tenure * 1.5)));

            var stage = (familiarity, trust) switch
            {
                ( >= 70, >= 60) => FriendshipStage.CloseFriend,
                ( >= 40, >= 30) => FriendshipStage.Friend,
                ( >= 15, _) => FriendshipStage.Acquaintance,
                _ => FriendshipStage.NewAcquaintance
            };

            var canJoke = stage >= FriendshipStage.Acquaintance;
            var canAskPersonal = stage >= FriendshipStage.Friend;

            var style = stage switch
            {
                FriendshipStage.CloseFriend => "Playful",
                FriendshipStage.Friend => "Warm and relaxed",
                FriendshipStage.Acquaintance => "Friendly",
                _ => "Gentle and welcoming"
            };

            var formality = stage switch
            {
                FriendshipStage.CloseFriend => "Very informal",
                FriendshipStage.Friend => "Informal",
                FriendshipStage.Acquaintance => "Relaxed",
                _ => "Polite and easygoing"
            };

            return new RelationshipState
            {
                Stage = stage,
                Trust = trust,
                Familiarity = familiarity,
                ConversationStyle = style,
                EmotionalTone = DescribeTone(mood),
                CanJoke = canJoke,
                CanAskPersonalQuestions = canAskPersonal,
                Formality = formality
            };
        }

        private static string DescribeTone(CompanionMood mood) => mood switch
        {
            CompanionMood.Tired => "Calm",
            CompanionMood.Curious => "Curious",
            CompanionMood.Nostalgic => "Reflective",
            CompanionMood.Excited => "Enthusiastic",
            _ => "Balanced"
        };

        private static int Clamp(int value) => value < 0 ? 0 : value > 100 ? 100 : value;
    }
}
