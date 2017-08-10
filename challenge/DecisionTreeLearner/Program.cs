using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionTreeLearner
{
    class Program
    {
        static void EditDistanceTests()
        {
            string str1 = "Hello World";
            string str2 = "ello World";

            int editDistance = NLP.EditDistance.Compute(str1, str2);
            Debug.Assert(editDistance == 1);

            str1 = "hello world";
            str2 = "Hello World";

            editDistance = NLP.EditDistance.Compute(str1, str2);

            Debug.Assert(editDistance == 2);

            str1 = "abcdefg";
            str2 = "hijklmn";

            editDistance = NLP.EditDistance.Compute(str1, str2);

            Debug.Assert(editDistance == 7);

            str1 = "";
            str2 = "";

            editDistance = NLP.EditDistance.Compute(str1, str2);

            Debug.Assert(editDistance == 0);

            str1 = "";
            str2 = "googliebah";

            editDistance = NLP.EditDistance.Compute(str1, str2);

            Debug.Assert(editDistance == 10);
        }

        static List<RecordPair> BuildTrainingData(string inputFilePath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();

            string[] lines = File.ReadAllLines(inputFilePath);

            List<Record> allRecords = new List<Record>();
            for (int c = 0; c < lines.Length; c += 3)
            {
                allRecords.Add(Record.FromString(lines[c]));
                allRecords.Add(Record.FromString(lines[c + 1]));
            }

            for (int c = 0; c < allRecords.Count; c += 2)
            {
                if (c % 500 == 0)
                {
                    Console.Clear();
                    Console.WriteLine(((c / (allRecords.Count * 1.0)) * 100) + "%");
                }

                trainingData.Add(new RecordPair
                {
                    IsMatch = true,
                    Record1 = allRecords[c],
                    Record2 = allRecords[c + 1],
                });

                Random rand = new Random();
                for (int d = 0; d < allRecords.Count; d += 2)
                {
                    if (c != d)
                    {
                        if (rand.Next() % 2 == 0)
                        {
                            trainingData.Add(new RecordPair
                            {
                                IsMatch = false,
                                Record1 = allRecords[c],
                                Record2 = allRecords[d]
                            });
                        }
                        else
                        {
                            trainingData.Add(new RecordPair
                            {
                                IsMatch = false,
                                Record1 = allRecords[c + 1],
                                Record2 = allRecords[d]
                            });

                        }
                    }
                }
            }

            return trainingData;
        }

        static void Train(int numberOfTrees, string outputDirectory, double subsamplingPercentage,
            double minGain, int maximumEditDistance)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");

            //long workingSet = Process.GetCurrentProcess().WorkingSet64;
            //Console.WriteLine(workingSet / 1024.0 / 1024.0);
            //Console.ReadLine();

            //RecordPair[] copy = new RecordPair[trainingData.Count];
            //for (int c = 0; c < trainingData.Count; c++)
            //{
            //    copy[c] = trainingData[c]; 
            //}


            //workingSet = Process.GetCurrentProcess().WorkingSet64;
            //Console.WriteLine(workingSet / 1024.0 / 1024.0);
            //Console.ReadLine();

            //List<int> copy2 = new List<int>(); 
            //foreach(RecordPair pair in trainingData)
            //{
            //    copy2.Add(1); 
            //}

            //workingSet = Process.GetCurrentProcess().WorkingSet64;
            //Console.WriteLine(workingSet / 1024.0 / 1024.0);
            //Console.ReadLine();

            //int[] copy3 = new int[trainingData.Count]; 
            //for(int c=0;c<trainingData.Count;c++)
            //{
            //    copy3[c] = c; 
            //}


            //workingSet = Process.GetCurrentProcess().WorkingSet64;
            //Console.WriteLine(workingSet / 1024.0 / 1024.0);
            //Console.ReadLine();

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

                DecisionTreeBuilder treeBuilder = new DecisionTreeBuilder();
                DecisionTree tree = treeBuilder.Train(trainingDataSubset,
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

        static DecisionTree[] LoadForest(string forestDirectory)
        {
            BinaryFormatter bf = new BinaryFormatter();
            string[] treePaths = Directory.GetFiles(forestDirectory, "*.dat");
            DecisionTree[] forest = new DecisionTree[treePaths.Length];
            for (int c = 0; c < treePaths.Length; c++)
            {
                string treePath = treePaths[c];
                using (FileStream fin = File.OpenRead(treePath))
                {
                    forest[c] = (DecisionTree)bf.Deserialize(fin);
                }
            }
            return forest;
        }

        static void TestOnTrainingData()
        {
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");
            int consoleLeft = Console.CursorLeft;
            int consoleTop = Console.CursorTop;

            int gotRight = 0;
            int truePositive = 0;
            int trueNegative = 0;
            int falseNegative = 0;
            int falsePositive = 0;
            int numberOfMatches = 0;
            int numberOfNonMatches = 0;


            BinaryFormatter bf = new BinaryFormatter();

            DecisionTree[] forest = LoadForest("C:/users/brush/desktop/forest");

            int numberExamined = 0;
            Parallel.ForEach(trainingData, pair =>
            {
                bool actual = pair.IsMatch;

                if (actual)
                {
                    Interlocked.Increment(ref numberOfMatches);
                }
                else
                {
                    Interlocked.Increment(ref numberOfNonMatches);
                }

                bool guess = DecisionTreeBuilder.IsMatch(pair, forest);

                if (guess == actual)
                {
                    Interlocked.Increment(ref gotRight);

                    if (guess)
                    {
                        Interlocked.Increment(ref truePositive);
                    }
                    else
                    {
                        Interlocked.Increment(ref trueNegative);
                    }
                }
                else
                {
                    if (guess)
                    {
                        Interlocked.Increment(ref falsePositive);
                    }
                    else
                    {
                        Interlocked.Increment(ref falseNegative);
                    }
                }

                lock (bf)
                {
                    if (numberExamined % 1000000 == 0)
                    {
                        Console.SetCursorPosition(consoleLeft, consoleTop);
                        Console.WriteLine($"{(numberExamined / (trainingData.Count * 1.0)) * 100}% done");
                    }
                    numberExamined++;
                }

            });

            Console.WriteLine($"Sensitivity: {(truePositive / ((truePositive + falseNegative) * 1.0)) * 100}%");
            Console.WriteLine($"PPV: {(truePositive / ((truePositive + falsePositive) * 1.0)) * 100}%");
        }

        static void Main(string[] args)
        {
            //EditDistanceTests();

            Train(1, "C:/users/brush/desktop/forest", 1, 0, 3);
            //TestOnTrainingData();
        }
    }
}
