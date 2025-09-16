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
        public record CardAudiov1
        {
            public CardAudiov1(Card card, string audioData)
            {
                NativeSentence = card.NativeSample;
                TargetSentence = card.TargetSample;
                Tags = card.Tags;
                DifficultyLevel = card.DifficultyLevel;
                AudioData = audioData;
            }
            public Sentence NativeSentence { get; set; }
            public Sentence TargetSentence { get; set; }

            public string AudioData { get; set; }
            public ICollection<Tag> Tags { get; set; }
            public DifficultyLevel DifficultyLevel { get; set; }

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
}
