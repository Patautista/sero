using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class CurriculumTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IReadOnlyList<CurriculumSection> Sections { get; set; }
    }
}
