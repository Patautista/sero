using Domain.Events;
using Infrastructure.Data;
using Infrastructure.Data.Mappers;
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
        public async Task SaveCardSkippedAsync(CardSkippedEvent domainEvent)
        {
            var evt = EventMapper.ToTable(domainEvent, Events.CardSkipped.Schemas.CardSkippedV1);
            evt.CardId = domainEvent.CardId;
            db.Events.Add(evt);
            await db.SaveChangesAsync();
        }

        public async Task SaveCardAnsweredAsync(CardAnsweredEvent domainEvent)
        {
            try
            {
                var evt = EventMapper.ToTable(domainEvent, Events.CardAnswered.Schemas.CardAnsweredV1);
                evt.CardId = domainEvent.CardId;
                db.Events.Add(evt);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
