using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    [Serializable]
    public enum MatchTypeEnum
    {
        EditDistance = 0,
        EmptyMatch = 1,
        SoftMatch = 2,
        IsFemale = 3,
        IsHomeless = 4,
        LivesInMassResidence = 5,
        MRNDistance = 6,
    }
}
