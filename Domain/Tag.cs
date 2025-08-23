using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Tag
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
    public static class TagTypes
    {
        public static string Difficulty { get; set; }
        public static string LearningTopic { get; set; }
        public static string GeneralTopic { get; set; }
    }
}
