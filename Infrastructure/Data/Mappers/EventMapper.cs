using Domain.Events;
using Infrastructure.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Data.Mappers
{
    public static class EventMapper
    {
        public static EventTable ToTable(DomainEvent domainEvent, string schema)
        {
            var name = domainEvent.GetType().Name; // ex: "CardAnswered"

            return new EventTable
            {
                Id = domainEvent.Id,
                OccurredAtUtc = domainEvent.OccurredAtUtc,
                Name = name,
                Schema = schema,
                PropertiesJson = JsonSerializer.Serialize(domainEvent)
            };
        }

        public static DomainEvent ToDomain(EventTable table)
        {
            return table.Name switch
            {
                nameof(CardAnswered) =>
                    JsonSerializer.Deserialize<CardAnswered>(table.PropertiesJson)!,

                nameof(CardSkipped) =>
                    JsonSerializer.Deserialize<CardSkipped>(table.PropertiesJson)!,

                _ => throw new NotSupportedException($"Unknown event type: {table.Name}")
            };
        }
    }
}
