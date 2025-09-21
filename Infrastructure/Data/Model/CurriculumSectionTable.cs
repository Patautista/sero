using Domain.Entity;
using Domain.Entity.Specification;
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
        [ForeignKey(nameof(MobileDbContext.Curricula))]
        public int CurriculumId { get; set; }
        public string Title { get; set; }
        public string TagsSpecificationJson { get; set; } = new TautologySpecification().ToJson();
        public string DifficultySpecificationJson { get; set; } = new TautologySpecification().ToJson();
        public int RequiredExp { get; set; }
    }
}
