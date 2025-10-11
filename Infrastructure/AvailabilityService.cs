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
        };
        

        public static List<string> AppLanguages = new()
        {
            "pt",
            "en",
        };
    }
    public static class AvailableCodes
    {
        public const string Italian = "it";
        public const string Norwegian = "no";
        public const string Portuguese = "pt";
    }
}
