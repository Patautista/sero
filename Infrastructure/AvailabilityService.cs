using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AvailabilityService
    {
        public static List<string> TargetLanguages = new()
        {
            AvailableCodes.Italian,
            AvailableCodes.Norwegian,
            AvailableCodes.English,
            AvailableCodes.Vietnamese,
            AvailableCodes.Chinese
        };
        
        public static List<string> AppLanguages = new()
        {
            AvailableCodes.Portuguese,
            AvailableCodes.English,
        };
    }
    public static class AvailableCodes
    {
        public const string Italian = "it";
        public const string Norwegian = "no";
        public const string Portuguese = "pt";
        public const string English = "en";
        public const string Vietnamese = "vi";
        public const string Chinese = "zh";
        public const string German = "de";
    }
}
