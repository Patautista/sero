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
        public string Description { get; init; } = "Just another event.";
    }

    public record CardAnsweredEvent(
        Guid ReviewSessionId,
        string ChallengeType,
        int CardId,
        int EllapsedMs,
        bool Correct
    ) : DomainEvent;

    public record CardSkippedEvent(
        Guid ReviewSessionId,
        string ChallengeType,
        int CardId
    ) : DomainEvent;
}

