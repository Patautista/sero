using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model.Server
{
    public class ApiAccess
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty; // Optional: who owns this key
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
