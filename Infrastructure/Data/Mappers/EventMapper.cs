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
            return ToTable(domainEvent, schema, Guid.Empty);
        }
        public static EventTable ToTable(object obj, string schema, Guid sessionId)
        {
            var name = obj.GetType().Name;
            var domainEvent = obj as DomainEvent;

            return new EventTable
            {
                Id = domainEvent.Id,
                SessionId = sessionId,
                Description = domainEvent.Description,
                OccurredAtUtc = domainEvent.OccurredAtUtc,
                Name = name,
                Schema = schema,
                DomainEventJson = JsonSerializer.Serialize(obj)
            };
        }

        public static DomainEvent ToDomain(EventTable table)
        {
            return table.Name switch
            {
                nameof(CardAnsweredEvent) =>
                    JsonSerializer.Deserialize<CardAnsweredEvent>(table.DomainEventJson)!,

                nameof(CardSkippedEvent) =>
                    JsonSerializer.Deserialize<CardSkippedEvent>(table.DomainEventJson)!,

                _ => throw new NotSupportedException($"Unknown event type: {table.Name}")
            };
        }
    }
}
