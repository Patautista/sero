using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Business.MobileConfig
{
    public class StudyConfig
    {
        public string SelectedLanguageCode { get; set; }  // e.g. "it-pt"
        public DifficultyLevel DifficultyLevel { get; set; }
        private LanguagePair _selectedLanguage;

        [JsonIgnore] // computed, not serialized
        public LanguagePair SelectedLanguage
        {
            get
            {
                if (_selectedLanguage != null) return _selectedLanguage;
                if (string.IsNullOrEmpty(SelectedLanguageCode)) return null;
                var parts = SelectedLanguageCode.Split('-');
                return new LanguagePair
                {
                    Source = new CultureInfo(parts[0]),
                    Target = new CultureInfo(parts[1])
                };
            }
            set
            {
                _selectedLanguage = value;
                SelectedLanguageCode = $"{value.Source.TwoLetterISOLanguageName}-{value.Target.TwoLetterISOLanguageName}";
            }
        }
        public static StudyConfig Default => new StudyConfig
        {
            SelectedLanguageCode = "it-pt",
            DifficultyLevel = DifficultyLevel.Beginner
        };
    }

}
