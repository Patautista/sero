using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class EventTable
    {
        public Guid Id { get; set; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// Propriedades específicas em JSON (ex.: serialização de CardAnsweredEventProperties)
        /// </summary>
        public string PropertiesJson { get; set; } = null!;
        /// <summary>
        /// Esquema ou versão do evento, ex.: "CardAnswered.v1"
        /// </summary>
        public required string Schema { get; set; }

        [ForeignKey(nameof(AnkiDbContext.Users))]
        public int UserId { get; set; }

        [ForeignKey(nameof(AnkiDbContext.ReviewSessions))]
        public Guid SessionId { get; set; }
        [ForeignKey(nameof(AnkiDbContext.Cards))]
        public int CardId { get; set; }
    }
    public static class Events
    {
        public static class Names
        {
            public static string CardSkipped = nameof(CardSkipped);
            public static string CardAnswered = nameof(CardAnswered);
        }
        public static class Schemas
        {
            public static string CardSkippedV1 = nameof(CardSkippedV1);
            public static string CardAnsweredV1 = nameof(CardAnsweredV1);
        }
    }
    public class CardAnsweredEventProperties : ReviewSessionEventProperties
    {
        public int EllapsedMs { get; set; }
        public int AnswerAttempt { get; set; }
    }
    public class CardSkippedEventProperties : ReviewSessionEventProperties
    {
        public int AnswerAttempt { get; set; }
    }
    public abstract class ReviewSessionEventProperties
    {
        public Guid ReviewSessionId { get; set; }
        public int CardId { get; set; }
    }
}
