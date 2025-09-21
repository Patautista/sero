using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class ReviewSessionTable
    {
        public Guid Id { get; set; }
        [ForeignKey(nameof(MobileDbContext.Users))]
        public int UserId { get; set; }
    }
}
