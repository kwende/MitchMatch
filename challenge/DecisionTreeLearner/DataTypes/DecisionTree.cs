using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.DataTypes
{
    [Serializable]
    public class DecisionTree
    {
        public DecisionTreeNode Root { get; set; }
    }
}
