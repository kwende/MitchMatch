using DecisionTreeLearner.Data;
using DecisionTreeLearner.DataTypes;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Testers
{
    public static class IterativeLearner
    {
        public static void DoIt(string misfitsFilePath, string answerKeyPath, string finalDataSetPath)
        {
            if (!File.Exists(misfitsFilePath))
            {
                File.Create(misfitsFilePath).Close();
            }

            Console.Write("Loading final data set...");
            List<Record> finalDataSet = DataLoader.LoadFinalDataSet(finalDataSetPath);
            Console.WriteLine("...done");

            Console.Write("Load all positives from answer key...");
            List<RecordPair> allPositives = DataLoader.LoadAllPositivesFromAnswerKey(answerKeyPath, finalDataSet);
            Console.WriteLine("...done");

            List<RecordPair> trainingData = new List<RecordPair>();
            Console.Write("Loading training data for this iteration...");

            trainingData.AddRange(DataLoader.LoadAllPositivesFromAnswerKey(answerKeyPath, finalDataSet));
            trainingData.AddRange(DataLoader.GetPairsFromMisfitsFile(misfitsFilePath));
            Console.WriteLine("...done");

            Console.Write("Generating splitting questions for this iteration...");
            int maximumEditDistance = 3;
            SplittingQuestion[] splittingQuestions = DecisionTreeBuilder.GenerateSplittingQuestions(maximumEditDistance);
            Console.WriteLine("...done");

            // 1. get a random assort of training data. 
            for (; ; )
            {
                DecisionTreeBuilder builder = new DecisionTreeBuilder();

                Console.Write("Training...");
                DecisionTree trainedTree = builder.Train(trainingData, splittingQuestions, 1, 0, null);
                Console.WriteLine("...done.");

                Console.Write("Serializing to disk the latest tree...");
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fout = File.Create("tree.dat"))
                {
                    bf.Serialize(fout, trainedTree);
                }
                Console.WriteLine("...done");

                Console.Write("Now let's test the tree and find the misfits...");
                List<RecordPair> misfits = new List<RecordPair>();
                bool allDoneTraining = true;
                int runCounter = 0;
                int left = Console.CursorLeft;
                int top = Console.CursorTop;

                Parallel.ForEach(DataLoader.LoadNegativesFromAnswerKey(allPositives, finalDataSet), (pair, state) =>
                {
                    Interlocked.Increment(ref runCounter);

                    if (runCounter % 1000000 == 0)
                    {
                        lock (misfits)
                        {
                            Console.SetCursorPosition(left, top);
                            Console.WriteLine($"\tExamined {runCounter} entries thus far. {misfits.Count} misfits found.");
                        }
                    }

                    if (misfits.Count() < 100000)
                    {
                        if (DecisionTreeBuilder.IsMatch(pair, new DecisionTree[] { trainedTree }, null) != pair.IsMatch)
                        {
                            lock (misfits)
                            {
                                misfits.Add(pair);
                            }
                            allDoneTraining = false;
                        }
                    }
                    else
                    {
                        state.Break();
                    }
                });
                Console.WriteLine($"..done. {misfits.Count} misfits found.");

                if (allDoneTraining)
                {
                    break;
                }
                else
                {
                    Console.Write("Writing misfits to disk...");
                    using (StreamWriter sw = File.AppendText(misfitsFilePath))
                    {
                        foreach (RecordPair misfit in misfits)
                        {
                            sw.WriteLine(misfit);
                        }
                    }
                    Console.WriteLine("...done");

                    Console.Write("Adding misfits to training data...");
                    int trainingDataOriginalSize = trainingData.Count;
                    trainingData.AddRange(misfits);
                    int trainingDataNowSize = trainingData.Count;
                    Console.WriteLine($"...done. Training data increased by {trainingDataNowSize - trainingDataOriginalSize} records.");

                    Console.WriteLine("Starting over!");
                }
            }
        }
    }
}
