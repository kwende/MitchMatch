using DecisionTreeLearner.Data;
using DecisionTreeLearner.DataTypes;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
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

            DecisionTree[] forest = DataLoader.LoadForestFromDirectory(".");

            TreeLogger logger = new TreeLogger();
            bool isMatch = DecisionTreeBuilder.IsMatch(pair, forest, logger);

            Console.WriteLine("Search for those in training data who make it there?");
            string response = Console.ReadLine();

            //RecordPair pair2 = new RecordPair();
            //pair2.Record1 = Record.FromString("LAZAR,,KALLER,,M,,16/10/1965,,,,,,,-1,,,4839002,15479245,");
            //pair2.Record2 = Record.FromString("ADRIENNE,,KELLEY,,F,895535860,16/10/1965,9175738850,,1560 SILVER ST,2H,BRONX,NY,10461,,AKELLEY@AMGGT.COM,4799491,15637549,");

            //bool ret = DecisionTreeBuilder.ReplayDecision(pair2, logger.SplittingQuestionsToTheBottom); 

            if (response.ToLower() == "y")
            {
                using (StreamWriter sw = File.AppendText("c:/users/brush/desktop/gothere.txt"))
                {
                    List<RecordPair> pairs = new List<RecordPair>();
                    Console.Write("Loading training data for this iteration...");
                    pairs.AddRange(DataLoader.GetPositivesFromMRNData("mrns.csv"));
                    pairs.AddRange(DataLoader.GetHandPassedSets("more.csv"));
                    pairs.AddRange(DataLoader.GetRejectedRecordPairs("rejected.txt"));
                    pairs.AddRange(DataLoader.GetPairsFromMisfitsFile("misfits.txt"));
                    Console.WriteLine("...done");

                    Parallel.ForEach(pairs, p =>
                    {
                        if (DecisionTreeBuilder.ReplayDecision(p, logger.SplittingQuestionsToTheBottom))
                        {
                            lock (sw)
                            {
                                sw.WriteLine(p);
                            }
                        }
                    });
                }
            }

            return;
        }
    }
}
