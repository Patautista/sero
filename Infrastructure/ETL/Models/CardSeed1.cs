using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ETL.Models
{
    public record CardSeed1
    {
        public Sentence NativeSentence { get; set; }
        public Sentence TargetSentence { get; set; }

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
        public Card ToDomain()
        {
            return new Card
            {
                DifficultyLevel = DifficultyLevel,
                SentencesInNativeLanguage = new List<Sentence> { NativeSentence },
                SentencesInTargetLanguage = new List<Sentence> { TargetSentence },
                Tags = Tags
            };
        }
    }
}
