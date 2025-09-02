using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class UserCardStateTable
    {
        public int Id { get; set;  }
        [ForeignKey($"{nameof(UserTable)}s")]
        public int UserId { get; set; }          // link to the user
        [ForeignKey($"{nameof(CardTable)}s")]
        public int CardId { get; set; }          // link to the card
        public int Repetitions { get; set; } = 0;
        public double EaseFactor { get; set; } = 2.5;
        public int Interval { get; set; } = 1;  // in days
        public DateTime NextReview { get; set; } = DateTime.Today;

        // Optional: keep track of last review date
        public DateTime LastReviewed { get; set; } = DateTime.MinValue;

        public Business.Model.UserCardState ToDomain()
        {
            return new Business.Model.UserCardState
            {
                UserId = UserId,
                CardId = CardId,
                Repetitions = Repetitions,
                EaseFactor = EaseFactor,
                Interval = Interval,
                NextReview = NextReview,
                LastReviewed = LastReviewed,
            };
        }
    }
}
