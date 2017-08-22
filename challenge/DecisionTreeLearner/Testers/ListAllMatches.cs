using DecisionTreeLearner.NLP;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Testers
{
    public static class ListAllMatches
    {
        public static void List()
        {
            Console.WriteLine("Line to match:");
            string line1 = Console.ReadLine();
            Record toMatch = Record.FromString(line1);

            string[] finalDataSetList = File.ReadAllLines("c:/users/brush/desktop/finaldataset.csv");

            Console.WriteLine("Searching for matches...");
            for (int c = 1; c < finalDataSetList.Length; c++)
            {
                string finalDataSetRow = finalDataSetList[c];
                string[] bits = finalDataSetRow.Split(',');

                if (bits[0] != "")
                {
                    int enterpriseId = int.Parse(bits[0]);

                    if (enterpriseId > 15374761)
                    {
                        Record comparisonRecord = Record.FromFinalDatasetString(bits);

                        RecordPair pair = new RecordPair
                        {
                            Record1 = toMatch,
                            Record2 = comparisonRecord,
                        };

                        DecisionTree[] forest = ForestLoader.FromDirectory("C:/users/brush/desktop/forest");

                        bool isMatch = DecisionTreeBuilder.IsMatch(pair, forest, false);

                        if (isMatch)
                        {
                            Console.WriteLine(comparisonRecord);
                            Console.WriteLine();
                        }
                    }
                }

            }
        }

        public static Dictionary<Record, List<Record>> GetMatches(List<Record> records)
        {
            Dictionary<Record, List<Record>> ret = new Dictionary<Record, List<Record>>();
            string[] finalDataSetList = File.ReadAllLines("c:/users/brush/desktop/finaldataset.csv");
            DecisionTree[] forest = ForestLoader.FromDirectory("C:/users/brush/desktop/forest");

            foreach (Record record in records)
            {
                ret.Add(record, new List<Record>());
            }

            Console.WriteLine("Searching for matches...");
            //for (int c = 1; c < finalDataSetList.Length; c++)
            Parallel.For(1, finalDataSetList.Length, c =>
            {
                if (c % 10000 == 0)
                {
                    Console.WriteLine($"{(c / (finalDataSetList.Length * 1.0) * 100)}%");
                }

                string finalDataSetRow = finalDataSetList[c];
                string[] bits = finalDataSetRow.Split(',');

                if (bits[0] != "")
                {
                    int enterpriseId = int.Parse(bits[0]);

                    if (enterpriseId > 15374761)
                    {
                        Record comparisonRecord = DataCleaner.CleanRecord(Record.FromFinalDatasetString(bits));

                        foreach (Record toMatch in records)
                        {
                            if (!toMatch.Equals(comparisonRecord))
                            {
                                RecordPair pair = new RecordPair
                                {
                                    Record1 = toMatch,
                                    Record2 = comparisonRecord,
                                };

                                bool isMatch = DecisionTreeBuilder.IsMatch(pair, forest, false);

                                if (isMatch)
                                {
                                    lock (ret)
                                    {
                                        ret[toMatch].Add(comparisonRecord);
                                    }
                                }
                            }
                        }
                    }
                }
            });

            return ret;
        }
    }
}
