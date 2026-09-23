using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using MauiApp2.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MauiApp2.Features.ProactiveMessages
{
    public class TimingLearningService
    {
        private readonly IPetDataStore _store;
        private readonly ConfigurationService _config;
        private readonly ILogger<TimingLearningService> _logger;

        public TimingLearningService(
            IPetDataStore store,
            ConfigurationService config,
            ILogger<TimingLearningService> logger)
        {
            _store = store;
            _config = config;
            _logger = logger;
        }

        public async Task RecordUserActivityAsync(int userProfileId, string activityType)
        {
            try
            {
                var activity = new UserActivityTable
                {
                    UserProfileId = userProfileId,
                    ActivityType = activityType,
                    Timestamp = DateTime.UtcNow
                };

                _store.UserActivities.Add(activity);
                await _store.SaveChangesAsync();

                _logger.LogInformation($"Recorded activity: {activityType} for user {userProfileId}");

                // Update user profile last active time
                var userProfile = await _store.UserProfiles.FindByIdAsync(userProfileId);
                if (userProfile != null)
                {
                    userProfile.LastActiveAt = DateTime.UtcNow;
                    await _store.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording user activity");
            }
        }

        public async Task<bool> ShouldSendProactiveMessageAsync()
        {
            try
            {
                if (!_config.ProactiveMessagesEnabled)
                {
                    _logger.LogInformation("Proactive messages disabled in configuration");
                    return false;
                }

                // Get all user profiles
                var userProfiles = (await _store.UserProfiles.GetAllAsync()).ToList();

                if (!userProfiles.Any())
                {
                    _logger.LogInformation("No user profiles found");
                    return false;
                }

                // For each user, check if they should receive a message
                foreach (var userProfile in userProfiles)
                {
                    var shouldSend = await ShouldSendToUserAsync(userProfile.Id);
                    if (shouldSend)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if proactive message should be sent");
                return false;
            }
        }

        private async Task<bool> ShouldSendToUserAsync(int userProfileId)
        {
            try
            {
                // Check time since last message
                var timeSinceLast = await GetTimeSinceLastMessageAsync(userProfileId);
                if (timeSinceLast.TotalHours < _config.ProactiveMessageMinHoursBetween)
                {
                    _logger.LogInformation($"Too soon since last message for user {userProfileId}: {timeSinceLast.TotalHours} hours");
                    return false;
                }

                // Check messages sent today
                var messagesToday = await GetProactiveMessagesCountTodayAsync(userProfileId);
                if (messagesToday >= _config.ProactiveMessageMaxPerDay)
                {
                    _logger.LogInformation($"Max messages reached for user {userProfileId}: {messagesToday}/{_config.ProactiveMessageMaxPerDay}");
                    return false;
                }

                // Check if current time is optimal
                if (_config.RequireOptimalTiming)
                {
                    var optimalHours = await GetOptimalHoursAsync(userProfileId);
                    var currentHour = DateTime.Now.Hour;

                    if (!optimalHours.Contains(currentHour))
                    {
                        _logger.LogInformation($"Current hour {currentHour} not in optimal hours for user {userProfileId}: {string.Join(", ", optimalHours)}");
                        return false;
                    }
                }

                _logger.LogInformation($"User {userProfileId} should receive proactive message");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user {userProfileId} should receive message");
                return false;
            }
        }

        public async Task<List<int>> GetOptimalHoursAsync(int userProfileId)
        {
            try
            {
                // Get all user activities
                var activities = (await _store.UserActivities.WhereAsync(a => a.UserProfileId == userProfileId)).ToList();

                if (!activities.Any())
                {
                    // No data yet, return default hours (morning, afternoon, evening)
                    return new List<int> { 9, 14, 20 };
                }

                // Build histogram by hour
                var histogram = new Dictionary<int, int>();
                for (int hour = 0; hour < 24; hour++)
                {
                    histogram[hour] = 0;
                }

                foreach (var activity in activities)
                {
                    var localTime = activity.Timestamp.ToLocalTime();
                    histogram[localTime.Hour]++;
                }

                // Get top 3 most active hours
                var optimalHours = histogram
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(3)
                    .Select(kvp => kvp.Key)
                    .OrderBy(h => h)
                    .ToList();

                _logger.LogInformation($"Optimal hours for user {userProfileId}: {string.Join(", ", optimalHours)}");

                return optimalHours.Any() ? optimalHours : new List<int> { 9, 14, 20 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating optimal hours");
                return new List<int> { 9, 14, 20 }; // Default fallback
            }
        }

        public async Task<TimeSpan> GetTimeSinceLastMessageAsync(int userProfileId)
        {
            try
            {
                // Get last proactive message sent to user
                var allMessages = await _store.Messages.GetAllAsync();
                var lastMessage = allMessages
                    .Where(m => m.Conversation?.UserProfileId == userProfileId && 
                                m.SenderType == Domain.Shared.Models.SenderType.Companion &&
                                m.MessageType == Domain.Shared.Models.MessageType.ProactivePrompt)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefault();

                if (lastMessage == null)
                {
                    // No previous message, return max time
                    return TimeSpan.FromDays(365);
                }

                return DateTime.UtcNow - lastMessage.Timestamp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time since last message");
                return TimeSpan.FromDays(365);
            }
        }

        private async Task<int> GetProactiveMessagesCountTodayAsync(int userProfileId)
        {
            try
            {
                var todayStart = DateTime.UtcNow.Date;
                var todayEnd = todayStart.AddDays(1);

                var allMessages = await _store.Messages.GetAllAsync();
                var count = allMessages.Count(m => m.Conversation?.UserProfileId == userProfileId &&
                                m.SenderType == Domain.Shared.Models.SenderType.Companion &&
                                m.MessageType == Domain.Shared.Models.MessageType.ProactivePrompt &&
                                m.Timestamp >= todayStart &&
                                m.Timestamp < todayEnd);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proactive messages count");
                return 0;
            }
        }

        public async Task<Dictionary<int, int>> GetActivityHistogramAsync(int userProfileId, int daysPast = 30)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-daysPast);

                var allActivities = await _store.UserActivities.WhereAsync(a => a.UserProfileId == userProfileId);
                var activities = allActivities.Where(a => a.Timestamp >= cutoff).ToList();

                var histogram = new Dictionary<int, int>();
                for (int hour = 0; hour < 24; hour++)
                {
                    histogram[hour] = activities.Count(a => a.Timestamp.ToLocalTime().Hour == hour);
                }

                return histogram;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity histogram");
                return new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Gets the most recent learning activity (started) for the given user within the last specified minutes.
        /// Returns null if no activity was found within the time window.
        /// </summary>
        public async Task<UserActivityTable?> GetMostRecentActivityAsync(int userProfileId, int minutesBack = 15)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-minutesBack);
                var recentActivity = await _store.UserActivities.FirstOrDefaultAsync(a => a.UserProfileId == userProfileId && a.Timestamp >= cutoffTime);

                return recentActivity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting most recent activity");
                return null;
            }
        }

        public async Task<ActivityInsights> GetActivityInsightsAsync(int userProfileId)
        {
            try
            {
                var activities = (await _store.UserActivities.WhereAsync(a => a.UserProfileId == userProfileId)).ToList();

                if (!activities.Any())
                {
                    return new ActivityInsights
                    {
                        HasData = false,
                        Message = "Not enough activity data yet. Keep using the app!"
                    };
                }

                var histogram = await GetActivityHistogramAsync(userProfileId);
                var optimalHours = await GetOptimalHoursAsync(userProfileId);
                var mostActiveHour = histogram.OrderByDescending(kvp => kvp.Value).First().Key;

                var timeOfDay = mostActiveHour switch
                {
                    >= 5 and < 12 => "morning",
                    >= 12 and < 17 => "afternoon",
                    >= 17 and < 21 => "evening",
                    _ => "night"
                };

                return new ActivityInsights
                {
                    HasData = true,
                    MostActiveHour = mostActiveHour,
                    MostActiveTimeOfDay = timeOfDay,
                    OptimalHours = optimalHours,
                    TotalActivities = activities.Count,
                    Message = $"You're most active in the {timeOfDay} around {mostActiveHour}:00"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity insights");
                return new ActivityInsights { HasData = false };
            }
        }
    }

    public class ActivityInsights
    {
        public bool HasData { get; set; }
        public int MostActiveHour { get; set; }
        public string MostActiveTimeOfDay { get; set; } = string.Empty;
        public List<int> OptimalHours { get; set; } = new();
        public int TotalActivities { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
