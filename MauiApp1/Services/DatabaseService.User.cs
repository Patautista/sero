using Business.Model;
using Infrastructure.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
    public partial class DatabaseService
    {
        public async Task UpdateUserCardState(SrsCard srsCard, int earnedExp, CancellationToken cancellationToken = default)
        {
            try
            {
                var oldState = await db.UserCardStates.FindAsync(srsCard.StateId);

                var user = UserTable.Default;
                if (user != null)
                {
                    user.Exp += earnedExp;
                }

                if (oldState == null)
                {
                    oldState = new SrsCardStateTable()
                    {
                        CardId = srsCard.CardId,
                        //UserId = userCardState.UserId
                    };
                    db.Add(oldState);
                }

                oldState.Interval = srsCard.Interval;
                oldState.NextReview = srsCard.NextReview;
                oldState.LastReviewed = srsCard.LastReviewed;
                oldState.EaseFactor = srsCard.EaseFactor;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<int> GetUserExpAsync()
        {
            var user = await db.Users.FindAsync(UserTable.Default.Id);
            if(user == null)
            {
                db.Add(UserTable.Default);
                await db.SaveChangesAsync();
                return 0;
            }
            return user.Exp;
        }
    }
}
