using Business.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces
{
    public interface IAIEvaluator
    {
        Task<AnswerEvaluation> GetAnswerEvaluation(string challenge, string userAnswer, ICollection<string> possibleAnswers);
        Task<bool> IsAvailable();
    }
}
