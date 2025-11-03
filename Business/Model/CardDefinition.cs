using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Business.Model
{
    public class CardDefinition
    {
        public string NativeLanguageCode {  get; set; }
        public string TargetLanguageCode { get; set; }
        public string NativeSentence { get; set; }
        public string TargetSentence { get; set; }

        public ICollection<Tag> Tags { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
    }
}
