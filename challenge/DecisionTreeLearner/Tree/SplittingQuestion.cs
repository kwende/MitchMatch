using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    public class SplittingQuestion
    {
        // descriptors
        public FieldEnum Field { get; set; }
        public MatchTypeEnum MatchType { get; set; }

        // parameters
        public int MaximumEditDistance { get; set; }
        public bool OneFieldValueIsEmpty { get; set; }
        public bool BothFieldValuesAreEmpty { get; set; }
    }
}
