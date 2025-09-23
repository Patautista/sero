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
        public MeaningTable Meaning { get; set; }
        public SrsCardStateTable? UserCardState { get; set; }
        public ICollection<EventTable> Events { get; set; }
    }
}
