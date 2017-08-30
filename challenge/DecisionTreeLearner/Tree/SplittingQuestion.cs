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
            string ret = ""; 
            switch(MatchType)
            {
                case MatchTypeEnum.EditDistance:
                    ret = $"Is edit distance at or below {MaximumEditDistance} on {Field}?"; 
                    break;
                case MatchTypeEnum.EmptyMatch:
                    if (OneFieldValueIsEmpty)
                    {
                        ret = $"Is one and only one value of {Field} empty?";
                    }
                    else
                    {
                        ret = $"Are both fields of {Field} empty?";
                    }
                    break;
                case MatchTypeEnum.SoftMatch:
                    ret = $"Soft match on {Field}"; 
                    break;
                case MatchTypeEnum.IsFemale:
                    ret = "Are both entries female?";
                    break;
                case MatchTypeEnum.IsHomeless:
                    ret = "Is or was patient homeless.";
                    break;
                case MatchTypeEnum.LivesInMassResidence:
                    ret = "Is or was the patient living in a mass residence.";
                    break; 
            }
            return ret; 
        }
    }
}
