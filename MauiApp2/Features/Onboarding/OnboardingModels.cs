using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Shared.Models;
using Infrastructure.Services;

namespace MauiApp2.Features.Onboarding
{
    public class OnboardingRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string NativeLanguage { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();

        /// <summary>
        /// The user's self-assessed level for each skill, collected during onboarding.
        /// Mapped to initial 0-100 skill scores when the profile is created.
        /// </summary>
        public Dictionary<SkillType, SelfAssessmentLevel> SkillAssessments { get; set; } = new();
    }

    public class OnboardingResponse
    {
        public bool Success { get; set; }
        public int UserProfileId { get; set; }
        public string CompanionName { get; set; } = string.Empty;
        public string WelcomeMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AvailableInterest
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public static class PredefinedInterests
    {
        public static List<AvailableInterest> GetInterests()
        {
            return new List<AvailableInterest>
            {
                new AvailableInterest { Name = "Travel", Icon = "✈️" },
                new AvailableInterest { Name = "Food", Icon = "🍕" },
                new AvailableInterest { Name = "Sports", Icon = "⚽" },
                new AvailableInterest { Name = "Music", Icon = "🎵" },
                new AvailableInterest { Name = "Technology", Icon = "💻" },
                new AvailableInterest { Name = "Art", Icon = "🎨" },
                new AvailableInterest { Name = "Nature", Icon = "🌿" },
                new AvailableInterest { Name = "Movies", Icon = "🎬" },
                new AvailableInterest { Name = "Books", Icon = "📚" },
                new AvailableInterest { Name = "Gaming", Icon = "🎮" },
                new AvailableInterest { Name = "Fashion", Icon = "👗" },
                new AvailableInterest { Name = "History", Icon = "🏛️" }
            };
        }
    }

    public class LanguageOption
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
    }

    public static class AvailableLanguages
    {
        private static readonly Dictionary<string, LanguageOption> _allLanguages = new()
        {
            [AvailableCodes.English]    = new LanguageOption { Code = AvailableCodes.English,    Name = "English",    Flag = "🇬🇧" },
            [AvailableCodes.French]     = new LanguageOption { Code = AvailableCodes.French,     Name = "French",     Flag = "🇫🇷" },
            [AvailableCodes.German]     = new LanguageOption { Code = AvailableCodes.German,     Name = "German",     Flag = "🇩🇪" },
            [AvailableCodes.Italian]    = new LanguageOption { Code = AvailableCodes.Italian,    Name = "Italian",    Flag = "🇮🇹" },
            [AvailableCodes.Norwegian]  = new LanguageOption { Code = AvailableCodes.Norwegian,  Name = "Norwegian",  Flag = "🇳🇴" },
            [AvailableCodes.Portuguese] = new LanguageOption { Code = AvailableCodes.Portuguese, Name = "Portuguese", Flag = "🇵🇹" },
            [AvailableCodes.Vietnamese] = new LanguageOption { Code = AvailableCodes.Vietnamese, Name = "Vietnamese", Flag = "🇻🇳" },
            [AvailableCodes.Chinese]    = new LanguageOption { Code = AvailableCodes.Chinese,    Name = "Chinese",    Flag = "🇨🇳" },
        };

        /// <summary>Languages available to learn, sourced from <see cref="AvailabilityService.TargetLanguages"/>.</summary>
        public static List<LanguageOption> GetLanguages()
            => AvailabilityService.TargetLanguages
                .Where(_allLanguages.ContainsKey)
                .Select(code => _allLanguages[code])
                .ToList();

        /// <summary>Native/app languages, sourced from <see cref="AvailabilityService.AppLanguages"/>.</summary>
        public static List<LanguageOption> GetNativeLanguages()
            => AvailabilityService.AppLanguages
                .Where(_allLanguages.ContainsKey)
                .Select(code => _allLanguages[code])
                .ToList();
    }
}
