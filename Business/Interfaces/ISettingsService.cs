using Business.MobileConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces
{
    public interface ISettingsService
    {
        public ISettingProperty<ApiConfig?> ApiConfig { get; }
        public ISettingProperty<StudyConfig?> StudyConfig { get; }
        public ISettingProperty<RssConfig?> RssConfig { get; }
    }

    public interface ISettingProperty<T>
    {
        T Value { get; }
        void Update(T value);
    }
}
