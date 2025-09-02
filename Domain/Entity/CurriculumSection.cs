using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class CurriculumSection
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public ICollection<Card> Cards { get; private set; }
        public int RequiredExp { get; private set; }

        public CurriculumSection(Guid id, string title, int requiredExp)
        {
            Id = id;
            Title = title;
            RequiredExp = requiredExp;
        }
    }
}
