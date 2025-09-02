using Business.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class CardTable
    {
        public int Id { get; set; }
        [ForeignKey(nameof(AnkiDbContext.Meanings))]
        public int MeaningId { get; set; }
        public MeaningTable Meaning { get; set; }
        // Native sentence (e.g. Portuguese)
        public int NativeSentenceId { get; set; }
        public SentenceTable NativeSentence { get; set; } = null!;

        // Target sentence (e.g. English)
        public int TargetSentenceId { get; set; }
        public SentenceTable TargetSentence { get; set; } = null!;
        public UserCardStateTable? UserCardState { get; set; }
    }
}
