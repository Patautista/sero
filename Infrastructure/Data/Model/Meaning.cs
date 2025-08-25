using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class Meaning
    {
        public int Id { get; set; }
        public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
    }
}
