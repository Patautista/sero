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
        [ForeignKey(nameof(MobileDbContext.Meanings))]
        public int MeaningId { get; set; }
        [ForeignKey(nameof(MobileDbContext.Decks))]
        public int DeckId { get; set; }
        public DeckTable Deck { get; set; }
        public MeaningTable Meaning { get; set; }
        public DateTime CreatedIn { get; set; } = DateTime.Now;
        public SrsCardStateTable? UserCardState { get; set; }
        //[InverseProperty(nameof(EventTable.Card))]
        public ICollection<EventTable> Events { get; set; }
    }
}
