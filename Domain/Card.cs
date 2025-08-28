using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public record Card
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
            var tag = Tags.First(t => t.Type == TagTypes.Difficulty);
            var cardDifficulty = DifficultyLevelExtensions.FromString(tag.Name);
            return cardDifficulty <= difficultyLevel;
        }
    }
}
