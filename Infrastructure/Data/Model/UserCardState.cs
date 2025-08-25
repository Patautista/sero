using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class UserCardState
    {
        public int Id { get; set;  }
        public int UserId { get; set; }          // link to the user
        public int CardId { get; set; }          // link to the card
        public int Repetitions { get; set; } = 0;
        public double EaseFactor { get; set; } = 2.5;
        public int Interval { get; set; } = 1;  // in days
        public DateTime NextReview { get; set; } = DateTime.Today;

        // Optional: keep track of last review date
        public DateTime LastReviewed { get; set; } = DateTime.MinValue;
    }
}
