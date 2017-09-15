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
    public static class TrainedDataTesters
    {
        public static void SearchForFalseNegatives(string closedSetPath, string finalDataSetPath,
            string forestDirectory, string outputFile, string stateFile)
        {
            MySQLConnector connector = MySQLConnector.Connect();
            connector.ClearMLFoundExtraSetMemberTable();

            Console.Write("Loading final dataset...");
            List<Record> finalDataSet = DataLoader.LoadFinalDataSet(finalDataSetPath);
            Console.WriteLine("...done");
            Console.Write("Loading closed sets file...");
            List<List<Record>> closedSets = DataLoader.LoadClosedSets(closedSetPath, finalDataSet);
            Console.WriteLine("...done");

            Console.Write("Loading decision tree forest...");
            DecisionTree[] forest =
                DataLoader.LoadForestFromDirectory(forestDirectory);
            Console.WriteLine("...done");

            Console.Write("Reading state file...");
            int startIndex = 0;
            //if (File.Exists(stateFile))
            //{
            //    startIndex = int.Parse(File.ReadAllText(stateFile));
            //}
            Console.WriteLine($"...done. Starting at {startIndex}.");

            Console.WriteLine("Starting....");
            int extraSetMembersFoundCount = 0;
            using (StreamWriter fout = File.AppendText(outputFile))
            {
                for (int c = startIndex; c < closedSets.Count;)
                {
                    Console.WriteLine($"Working on set {c + 1} of {closedSets.Count}");
                    List<Record> closedSet = closedSets[c];

                    List<Record> extraSetMembersFound = new List<Record>();
                    foreach (Record record in closedSet)
                    {
                        Parallel.ForEach(finalDataSet, otherRecord =>
                        //foreach (Record otherRecord in finalDataSet)
                        {
                            if (otherRecord.EnterpriseId != record.EnterpriseId)
                            {
                                if (DecisionTreeBuilder.IsMatch(new RecordPair
                                {
                                    Record1 = record,
                                    Record2 = otherRecord
                                }, forest, null))
                                {
                                    lock (extraSetMembersFound)
                                    {
                                        // is this NOT a set member and have we not added it yet...
                                        if (!extraSetMembersFound.Any(n => n.EnterpriseId == otherRecord.EnterpriseId) &&
                                            !closedSet.Any(n => n.EnterpriseId == otherRecord.EnterpriseId))
                                        {
                                            extraSetMembersFound.Add(otherRecord);
                                        }
                                    }
                                }
                            }
                        });
                    }

                    if (extraSetMembersFound.Count > 0)
                    {
                        //extraSetMembersFoundCount += extraSetMembersFound.Count;
                        //Console.WriteLine($"\tFound {extraSetMembersFoundCount} found so far.");
                        //int setId = connector.GetSetIdForSetGivenMember(closedSet.First());
                        //connector.CreateMLFoundExtraRecordsForSet(setId, extraSetMembersFound);

                        StringBuilder sb = new StringBuilder(1024);
                        sb.AppendLine("==========");
                        foreach (Record record in closedSet)
                        {
                            sb.AppendLine(record.ToString());
                        }
                        sb.AppendLine();
                        foreach (Record record in extraSetMembersFound)
                        {
                            sb.AppendLine(record.ToString());
                        }
                        sb.AppendLine();


                        fout.Write(sb.ToString());
                        fout.Flush();
                    }

                    //string toSave = $"[{string.Join(",", closedSet.Select(n => n.EnterpriseId))}][{string.Join(",", extraSetMembersFound.Select(n => n.EnterpriseId))}]\n";

                    c++;
                    File.WriteAllText(stateFile, c.ToString());
                }
            }
        }

        public static void TestOnTrainingData()
        {
            List<RecordPair> trainingData = DataLoader.BuildTrainingData("mrns.csv", "more.csv", "rejected.txt");

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

            DecisionTree[] forest = DataLoader.LoadForestFromDirectory("C:/users/brush/desktop/forest");

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

                bool guess = DecisionTreeBuilder.IsMatch(pair, forest, null);

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

        public static void TestOnLucasClosedSets(string closedSetPath, string finalDataSetPath, string forestDirectory)
        {
            string[] finalDataSetList = File.ReadAllLines(finalDataSetPath);
            string[] closedSetIdLists = File.ReadAllLines(closedSetPath);
            List<string[]> finalDataSetListSplit = new List<string[]>();
            foreach (string finalDataSetRow in finalDataSetList)
            {
                string[] bits = finalDataSetRow.Split(',');
                finalDataSetListSplit.Add(bits);
            }

            DecisionTree[] forest = DataLoader.LoadForestFromDirectory("C:/users/brush/desktop/forest");

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

                                    bool match = DecisionTreeBuilder.IsMatch(pair, forest, null);

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
    }
}
