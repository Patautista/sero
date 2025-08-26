using ApplicationL.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationL
{
    public static class Srs
    {
        public static void Review(UserCardState state, int quality)
        {
            if (quality < 3)
            {
                state.Repetitions = 0;
                state.Interval = 1;
            }
            else
            {
                state.Repetitions++;
                state.EaseFactor = Math.Max(1.3, state.EaseFactor + 0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));

                switch (state.Repetitions)
                {
                    case 1: state.Interval = 1; break;
                    case 2: state.Interval = 6; break;
                    default: state.Interval = (int)Math.Round(state.Interval * state.EaseFactor); break;
                }
            }

            state.LastReviewed = DateTime.Today;
            state.NextReview = DateTime.Today.AddDays(state.Interval);
        }

        public static List<UserCardState> GetDueCards(List<UserCardState> states)
        {
            return states.Where(s => s.NextReview <= DateTime.Today)
                         .OrderBy(s => s.NextReview)
                         .ToList();
        }
    }
}
