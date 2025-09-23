using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class SrsCardStateTable
    {
        public int Id { get; set;  }
        [ForeignKey(nameof(MobileDbContext.Users))]
        public int UserId { get; set; }          
        [ForeignKey(nameof(MobileDbContext.Cards))]
        public int CardId { get; set; }       
        public double EaseFactor { get; set; } = 2.5;
        public int Interval { get; set; } = 1;  // in days
        public DateTime NextReview { get; set; } = DateTime.Today;

        public DateTime LastReviewed { get; set; } = DateTime.MinValue;

    }
}
