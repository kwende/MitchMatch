using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Testers
{
    public static class TestSplitDirection
    {
        public static void Test()
        {
            Console.WriteLine("Line 1:");
            string line1 = Console.ReadLine();
            Console.WriteLine("Line 2:");
            string line2 = Console.ReadLine();

            RecordPair pair = new RecordPair();
            pair.Record1 = Record.FromString(line1);
            pair.Record2 = Record.FromString(line2); 

            DecisionTree[] forest = ForestLoader.FromDirectory("C:/users/brush/desktop/forest");

            bool isMatch = DecisionTreeBuilder.IsMatch(pair, forest, true);

            Console.WriteLine($"IsMatch: {isMatch}");
            Console.ReadLine(); 

            return; 
        }
    }
}
