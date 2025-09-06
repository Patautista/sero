using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Business;


namespace AppLogic.Web
{
    public interface ISettingsService
    {
        Task<StudyConfig?> LoadAsync();
        Task SaveStudyConfigAsync(StudyConfig config);
    }

    public class SettingsService : ISettingsService
    {
        private const string StudySectionKey = "StudyConfig";

        public async Task<StudyConfig?> LoadAsync()
        {
            if (!Preferences.ContainsKey(StudySectionKey))
                return null;

            var json = Preferences.Get(StudySectionKey, string.Empty);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<StudyConfig>(json);
        }

        public async Task SaveStudyConfigAsync(StudyConfig config)
        {
            var json = JsonSerializer.Serialize(config);
            Preferences.Set(StudySectionKey, json);
        }
    }

}
