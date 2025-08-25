using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model

{
    public record Tag
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("name")]
        [Key]
        public string Name { get; set; }
        public ICollection<Card>? Cards { get; set; } = new HashSet<Card>();
    }
    public static class TagTypes
    {
        public static string Difficulty { get; set; }
        public static string LearningTopic { get; set; }
        public static string GeneralTopic { get; set; }
    }
}
