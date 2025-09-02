using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public record Sentence
    {
        public int MeaningId { get; set; }
        public int Id { get; set; }
        public string Text { get; set; }
        public string Language { get; set; } = "pt";
    }
}
