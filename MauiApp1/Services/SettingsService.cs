using System.Text.Json;
using Business.Interfaces;
using Business.MobileConfig;

namespace AppLogic.Web
{
    public class SettingsService : ISettingsService
    {
        private const string StudySectionKey = "StudyConfig";
        private const string ApiConfigKey = "ApiConfig";

        public ISettingProperty<StudyConfig?> StudyConfig { get; }

        public ISettingProperty<ApiConfig?> ApiConfig { get; }

        public SettingsService()
        {
            ApiConfig = new SettingProperty<ApiConfig?>(ApiConfigKey);
            StudyConfig = new SettingProperty<StudyConfig?>(StudySectionKey);
        }

        private class SettingProperty<T> : ISettingProperty<T>
        {
            private readonly string _key;

            public SettingProperty(string key)
            {
                _key = key;
            }

            public T Value
            {
                get
                {
                    if (!Preferences.ContainsKey(_key))
                        return default!;
                    var json = Preferences.Get(_key, string.Empty);
                    return string.IsNullOrEmpty(json)
                        ? default!
                        : JsonSerializer.Deserialize<T>(json)!;
                }
            }

            public void Update(T value)
            {
                var json = JsonSerializer.Serialize(value);
                Preferences.Set(_key, json);
            }
        }
    }
}