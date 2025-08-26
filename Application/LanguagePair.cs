using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Business
{
    public class LanguagePair
    {
        public CultureInfo Source { get; set; }
        public CultureInfo Target { get; set; }

        public string DisplayName => $"{Source.NativeName} → {Target.NativeName}";
    }
}
