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
        /// Propriedades específicas em JSON
        /// </summary>
        public string DomainEventJson { get; set; } = null!;
        /// <summary>
        /// Esquema ou versão do evento, ex.: "CardAnswered.v1"
        /// </summary>
        public required string Schema { get; set; }

        [ForeignKey(nameof(MobileDbContext.Users))]
        public int UserId { get; set; }

        [ForeignKey(nameof(MobileDbContext.ReviewSessions))]
        public Guid SessionId { get; set; }
        [ForeignKey(nameof(MobileDbContext.Cards))]
        public int CardId { get; set; }
    }
    public static class Events
    {
        public static class CardAnswered
        {
            public static class Schemas
            {
                public static string CardAnsweredV1 = nameof(CardAnsweredV1);
            }
        }
        public static class CardSkipped
        {
            public static class Schemas
            {
                public static string CardSkippedV1 = nameof(CardSkippedV1);
            }
        }
    }
}
