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
            Dictionary<int,Record> finalDataSet = DataLoader.LoadFinalDataSet(finalDataSetPath);
            Console.WriteLine("...done");

            List<RecordPair> trainingData = new List<RecordPair>();
            Console.Write("Getting positive training data...");
            List<RecordPair> allPositives = DataLoader.LoadAllPositivesFromAnswerKey(answerKeyPath, finalDataSet);
            trainingData.AddRange(allPositives);
            Console.WriteLine("...done");
            Console.Write("Getting negative training data (misfits)...");
            trainingData.AddRange(DataLoader.GetPairsFromMisfitsFile(misfitsFilePath, finalDataSet));
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
                int positiveCount = trainingData.Count(n => n.IsMatch);
                int negativeCount = trainingData.Count(n => !n.IsMatch);
                Console.WriteLine($"\tThere are {positiveCount} positive instances and {negativeCount} negative instances in the data...");
                DecisionTree trainedTree = builder.Train(trainingData, splittingQuestions, 1, 0, null);
                Console.WriteLine("...done.");

                Console.Write("Serializing to disk the latest tree...");
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fout = File.Create("tree.dat"))
                {
                    bf.Serialize(fout, trainedTree);
                }
                Console.WriteLine("...done");

                Console.WriteLine("Now let's test the tree and find the misfits...");
                List<RecordPair> misfits = new List<RecordPair>();
                bool allDoneTraining = true;
                long runCounter = 0;
                int left = Console.CursorLeft;
                int top = Console.CursorTop;

                Parallel.ForEach(DataLoader.LoadNegativesFromAnswerKey(allPositives), (pair, state) =>
                {
                    Interlocked.Increment(ref runCounter);

                    if (runCounter % 1000000 == 0)
                    {
                        lock (misfits)
                        {
                            Console.SetCursorPosition(left, top);
                            Console.WriteLine($"\tExamined {runCounter.ToString("N0")} entries thus far. {misfits.Count.ToString("N0")} misfits found.");
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
                    //using (StreamWriter outputForThisRound = File.AppendText($"c:/users/brush/desktop/misfits/{DateTime.Now.ToString()}.txt"))
                    {
                        using (StreamWriter sw = File.AppendText(misfitsFilePath))
                        {
                            foreach (RecordPair misfit in misfits)
                            {
                                string toWrite = $"{misfit.Record1.EnterpriseId},{misfit.Record2.EnterpriseId}";

                                sw.WriteLine(toWrite);
                                //outputForThisRound.WriteLine(toWrite);
                            }
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
