using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Business.Model
{
    public class SrsCard
    {
        public int CardId { get; set; }
        public int StateId { get; set; }
        public List<Sentence> SentencesInNativeLanguage { get; set; }
        public List<Sentence> SentencesInTargetLanguage { get; set; }
        [JsonIgnore]
        public Sentence NativeSample { get => SentencesInNativeLanguage.First(); }
        [JsonIgnore]
        public Sentence TargetSample { get => SentencesInTargetLanguage.First(); }

        public ICollection<Tag> Tags { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public int Repetitions { get; set; } = 0;
        public double EaseFactor { get; set; } = 2.5;
        public int Interval { get; set; } = 1;  // in days
        public DateTime NextReview { get; set; } = DateTime.Today;
        public DateTime LastReviewed { get; set; } = DateTime.MinValue;
    }
}
