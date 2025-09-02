using Domain.Entity;
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
    public record TagTable
    {
        public string Type { get; set; }
        [Key]
        public string Name { get; set; }
        public ICollection<MeaningTable>? Meanings { get; set; } = new HashSet<MeaningTable>();

        public Tag ToDomain()
        {
            return new Domain.Entity.Tag
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
