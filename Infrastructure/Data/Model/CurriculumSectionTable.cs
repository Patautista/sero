using Domain.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class CurriculumSectionTable
    {
        public int Id { get;  set; }
        [ForeignKey(nameof(AnkiDbContext.Curricula))]
        public int CurriculumId { get; set; }
        public string Title { get; set; }
        public string PropertySpecificationJson { get; set; }
        public int RequiredExp { get; set; }
    }
}
