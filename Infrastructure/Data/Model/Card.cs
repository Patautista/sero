using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class Card
    {
        public int Id { get; set; }
        [ForeignKey($"{nameof(Meaning)}s")]
        public int MeaningId { get; set; }
        public Meaning Meaning { get; set; }
        // Native sentence (e.g. Portuguese)
        public int NativeSentenceId { get; set; }
        public Sentence NativeSentence { get; set; } = null!;

        // Target sentence (e.g. English)
        public int TargetSentenceId { get; set; }
        public Sentence TargetSentence { get; set; } = null!;
    }
}
