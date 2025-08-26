using Business.Model;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ViewModel
{
    public class CardWithState
    {
        public Card Card { get; set; }
        public UserCardState State { get; set; }
    }
}
