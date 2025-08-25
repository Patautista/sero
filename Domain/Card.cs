using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Card
    {
        public int Id { get; set; }
        public int MeaningId { get; set; }
        public Sentence NativeSentence { get; set; }
        public Sentence TargetSentence { get; set; }
        public ICollection<Tag> Tags { get; set; }
    }
}
