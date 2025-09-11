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
                DomainEventJson = JsonSerializer.Serialize(domainEvent)
            };
        }
        public static EventTable ToTable(DomainEvent domainEvent, string schema, Guid sessionId)
        {
            var name = domainEvent.GetType().Name; // ex: "CardAnswered"

            return new EventTable
            {
                Id = domainEvent.Id,
                SessionId = sessionId,
                OccurredAtUtc = domainEvent.OccurredAtUtc,
                Name = name,
                Schema = schema,
                DomainEventJson = JsonSerializer.Serialize(domainEvent)
            };
        }

        public static DomainEvent ToDomain(EventTable table)
        {
            return table.Name switch
            {
                nameof(Events.Names.CardAnswered) =>
                    JsonSerializer.Deserialize<CardAnsweredEvent>(table.DomainEventJson)!,

                nameof(Events.Names.CardSkipped) =>
                    JsonSerializer.Deserialize<CardSkippedEvent>(table.DomainEventJson)!,

                _ => throw new NotSupportedException($"Unknown event type: {table.Name}")
            };
        }
    }
}
