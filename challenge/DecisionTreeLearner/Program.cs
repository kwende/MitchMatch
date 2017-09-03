using DecisionTreeLearner.Data;
using DecisionTreeLearner.NLP;
using DecisionTreeLearner.Testers;
using DecisionTreeLearner.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DecisionTreeLearner.Tree;
using DecisionTreeLearner.DataTypes.MySQL;

namespace DecisionTreeLearner
{
    class Program
    {
        static void Train(int numberOfTrees, string outputDirectory, double subsamplingPercentage,
            double minGain, int maximumEditDistance)
        {
            Console.WriteLine("Train options:");
            Console.WriteLine("\t1. Start full training.");
            Console.WriteLine("\t2. Start debug training from nohomo file.");
            Console.Write("Choice:");
            int option = int.Parse(Console.ReadLine());

            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<RecordPair> trainingData = null;
            if (option == 1)
            {
                trainingData = DataLoader.BuildTrainingData("mrns.csv", "more.csv", "rejected.txt");
            }
            else if (option == 2)
            {
                Console.Write("Nohomo file path:");
                string filePath = Console.ReadLine().Replace("\"", "");
                trainingData = DataLoader.LoadTrainingDataFromNoHomoFile(filePath);
            }
            //
            //List<RecordPair> trainingData = LoadTrainingData("D:/positives.csv", "D:/negatives.csv");

            int numberPerTree = trainingData.Count / numberOfTrees;

            for (int c = 0; c < numberOfTrees; c++)
            {
                List<RecordPair> trainingDataSubset = new List<RecordPair>();
                int startIndex = c * numberPerTree;
                int length = numberPerTree;
                if (c == numberOfTrees - 1)
                {
                    length += trainingData.Count % numberPerTree;
                }

                for (int d = startIndex; d < (startIndex + length); d++)
                {
                    trainingDataSubset.Add(trainingData[d]);
                }

                SplittingQuestion[] splittingQuestions = DecisionTreeBuilder.GenerateSplittingQuestions(maximumEditDistance);

                DecisionTreeBuilder treeBuilder = new DecisionTreeBuilder();
                DecisionTree tree = treeBuilder.Train(trainingDataSubset, splittingQuestions,
                    subsamplingPercentage, minGain, maximumEditDistance);

                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fout = File.Create(Path.Combine(outputDirectory, $"tree{c}.dat")))
                {
                    bf.Serialize(fout, tree);
                }
            }

            sw.Stop();

            Console.WriteLine($"Whole operation took {sw.ElapsedMilliseconds / 1000.0 / 60.0} minutes");
        }


        static void Main(string[] args)
        {
            //string response = Regex.Replace("SCOT   DREYER      5", " + ", " ", RegexOptions.None);

            //MySQLConnector conn = MySQLConnector.Connect();
            //List<SetWithPossibleOthers> setsWithOthers = conn.GetSetsWithPossibleOthers();

            //DecisionTree[] forest = DataLoader.LoadForestFromDirectory("c:/users/brush/desktop/forest/");
            //List<RecordPair> pairs = DataLoader.BuildTrainingData("mrns.csv", "more.csv", "rejected.txt");

            //int number = 1;
            //foreach (SetWithPossibleOthers setWithOther in setsWithOthers)
            //{
            //    Console.WriteLine($"Working on {number} of {setsWithOthers.Count}");
            //    number++;

            //    foreach (Record recordInSet in setWithOther.OriginalSet)
            //    {
            //        foreach (Record possibleMatch in setWithOther.PossibleMatches)
            //        {
            //            RecordPair pair = new RecordPair
            //            {
            //                Record1 = recordInSet,
            //                Record2 = possibleMatch
            //            };

            //            TreeLogger logger = new TreeLogger();
            //            if (DecisionTreeBuilder.IsMatch(pair, forest, logger))
            //            {
            //                using (StreamWriter sw = File.AppendText(
            //                    $"c:/users/brush/desktop/ends/{Guid.NewGuid().ToString().Replace("-", "")}.txt"))
            //                {
            //                    Parallel.ForEach(pairs, p =>
            //                    {
            //                        if (DecisionTreeBuilder.ReplayDecision(p, logger.SplittingQuestionsToTheBottom))
            //                        {
            //                            lock (sw)
            //                            {
            //                                sw.WriteLine(p);
            //                            }
            //                        }
            //                    });
            //                }
            //            }
            //        }
            //    }
            //}

            //return;

            Train(1, "C:/users/brush/desktop/forest", 1, 0, 3);
            //TestOnTrainingData();
            //TrainedDataTesters.TestOnLucasClosedSets("D:/repos/mitchmatch/closedsets.txt", "C:/users/brush/desktop/finaldataset.csv", "C:/users/brush/desktop/forest");

            //Testers.TestSplitDirection.Test();

            //Testers.ListAllMatches.List();

            //Testers.TrainedDataTesters.SearchForFalseNegatives(
            //    "D:/repos/mitchmatch/closedsets.txt",
            //    "C:/users/brush/desktop/finaldataset.csv",
            //    "C:/users/brush/desktop/forest",
            //    "D:/mitchMatchFalseNegativeSearchResults.txt",
            //    "D:/mitchMatchFalsenegatievSearchState.txt");
        }
    }
}
