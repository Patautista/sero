using Application.Model;
using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class AnkiAppContext : DbContext
    {
        public DbSet<Card> Cards { get; set; }
        public DbSet<UserCardState> UserCardStates { get; set; }
        public DbSet<Sentence> Sentences { get; set; }
    }
}
