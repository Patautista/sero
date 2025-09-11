using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events
{
    public abstract record DomainEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    public record CardAnsweredEvent(
        Guid ReviewSessionId,
        int CardId,
        int ElapsedMs,
        bool Correct
    ) : DomainEvent;

    public record CardSkippedEvent(
        Guid ReviewSessionId,
        int CardId
    ) : DomainEvent;
}

