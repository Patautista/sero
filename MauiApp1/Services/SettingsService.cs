using System.Text.Json;
using Business;

namespace AppLogic.Web
{
    public interface ISettingsService
    {
        ISettingProperty<string?> ApiKey { get; }
        ISettingProperty<StudyConfig?> StudyConfig { get; }
    }

    public interface ISettingProperty<T>
    {
        T Value { get; }
        void Update(T value);
    }

    public class SettingsService : ISettingsService
    {
        private const string StudySectionKey = "StudyConfig";
        private const string ApiKeyKey = "ApiKey";

        public ISettingProperty<string?> ApiKey { get; }
        public ISettingProperty<StudyConfig?> StudyConfig { get; }

        public SettingsService()
        {
            ApiKey = new SettingProperty<string?>(ApiKeyKey);
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