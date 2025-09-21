using Domain.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model

{
    public record SentenceTable
    {
        public int Id { get; set; }
        public MeaningTable? Meaning { get; set; }

        [ForeignKey(nameof(MobileDbContext.Meanings))]
        public int MeaningId { get; set; }
        public string Text { get; set; }
        public string Language { get; set; } = "pt";

        public Sentence ToDomain()
        {
            return new Domain.Entity.Sentence
            {
                Id = Id,
                MeaningId = Meaning == null ? MeaningId : Meaning.Id,
                Language = Language,
                Text = Text
            };
        }
    }
}
