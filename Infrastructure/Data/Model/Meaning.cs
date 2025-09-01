using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class Meaning
    {
        public int Id { get; set; }
        public string DifficultyLevel { get; set; } = "beginner";
        public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
        public ICollection<Sentence> Sentences { get; set; } = new HashSet<Sentence>();
        public ICollection<Card> Cards { get; set; } = new HashSet<Card>();
    }
}
