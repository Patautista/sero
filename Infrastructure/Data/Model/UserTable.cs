using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Model
{
    public class UserTable
    {
        public int Id { get; set; }
        public int Exp { get; set; }
        public static UserTable Default { get; set; } = new UserTable { Exp = 0 , Id = 1};
    }
}
