using DecisionTreeLearner.NLP;
using DecisionTreeLearner.Testers;
using DecisionTreeLearner.Tree;
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

namespace DecisionTreeLearner
{
    class Program
    {
        static List<RecordPair> LoadTrainingData(string positivesFile, string negativesFile)
        {
            List<RecordPair> ret = new List<RecordPair>();

            string[] lines = File.ReadAllLines(positivesFile);

            for (int c = 0; c < lines.Length; c += 4)
            {
                RecordPair pair = new RecordPair();
                pair.Record1 = Record.FromString(lines[c]);
                pair.Record2 = Record.FromString(lines[c + 2]);
                pair.IsMatch = true;

                ret.Add(pair);
            }

            IEnumerable<string> lineReader = File.ReadLines(negativesFile);
            IEnumerator<string> lineReaderEnumerator = lineReader.GetEnumerator();

            while (lineReaderEnumerator.MoveNext())
            {
                RecordPair pair = new RecordPair();
                pair.Record1 = Record.FromString(lineReaderEnumerator.Current);

                lineReaderEnumerator.MoveNext();
                lineReaderEnumerator.MoveNext();

                pair.Record2 = Record.FromString(lineReaderEnumerator.Current);

                lineReaderEnumerator.MoveNext();

                pair.IsMatch = false;

                ret.Add(pair);
            }

            return ret;
        }

        static List<RecordPair> BuildTrainingData(string inputFilePath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();

            Console.Write("Reading training file...");
            string[] lines = File.ReadAllLines(inputFilePath);
            Console.WriteLine("...done");

            List<Record> allRecords = new List<Record>();
            for (int c = 0; c < lines.Length; c += 3)
            {
                allRecords.Add(Record.FromString(lines[c]));
                allRecords.Add(Record.FromString(lines[c + 1]));
            }

            allRecords = DataCleaner.CleanRecordPairs(allRecords);

            Console.Write("Building training data...");
            for (int c = 0; c < allRecords.Count; c += 2)
            {
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
            Console.WriteLine("...done");
            return trainingData;
        }

        static void Train(int numberOfTrees, string outputDirectory, double subsamplingPercentage,
            double minGain, int maximumEditDistance)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");
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

                FieldEnum[] fieldsOnWhichToTrain = new FieldEnum[]
                {
                    FieldEnum.Address1,
                    FieldEnum.Address2,
                    FieldEnum.City,
                    FieldEnum.DOB,
                    FieldEnum.Email,
                    FieldEnum.FirstName,
                    FieldEnum.Gender,
                    FieldEnum.LastName,
                    FieldEnum.MiddleName,
                    FieldEnum.Phone1,
                    FieldEnum.Phone2,
                    FieldEnum.SSN,
                    FieldEnum.State,
                    FieldEnum.Zip
                };
                SplittingQuestion[] splittingQuestions = DecisionTreeBuilder.GenerateSplittingQuestions(fieldsOnWhichToTrain, maximumEditDistance);


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

            DecisionTree[] forest = ForestLoader.FromDirectory("C:/users/brush/desktop/forest");

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

                bool guess = DecisionTreeBuilder.IsMatch(pair, forest, false);

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

        static void TestOnLucasClosedSets(string closedSetPath, string finalDataSetPath, string forestDirectory)
        {
            string[] finalDataSetList = File.ReadAllLines(finalDataSetPath);
            string[] closedSetIdLists = File.ReadAllLines(closedSetPath);

            DecisionTree[] forest = ForestLoader.FromDirectory("C:/users/brush/desktop/forest");

            // for each line of closed set ids
            int number = 0;
            foreach (string closedSetIdList in closedSetIdLists)
            {
                Console.Write($"{(number / (closedSetIdLists.Length * 1.0)) * 100}%...");
                number++;

                List<Record> blackSheep = new List<Record>();
                // get the ids
                string[] enterpriseIds = closedSetIdList.Split(',');
                List<string[]> list = new List<string[]>();
                // find the matching records
                foreach (string finalDataSetRow in finalDataSetList)
                {
                    string[] bits = finalDataSetRow.Split(',');
                    foreach (string enterpriseId in enterpriseIds)
                    {
                        if (bits[0] == enterpriseId)
                        {
                            list.Add(bits);
                        }
                    }

                    // if we have found all of the records
                    if (list.Count == enterpriseIds.Length)
                    {
                        Record[] recordsInSet =
                            list.Select(n => NLP.DataCleaner.CleanRecord(
                                Record.FromFinalDatasetString(n))).ToArray();

                        //foreach (Record recordA in recordsInSet)

                        for (int a = 0; a < recordsInSet.Length; a++)
                        {
                            Record recordA = recordsInSet[a];
                            bool matchFound = false;
                            //foreach (Record recordB in recordsInSet)
                            for (int b = 0; b < recordsInSet.Length; b++)
                            {
                                Record recordB = recordsInSet[b];
                                if (recordA != recordB)
                                {
                                    RecordPair pair = new RecordPair
                                    {
                                        Record1 = recordA,
                                        Record2 = recordB
                                    };

                                    bool match = DecisionTreeBuilder.IsMatch(pair, forest, false);

                                    if (match)
                                    {
                                        matchFound = true;
                                        break;
                                    }
                                }
                            }

                            if (!matchFound)
                            {
                                blackSheep.Add(recordA);
                            }
                        }
                        // transform them into the right colum format
                        break;
                    }
                }

                if (blackSheep.Count > 0)
                {
                    StringBuilder outputString = new StringBuilder();
                    outputString.AppendLine($"{blackSheep.Count} out of {enterpriseIds.Length} failed");
                    foreach (Record sheep in blackSheep)
                    {
                        outputString.AppendLine(sheep.ToString());
                    }

                    Console.WriteLine("...oops");
                    File.AppendAllText($"C:/users/brush/desktop/nomatches/{Guid.NewGuid().ToString().Replace("-", "")}.txt",
                        outputString.ToString());
                }
                else
                {
                    Console.WriteLine("...good"); 
                }
            }
        }

        static void Merge()
        {
            using (StreamWriter sw = File.CreateText("c:/users/brush/desktop/merged.csv"))
            {
                foreach (string file in Directory.GetFiles("c:/users/brush/desktop/nomatches"))
                {
                    string[] lines = File.ReadAllLines(file);

                    sw.WriteLine(lines[1]);
                    sw.WriteLine(lines[2]);
                    sw.WriteLine();
                }
            }
        }

        static void Main(string[] args)
        {
            //Merge(); 

            //EditDistanceTests();
            //TestTree();
            //TestTree2();
            //TestTree3();
            //TestTree4(); 
            //Train(1, "C:/users/brush/desktop/forest", 1, 0, 3);
            //TestOnTrainingData();
            TestOnLucasClosedSets("D:/repos/mitchmatch/closedsets.txt", "C:/users/brush/desktop/finaldataset.csv", "C:/users/brush/desktop/forest");

            //Testers.TestSplitDirection.Test(); 

            //Testers.ListAllMatches.List(); 
        }
    }
}
