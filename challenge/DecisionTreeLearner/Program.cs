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

        static List<RecordPair> LoadTrainingDataFromNoHomoFile(string noHomoFilePath)
        {
            List<RecordPair> ret = new List<RecordPair>();
            string[] lines = File.ReadAllLines(noHomoFilePath);

            for (int c = 0; c < lines.Length; c += 4)
            {
                RecordPair pair = new RecordPair();
                pair.IsMatch = bool.Parse(lines[c]);
                pair.Record1 = DataCleaner.CleanRecord(Record.FromString(lines[c + 1]));
                pair.Record2 = DataCleaner.CleanRecord(Record.FromString(lines[c + 2]));

                ret.Add(pair);
            }

            return ret;
        }

        static List<RecordPair> BuildTrainingData(string inputFilePath, string inputMoreFilePath)
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
                    //if (rand.Next() % 10 == 0)
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
            }

            string[] extraLines = File.ReadAllLines(inputMoreFilePath);
            List<Record[]> moreGroups = new List<Record[]>();

            for (int c = 0; c < extraLines.Length; c++)
            {
                List<Record> group = new List<Record>();
                for (; c < extraLines.Length; c++)
                {
                    if (extraLines[c] == "")
                    {
                        break;
                    }
                    else
                    {
                        Record record = Record.FromString(extraLines[c]); 
                        group.Add(DataCleaner.CleanRecord(record));
                    }
                }
                moreGroups.Add(group.ToArray());
            }

            for (int c = 0; c < moreGroups.Count; c++)
            {
                // get the positives by iterating in the group. 
                Record[] recordsInGroupC = moreGroups[c];
                for (int d = 0; d < recordsInGroupC.Length; d++)
                {
                    Record record1 = recordsInGroupC[d];
                    for (int e = d; e < recordsInGroupC.Length; e++)
                    {
                        Record record2 = recordsInGroupC[e];
                        RecordPair pair = new RecordPair
                        {
                            IsMatch = true,
                            Record1 = record1,
                            Record2 = record2
                        };
                        trainingData.Add(pair);
                    }
                }

                //get the negatives by iterating everyone else
                for (int d = 0; d < moreGroups.Count; d++)
                {
                    //Console.WriteLine(d.ToString()); 
                    if (c != d)
                    {
                        Record[] others = moreGroups[d];
                        for (int e = 0; e < recordsInGroupC.Length; e++)
                        {
                            Record record1 = recordsInGroupC[e];
                            for (int f = 0; f < others.Length; f++)
                            {
                                Record record2 = others[f];
                                RecordPair pair = new RecordPair
                                {
                                    IsMatch = false,
                                    Record1 = record1,
                                    Record2 = record2
                                };
                                trainingData.Add(pair);
                            }
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
                trainingData = BuildTrainingData("mrns.csv", "more.csv");
            }
            else if (option == 2)
            {
                Console.Write("Nohomo file path:");
                string filePath = Console.ReadLine().Replace("\"","");
                trainingData = LoadTrainingDataFromNoHomoFile(filePath);
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

                //SplittingQuestion[] splittingQuestions = new SplittingQuestion[]
                //{
                //     new SplittingQuestion
                //     {
                //          BothFieldValuesAreEmpty = true,
                //           Field = FieldEnum.SSN,
                //            MatchType = MatchTypeEnum.EmptyMatch
                //     }
                //};


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
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv", "more.csv");

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
            List<string[]> finalDataSetListSplit = new List<string[]>();
            foreach (string finalDataSetRow in finalDataSetList)
            {
                string[] bits = finalDataSetRow.Split(',');
                finalDataSetListSplit.Add(bits);
            }

            DecisionTree[] forest = ForestLoader.FromDirectory("C:/users/brush/desktop/forest");

            List<Record> blackSheep = new List<Record>();
            Dictionary<Record, List<Record>> originalBlackSheepMatches = new Dictionary<Record, List<Record>>();

            // for each line of closed set ids
            int number = 0;
            //foreach (string closedSetIdList in closedSetIdLists)
            Parallel.ForEach(closedSetIdLists, closedSetIdList =>
            {
                Interlocked.Increment(ref number);
                if (number % 100 == 0)
                {
                    Console.Clear();
                    Console.WriteLine($"{(number / (closedSetIdLists.Length * 1.0)) * 100}%...");
                }

                // get the ids
                string[] enterpriseIds = closedSetIdList.Split(',');
                List<string[]> list = new List<string[]>();
                // find the matching records
                foreach (string[] bits in finalDataSetListSplit)
                {
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

                        for (int a = 0; a < recordsInSet.Length; a++)
                        {
                            Record recordA = recordsInSet[a];
                            bool matchFound = false;
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
                                lock (blackSheep)
                                {
                                    blackSheep.Add(recordA);
                                    originalBlackSheepMatches.Add(recordA, new List<Record>(recordsInSet.Where(n => !n.Equals(blackSheep))));
                                }
                            }
                        }
                        // transform them into the right colum format
                        break;
                    }
                }
            });

            Console.WriteLine("Finding better options...");
            Dictionary<Record, List<Record>> matches = ListAllMatches.GetMatches(blackSheep);

            using (StreamWriter betterOptions = File.CreateText("c:/users/brush/desktop/betterOptions.txt"))
            {
                using (StreamWriter crapOptions = File.CreateText("C:/users/brush/desktop/crapMatches.txt"))
                {
                    foreach (Record key in matches.Keys)
                    {
                        List<Record> options = matches[key];

                        if (options.Count > 0)
                        {
                            betterOptions.WriteLine($"{key}");
                            betterOptions.WriteLine("Original matches:");
                            foreach (Record originalMatch in originalBlackSheepMatches[key])
                            {
                                if (!originalMatch.Equals(key))
                                {
                                    betterOptions.WriteLine($"\t\t{originalMatch}");
                                }
                            }
                            betterOptions.WriteLine("Better options:");
                            foreach (Record betterOption in options)
                            {
                                betterOptions.WriteLine($"\t\t{betterOption}");
                            }
                            betterOptions.WriteLine("============================");
                        }
                        else
                        {
                            crapOptions.WriteLine($"{key}");
                            crapOptions.WriteLine("Original matches:");
                            foreach (Record originalMatch in originalBlackSheepMatches[key])
                            {
                                if (!originalMatch.Equals(key))
                                {
                                    crapOptions.WriteLine($"\t\t{originalMatch}");
                                }
                            }
                            crapOptions.WriteLine("============================");
                        }
                    }
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
           // Train(1, "C:/users/brush/desktop/forest", 1, 0, 3);
            //TestOnTrainingData();
            TestOnLucasClosedSets("D:/repos/mitchmatch/closedsets.txt", "C:/users/brush/desktop/finaldataset.csv", "C:/users/brush/desktop/forest");

            //Testers.TestSplitDirection.Test();

            //Testers.ListAllMatches.List();

            //MatchTypeMatcher.BasedOnEditDistance(new SplittingQuestion { Field = FieldEnum.Phone2 },
            //    "862-868-5040^^212-606-1687", "212-606-1687"); 
        }
    }
}
