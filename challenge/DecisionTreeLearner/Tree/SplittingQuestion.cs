using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    [Serializable]
    public class SplittingQuestion
    {
        // descriptors
        public FieldEnum Field { get; set; }
        public MatchTypeEnum MatchType { get; set; }

        // parameters
        public int MaximumEditDistance { get; set; }
        public bool OneFieldValueIsEmpty { get; set; }
        public bool BothFieldValuesAreEmpty { get; set; }

        public override string ToString()
        {
            return $"Ask about {Field} question of type {MatchType} with maximum" +
                $" edit distance {MaximumEditDistance}, one field empty {OneFieldValueIsEmpty}, and both field values empty {BothFieldValuesAreEmpty}";
        }
    }
}
