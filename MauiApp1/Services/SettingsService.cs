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
        Task SaveAsync(StudyConfig config);
    }

    public class SettingsService : ISettingsService
    {
        private const string ConfigKey = "StudyConfig";

        public async Task<StudyConfig?> LoadAsync()
        {
            if (!Preferences.ContainsKey(ConfigKey))
                return null;

            var json = Preferences.Get(ConfigKey, string.Empty);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<StudyConfig>(json);
        }

        public async Task SaveAsync(StudyConfig config)
        {
            var json = JsonSerializer.Serialize(config);
            Preferences.Set(ConfigKey, json);
        }
    }

}
