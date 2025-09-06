using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public record Card
    {
        public List<Sentence> SentencesInNativeLanguage { get; set; }
        public List<Sentence> SentencesInTargetLanguage { get; set; }
        public Sentence NativeSample { get => SentencesInNativeLanguage.First(); }
        public Sentence TargetSample { get => SentencesInTargetLanguage.First(); }

        public ICollection<Tag> Tags { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }

        public bool HasTag(string name)
        {
            return Tags.Any(t => t.Name == name.ToLower());
        }
        public bool SuitsDifficulty(DifficultyLevel difficultyLevel)
        {
            return DifficultyLevel <= difficultyLevel;
        }
    }
}
