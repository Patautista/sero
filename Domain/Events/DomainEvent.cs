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

    public record CardAnswered(
        Guid ReviewSessionId,
        int CardId,
        int ElapsedMs,
        int AnswerAttempt,
        bool Correct
    ) : DomainEvent;

    public record CardSkipped(
        Guid ReviewSessionId,
        int CardId,
        int AnswerAttempt
    ) : DomainEvent;
}

