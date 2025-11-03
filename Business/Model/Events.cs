using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model
{
    public class CardAnsweredDto
    {
        public DateTime DateTime { get; set; }
        public int CardId { get; set; }
        public int EllapsedMs { get; set; }
        public int AnswerAttempt { get; set; }
    }

    public class CardSkippedDto
    {
        public DateTime DateTime { get; set; }
        public int CardId { get; set; }
        public int AnswerAttempt { get; set; }
    }
}
