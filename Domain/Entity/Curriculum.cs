using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Curriculum
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public IReadOnlyList<CurriculumSection> Sections { get; private set; }

        public Curriculum(Guid id, string name, IEnumerable<CurriculumSection> sections)
        {
            Id = id;
            Name = name;
            Sections = sections.ToList().AsReadOnly();
        }
    }
}
