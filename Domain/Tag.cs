using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain
{
    public sealed record Tag : IEquatable<Tag>
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        [Key]
        public string Name { get; set; }

        public ICollection<Card>? Cards { get; set; } = new HashSet<Card>();

        public bool Equals(Tag? other)
        {
            if (other is null) return false;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Name?.ToLowerInvariant().GetHashCode() ?? 0;
        }
    }
    public static class TagTypes
    {
        public static string Difficulty { get; set; } = nameof(Difficulty);
        public static string LearningTopic { get; set; } = nameof(LearningTopic);
        public static string GeneralTopic { get; set; } = nameof(GeneralTopic);
    }
    public static class TagConstants {
        public static Tag Beginner = new Tag { Name = nameof(Beginner), Type = TagTypes.Difficulty };
        public static Tag Intermediate = new Tag { Name = nameof(Intermediate), Type = TagTypes.Difficulty };
        public static Tag Advanced = new Tag { Name = nameof(Advanced), Type = TagTypes.Difficulty };

        public static Tag Culture = new Tag { Name = nameof(Culture), Type = TagTypes.GeneralTopic };
        public static Tag DailyLife = new Tag { Name = nameof(DailyLife), Type = TagTypes.GeneralTopic };
        public static Tag Biology = new Tag { Name = nameof(Biology), Type = TagTypes.GeneralTopic };
        public static Tag Health = new Tag { Name = nameof(Health), Type = TagTypes.GeneralTopic };

        public static Tag Numbers = new Tag { Name = nameof(Numbers), Type = TagTypes.LearningTopic };
        public static Tag Past = new Tag { Name = nameof(Past), Type = TagTypes.LearningTopic };
        public static Tag Future = new Tag { Name = nameof(Future), Type = TagTypes.LearningTopic };
        public static Tag Imperative = new Tag { Name = nameof(Imperative), Type = TagTypes.LearningTopic };
        public static Tag Interrogative = new Tag { Name = nameof(Interrogative), Type = TagTypes.LearningTopic };
        public static Tag Negation = new Tag { Name = nameof(Negation), Type = TagTypes.LearningTopic };
        public static Tag Movement = new Tag { Name = nameof(Movement), Type = TagTypes.LearningTopic };
        public static Tag ToBeSomething = new Tag { Name = nameof(ToBeSomething), Type = TagTypes.LearningTopic };
        public static Tag ToLikeSomething = new Tag { Name = nameof(ToLikeSomething), Type = TagTypes.LearningTopic };
    }
}
