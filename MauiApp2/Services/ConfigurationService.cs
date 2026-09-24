using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

namespace MauiApp2.Services
{
    /// <summary>
    /// Configuration service for loading appsettings.json in MAUI
    /// </summary>
    public class ConfigurationService
    {
        private readonly IConfiguration _configuration;

        public ConfigurationService()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("MauiApp2.appsettings.json");

            if (stream == null)
            {
                throw new FileNotFoundException(
                    "appsettings.json not found. Please copy appsettings.json.template to appsettings.json and configure it.");
            }

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            _configuration = config;
        }

        // AI Configuration
        public string GeminiApiKey => GetValue("AI:GeminiApiKey") ?? "";
        public string GeminiModel => GetValue("AI:GeminiModel") ?? "gemini-flash-latest";
        public string GeminiBaseUrl => GetValue("AI:GeminiBaseUrl") ?? "https://generativelanguage.googleapis.com/";
        public double Temperature => GetDoubleValue("AI:Temperature", 0.7);
        public int MaxTokens => GetIntValue("AI:MaxTokens", 1024);

        // Database Configuration
        public string DatabaseFileName => GetValue("Database:FileName") ?? "languagepet.db";
        public bool EnableDatabaseEncryption => GetBoolValue("Database:EnableEncryption", false);

        // Companion Configuration
        public string CompanionName => GetValue("Companion:DefaultName") ?? "Luna";
        public string CompanionPersonality => GetValue("Companion:DefaultPersonality") ?? 
            "Curious and encouraging, loves learning new things alongside you";
        public int MoodChangeIntervalHours => GetIntValue("Companion:MoodChangeIntervalHours", 4);

        // Proactive Messages
        public bool ProactiveMessagesEnabled => GetBoolValue("ProactiveMessages:Enabled", true);
        public int ProactiveMessageIntervalMinutes => GetIntValue("ProactiveMessages:IntervalMinutes", 30);
        public int ProactiveMessageMaxPerDay => GetIntValue("ProactiveMessages:MaxPerDay", 3);
        public int ProactiveMessageMinHoursBetween => GetIntValue("ProactiveMessages:MinHoursBetween", 6);
        public bool RequireOptimalTiming => GetBoolValue("ProactiveMessages:RequireOptimalTiming", true);

        // Language Settings
        public string DefaultTargetLanguage => GetValue("Languages:DefaultTargetLanguage") ?? "es";
        public string DefaultNativeLanguage => GetValue("Languages:DefaultNativeLanguage") ?? "en";

        // Feature Flags
        public bool EnableNotifications => GetBoolValue("Features:EnableNotifications", true);
        public bool EnableVoiceFeatures => GetBoolValue("Features:EnableVoiceFeatures", true);
        public bool EnableMemoryExtraction => GetBoolValue("Features:EnableMemoryExtraction", true);
        public bool EnableMistakeTracking => GetBoolValue("Features:EnableMistakeTracking", true);
        public bool EnableTimingLearning => GetBoolValue("Features:EnableTimingLearning", true);

        // Performance
        public int TranslationCacheSize => GetIntValue("Performance:TranslationCacheSize", 1000);
        public int MemoryCacheDurationHours => GetIntValue("Performance:MemoryCacheDurationHours", 24);
        public int ConversationHistoryLimit => GetIntValue("Performance:ConversationHistoryLimit", 50);
        public int MaxMessagesPerLoad => GetIntValue("Performance:MaxMessagesPerLoad", 50);

        // Logging
        public string LogLevel => GetValue("Logging:LogLevel:Default") ?? "Information";
        public bool LogAiRequests => GetBoolValue("Logging:LogAiRequests", false);
        public bool LogDatabaseQueries => GetBoolValue("Logging:LogDatabaseQueries", false);

        // Development
        public bool SkipOnboarding => GetBoolValue("Development:SkipOnboarding", false);
        public bool MockCompanionResponses => GetBoolValue("Development:MockCompanionResponses", false);
        public bool UseTestData => GetBoolValue("Development:UseTestData", false);
        public bool ShowDebugInfo => GetBoolValue("Development:ShowDebugInfo", false);

        // Memory
        public int MaxMemoriesPerRetrieval => GetIntValue("Memory:MaxMemoriesPerRetrieval", 5);
        public bool MemoryExtractionEnabled => GetBoolValue("Memory:MemoryExtractionEnabled", true);
        public int MinImportanceThreshold => GetIntValue("Memory:MinImportanceThreshold", 2);
        public int AutoExtractAfterMessages => GetIntValue("Memory:AutoExtractAfterMessages", 10);

        // Language Coaching
        public bool EnableMistakeDetection => GetBoolValue("LanguageCoaching:EnableMistakeDetection", true);
        public int MinMistakesToTrack => GetIntValue("LanguageCoaching:MinMistakesToTrack", 2);
        public bool CorrectionsInResponse => GetBoolValue("LanguageCoaching:CorrectionsInResponse", true);
        public bool DetailedFeedback => GetBoolValue("LanguageCoaching:DetailedFeedback", true);

        // UI
        public string Theme => GetValue("UI:Theme") ?? "System";
        public string AccentColor => GetValue("UI:AccentColor") ?? "#512BD4";
        public bool EnableAnimations => GetBoolValue("UI:EnableAnimations", true);
        public double MessageBubbleMaxWidth => GetDoubleValue("UI:MessageBubbleMaxWidth", 0.7);

        // Optional External APIs
        public string? GoogleTtsCredentialsJson => GetValue("Optional:GoogleTtsCredentialsJson");
        public string? TranslationApiKey => GetValue("Optional:TranslationApiKey");
        public string? TranslationApiUrl => GetValue("Optional:TranslationApiUrl");
        public string? AppInsightsInstrumentationKey => GetValue("Optional:AppInsightsInstrumentationKey");
        public bool EnableTelemetry => GetBoolValue("Optional:EnableTelemetry", false);

        // Validation
        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(GeminiApiKey) && 
                   GeminiApiKey != "your_gemini_api_key_here";
        }

        public string? ValidateConfiguration()
        {
            if (!IsConfigured())
            {
                return "Gemini API key is not configured. Please set your API key in appsettings.json";
            }

            return null; // Configuration is valid
        }

        // Helper methods
        private string? GetValue(string key)
        {
            return _configuration[key];
        }

        private int GetIntValue(string key, int defaultValue)
        {
            var value = _configuration[key];
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        private double GetDoubleValue(string key, double defaultValue)
        {
            var value = _configuration[key];
            return double.TryParse(value, out var result) ? result : defaultValue;
        }

        private bool GetBoolValue(string key, bool defaultValue)
        {
            var value = _configuration[key];
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
