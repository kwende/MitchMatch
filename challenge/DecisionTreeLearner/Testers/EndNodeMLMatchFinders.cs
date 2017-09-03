using DecisionTreeLearner.Data;
using DecisionTreeLearner.DataTypes;
using DecisionTreeLearner.DataTypes.MySQL;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Testers
{
    public static class EndNodeMLMatchFinders
    {
        public static void Find()
        {
            MySQLConnector conn = MySQLConnector.Connect();
            List<SetWithPossibleOthers> setsWithOthers = conn.GetSetsWithPossibleOthers();

            DecisionTree[] forest = DataLoader.LoadForestFromDirectory("c:/users/brush/desktop/forest/");
            List<RecordPair> pairs = DataLoader.BuildTrainingData("mrns.csv", "more.csv", "rejected.txt");

            int number = 1;
            foreach (SetWithPossibleOthers setWithOther in setsWithOthers)
            {
                Console.WriteLine($"Working on {number} of {setsWithOthers.Count}");
                number++;

                foreach (Record recordInSet in setWithOther.OriginalSet)
                {
                    foreach (Record possibleMatch in setWithOther.PossibleMatches)
                    {
                        RecordPair pair = new RecordPair
                        {
                            Record1 = recordInSet,
                            Record2 = possibleMatch
                        };

                        TreeLogger logger = new TreeLogger();
                        if (DecisionTreeBuilder.IsMatch(pair, forest, logger))
                        {
                            using (StreamWriter sw = File.AppendText(
                                $"c:/users/brush/desktop/ends/{Guid.NewGuid().ToString().Replace("-", "")}.txt"))
                            {
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
                    }
                }
            }

            return;
        }
    }
}
