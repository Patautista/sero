using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class CurriculumSection
    {
        public Guid Id { get; set; }
        public string Title { get;  set; }
        public ICollection<Card> Cards { get; set; }
        public int RequiredExp { get; set; }

    }
}
