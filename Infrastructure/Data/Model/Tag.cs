using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Infrastructure.Data.Model

{
    public record Tag
    {
        public string Type { get; set; }
        [Key]
        public string Name { get; set; }
        public ICollection<Meaning>? Meanings { get; set; } = new HashSet<Meaning>();

        public Domain.Tag ToDomain()
        {
            return new Domain.Tag
            {
                Type = Type,
                Name = Name,
            };
        }
    }
    public static class TagTypes
    {
        public static string Difficulty { get; set; }
        public static string LearningTopic { get; set; }
        public static string GeneralTopic { get; set; }
    }
}
