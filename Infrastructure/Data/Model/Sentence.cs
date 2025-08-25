using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model

{
    public record Sentence
    {
        public int Id { get; set; }
        [ForeignKey($"{nameof(Meaning)}s")]
        public Meaning? Meaning { get; set; }
        public int MeaningId { get; set; }
        public string Text { get; set; }
        public string Language { get; set; } = "pt";
    }
}
