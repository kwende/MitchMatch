using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    [Serializable]
    public class DecisionTreeNode
    {
        public bool IsLeaf { get; set; }
        public bool IsMatch { get; set; }
        public DecisionTreeNode LeftBranch { get; set; }
        public DecisionTreeNode RightBranch { get; set; }
        public SplittingQuestion Question { get; set; }
    }
}
