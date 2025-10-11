using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class DeckTable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation property
        public ICollection<CardTable> Cards { get; set; } = new List<CardTable>();
    }
}
