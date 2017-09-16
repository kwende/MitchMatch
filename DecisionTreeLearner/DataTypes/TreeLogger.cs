using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.DataTypes
{
    public class TreeLogger
    {
        public bool FinalResultIsMatch { get; set;}
        public List<Tuple<SplittingQuestion,bool>> SplittingQuestionsToTheBottom { get; set; }

        public TreeLogger()
        {
            SplittingQuestionsToTheBottom = new List<Tuple<SplittingQuestion, bool>>(); 
        }
    }
}
