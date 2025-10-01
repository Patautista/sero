using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
    public class AvailabilityService
    {
        public static List<string> TargetLanguages = new()
        {
            AvailableCodes.Italian,
        };
        

        public static List<string> AppLanguages = new()
        {
            "pt",
            "en",
        };
    }
    public static class AvailableCodes
    {
        public static string Italian = "it";
    }
}
