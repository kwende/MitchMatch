using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner
{
    class Program
    {
        static void Main(string[] args)
        {
            DecisionTree tree = new DecisionTree();
            tree.Train(); 
        }
    }
}
