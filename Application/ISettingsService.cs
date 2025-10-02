using Business.MobileConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public interface ISettingsService
    {
        public ISettingProperty<ApiConfig?> ApiConfig { get; }
        public ISettingProperty<StudyConfig?> StudyConfig { get; }
    }

    public interface ISettingProperty<T>
    {
        T Value { get; }
        void Update(T value);
    }
}
