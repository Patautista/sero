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
        public async Task UpdateUserCardState(SrsCard userCardState, int earnedExp, CancellationToken cancellationToken = default)
        {
            try
            {
                var oldState = await db.UserCardStates.FindAsync(userCardState.Id);

                var user = UserTable.Default;
                if (user != null)
                {
                    user.Exp += earnedExp;
                }

                if (oldState == null)
                {
                    oldState = new SrsCardStateTable()
                    {
                        CardId = userCardState.CardId,
                        //UserId = userCardState.UserId
                    };
                    db.Add(oldState);
                }

                oldState.Interval = userCardState.Interval;
                oldState.NextReview = userCardState.NextReview;
                oldState.LastReviewed = userCardState.LastReviewed;
                oldState.EaseFactor = userCardState.EaseFactor;

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
            return user.Exp;
        }
    }
}
