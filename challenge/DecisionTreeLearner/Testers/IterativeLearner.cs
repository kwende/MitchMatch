using DecisionTreeLearner.Data;
using DecisionTreeLearner.DataTypes;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Testers
{
    public static class IterativeLearner
    {
        public static void DoIt(string misfitsFile, string mrnInputFile, string handPassedSets, string handRejectedSets)
        {
            if (!File.Exists(misfitsFile))
            {
                File.Create(misfitsFile).Close();
            }

            List<RecordPair> trainingData = new List<RecordPair>();
            Console.Write("Loading training data for this iteration...");
            trainingData.AddRange(DataLoader.GetPositivesFromMRNData(mrnInputFile));
            trainingData.AddRange(DataLoader.GetHandPassedSets(handPassedSets));
            trainingData.AddRange(DataLoader.GetRejectedRecordPairs(handRejectedSets));
            trainingData.AddRange(DataLoader.GetPairsFromMisfitsFile(misfitsFile));
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
                List<RecordPair> misFits = new List<RecordPair>();
                bool allDoneTraining = true;
                Parallel.ForEach(DataLoader.GetAllNegativeRecordPairsForMRNData(mrnInputFile), pair =>
                {
                    if (DecisionTreeBuilder.IsMatch(pair, new DecisionTree[] { trainedTree }, null) != pair.IsMatch)
                    {
                        lock (misFits)
                        {
                            misFits.Add(pair);
                        }
                        allDoneTraining = false;
                    }
                });
                Console.WriteLine($"..done. {misFits.Count} misfits found.");

                if (allDoneTraining)
                {
                    break;
                }
                else
                {
                    Console.Write("Writing misfits to disk...");
                    using (StreamWriter sw = File.AppendText(misfitsFile))
                    {
                        foreach (RecordPair misfit in misFits)
                        {
                            sw.WriteLine(misfit);
                        }
                    }
                    Console.WriteLine("...done");

                    Console.Write("Adding misfits to training data...");
                    int trainingDataOriginalSize = trainingData.Count;
                    trainingData.AddRange(misFits);
                    int trainingDataNowSize = trainingData.Count;
                    Console.WriteLine($"...done. Training data increased by {trainingDataNowSize - trainingDataOriginalSize} records.");

                    Console.WriteLine("Starting over!");
                }
            }
        }
    }
}
