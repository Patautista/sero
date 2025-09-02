using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class MeaningTable
    {
        public int Id { get; set; }
        public string DifficultyLevel { get; set; } = "beginner";
        public ICollection<TagTable> Tags { get; set; } = new HashSet<TagTable>();
        public ICollection<SentenceTable> Sentences { get; set; } = new HashSet<SentenceTable>();
        public ICollection<CardTable> Cards { get; set; } = new HashSet<CardTable>();
    }
}
