using DecisionTreeLearner.NLP;
using DecisionTreeLearner.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DecisionTreeLearner.Data
{
    public static class DataLoader
    {
        public static IEnumerable<RecordPair> LoadNegativesFromAnswerKey(List<RecordPair> positives)
        {
            for (int a = 0; a < positives.Count; a++)
            {
                RecordPair matchPair1 = positives[a];

                for (int b = a + 1; b < positives.Count; b++)
                {
                    RecordPair matchPair2 = positives[b];

                    if (matchPair1.Record1.EnterpriseId != matchPair2.Record1.EnterpriseId &&
                        matchPair1.Record1.EnterpriseId != matchPair2.Record2.EnterpriseId &&
                        matchPair1.Record2.EnterpriseId != matchPair2.Record1.EnterpriseId &&
                        matchPair1.Record2.EnterpriseId != matchPair2.Record2.EnterpriseId)
                    {
                        RecordPair noMatch = new RecordPair
                        {
                            IsMatch = false,
                            Record1 = matchPair1.Record1,
                            Record2 = matchPair2.Record1
                        };

                        yield return noMatch;

                        noMatch = new RecordPair
                        {
                            IsMatch = false,
                            Record1 = matchPair1.Record2,
                            Record2 = matchPair2.Record2,
                        };

                        yield return noMatch;

                        noMatch = new RecordPair
                        {
                            IsMatch = false,
                            Record1 = matchPair1.Record1,
                            Record2 = matchPair2.Record2
                        };

                        yield return noMatch;

                        noMatch = new RecordPair
                        {
                            IsMatch = false,
                            Record1 = matchPair1.Record2,
                            Record2 = matchPair2.Record1
                        };

                        yield return noMatch;
                    }
                }
            }
        }

        public static List<RecordPair> LoadAllPositivesFromAnswerKey(string answerKeyPath, Dictionary<int, Record> finalDataSet)
        {
            List<RecordPair> ret = new List<RecordPair>();

            IEnumerable<string> lines = File.ReadLines(answerKeyPath);
            Parallel.ForEach(lines, line =>
            {
                string[] bits = line.Split(',');
                if (bits.Length == 3)
                {
                    Record[] records = bits.Take(2).Select(n => finalDataSet[int.Parse(n)]).ToArray();
                    RecordPair pair = new RecordPair
                    {
                        IsMatch = true,
                        Record1 = records[0],
                        Record2 = records[1],
                    };
                    lock (ret)
                    {
                        ret.Add(pair);
                    }

                }
            });
            return ret;
        }

        public static string[] SmartSplit(string csvLine)
        {
            List<int> separatorIndices = new List<int>();
            bool withinQuotes = false;
            for (int c = 0; c < csvLine.Length; c++)
            {
                if (csvLine[c] == '"')
                {
                    withinQuotes = !withinQuotes;
                }
                else if (csvLine[c] == ',' && !withinQuotes)
                {
                    separatorIndices.Add(c);
                }
            }

            string[] ret = new string[separatorIndices.Count + 1];
            int startIndex = 0;
            for (int c = 0; c < separatorIndices.Count; c++)
            {
                int separatorIndex = separatorIndices[c];
                ret[c] =
                    csvLine.Substring(startIndex, separatorIndex - startIndex);
                startIndex += ret[c].Length + 1;
            }
            ret[ret.Length - 1] = csvLine.Substring(startIndex);

            return ret;
        }

        public static Dictionary<int, Record> LoadFinalDataSet(string finalDataSetPath)
        {
            Dictionary<int, Record> ret = new Dictionary<int, Record>();
            IEnumerable<string> lines = File.ReadLines(finalDataSetPath);
            bool encounteredHeader = false;
            foreach (string line in lines)
            {
                if (!encounteredHeader)
                {
                    encounteredHeader = true;
                }
                else
                {
                    string[] bits = line.Split(',');
                    if (bits[0] != "" && int.Parse(bits[0]) >= 15374761)
                    {
                        Record cleanedRecord = DataCleaner.CleanRecord(Record.FromFinalDatasetString(line));
                        ret.Add(cleanedRecord.EnterpriseId, cleanedRecord);
                    }
                }

            }
            return ret;
        }

        //public static List<List<Record>> LoadClosedSets(string closedSetPath, string finalDataSetPath)
        //{
        //    List<Record> finalDataSet = LoadFinalDataSet(finalDataSetPath);
        //    return LoadClosedSets(closedSetPath, finalDataSet);
        //}

        public static DecisionTree[] LoadForestFromDirectory(string forestDirectory)
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

        public static List<List<Record>> LoadClosedSets(string closedSetsPath, List<Record> preloadedFinalDataSet)
        {
            List<List<Record>> ret = new List<List<Record>>();
            IEnumerable<string> closedSetsLines = File.ReadLines(closedSetsPath);
            foreach (string closedSetLine in closedSetsLines)
            {
                List<Record> set = new List<Record>();
                int[] enterpriseIds = closedSetLine.Split(',').Select(n => int.Parse(n)).ToArray();

                foreach (int enterpriseId in enterpriseIds)
                {
                    set.Add(preloadedFinalDataSet.First(n => n.EnterpriseId == enterpriseId));
                }



                ret.Add(set);
            }

            return ret;
        }

        public static List<RecordPair> GetPairsFromMisfitsFile(string misfitsFilePath, Dictionary<int, Record> finalDataSet)
        {
            string[] lines = File.ReadAllLines(misfitsFilePath);

            List<RecordPair> ret = new List<RecordPair>();

            //for (int c = 0; c < lines.Length; c++)
            Parallel.For(0, lines.Length, c =>
            {
                if (!string.IsNullOrEmpty(lines[c]))
                {
                    int[] enterpriseIds = lines[c].Split(',').Select(n => int.Parse(n)).ToArray();

                    RecordPair pair = new RecordPair();
                    pair.IsMatch = false;
                    pair.Record1 = finalDataSet[enterpriseIds[0]]; 
                    pair.Record2 = finalDataSet[enterpriseIds[1]];

                    lock(ret)
                    {
                        ret.Add(pair);
                    }
                }
            });

            //int duplicates = 0;
            //List<RecordPair> cleaned = new List<RecordPair>();
            ////foreach (RecordPair pairA in ret)
            //int counter = 0;
            ////Parallel.ForEach(ret, pairA =>
            //Parallel.For(0, ret.Count, n =>
            //{
            //    RecordPair pairA = ret[n];

            //    Interlocked.Increment(ref counter);

            //    if (counter % 1000 == 0)
            //    {
            //        Console.WriteLine($"{counter.ToString("N0")}/{ret.Count.ToString("N0")}");
            //    }

            //    bool isDuplicate = false;
            //    // foreach (RecordPair pairB in ret)
            //    for (int c = n + 1; c < ret.Count; c++)
            //    {
            //        RecordPair pairB = ret[c];

            //        if (pairA != pairB && pairA.Equals(pairB))
            //        {
            //            duplicates++;
            //            isDuplicate = true;
            //            break;
            //        }
            //    }

            //    if (!isDuplicate)
            //    {
            //        lock (cleaned)
            //        {
            //            cleaned.Add(pairA);
            //        }
            //    }
            //});

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/cleaned.csv"))
            //{
            //    foreach (RecordPair pair in cleaned)
            //    {
            //        sw.WriteLine($"{pair.Record1.EnterpriseId},{pair.Record2.EnterpriseId}");
            //    }
            //}

            //Console.WriteLine($"There are {ret} entries. After cleaning there are {cleaned.Count}");
            //Console.ReadLine();

            return ret;
        }

        public static List<RecordPair> LoadTrainingDataFromNoHomoFile(string noHomoFilePath)
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

        public static bool PassesBigBucketFilter(RecordPair pair, double percentageOfBigBucketToAllow = .5)
        {
            return true;
            //Random rand = new Random();
            //bool fallsIntoBigBucket = false;
            //int dobEditDistance = EditDistance.Compute(pair.Record1.DOB, pair.Record2.DOB);
            //if (!(dobEditDistance <= 0))
            //{
            //    if (!(System.Math.Abs(pair.Record1.MRN - pair.Record2.MRN) <= 100))
            //    {
            //        if (!(EditDistance.Compute(pair.Record1.LastName, pair.Record2.LastName) <= 1))
            //        {
            //            if (!(dobEditDistance <= 1))
            //            {
            //                fallsIntoBigBucket = true;
            //            }
            //        }
            //    }
            //}

            //bool shouldBeUsed = true;
            //if (fallsIntoBigBucket)
            //{
            //    shouldBeUsed = rand.NextDouble() <= percentageOfBigBucketToAllow;
            //}

            //return shouldBeUsed;
        }

        public static List<Record> GetCleanedRecordsFromMRNFile(string mrnFilePath)
        {
            string[] lines = File.ReadAllLines(mrnFilePath);

            List<Record> allRecords = new List<Record>();
            for (int c = 0; c < lines.Length; c += 3)
            {
                allRecords.Add(Record.FromString(lines[c]));
                allRecords.Add(Record.FromString(lines[c + 1]));
            }

            return DataCleaner.CleanRecordPairs(allRecords);
        }

        public static List<RecordPair> GetPositivesFromMRNData(string inputFilePath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();

            List<Record> mrnRecords = GetCleanedRecordsFromMRNFile(inputFilePath);

            for (int c = 0; c < mrnRecords.Count; c += 2)
            {
                trainingData.Add(new RecordPair
                {
                    IsMatch = true,
                    Record1 = mrnRecords[c],
                    Record2 = mrnRecords[c + 1],
                });
            }

            return trainingData;
        }

        public static List<RecordPair> GetRandomPairsForMRNData(string inputFilePath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();

            List<Record> mrnRecords = GetCleanedRecordsFromMRNFile(inputFilePath);

            Parallel.For(0, mrnRecords.Count() / 2, c =>
            {
                c *= 2;

                lock (trainingData)
                {
                    trainingData.Add(new RecordPair
                    {
                        IsMatch = true,
                        Record1 = mrnRecords[c],
                        Record2 = mrnRecords[c + 1],
                    });
                }

                Random rand = new Random();
                for (int d = 0; d < mrnRecords.Count; d += 2)
                {
                    {
                        if (c != d)
                        {
                            if (rand.Next() % 2 == 0)
                            {
                                RecordPair pair = new RecordPair
                                {
                                    IsMatch = false,
                                    Record1 = mrnRecords[c],
                                    Record2 = mrnRecords[d]
                                };
                                if (PassesBigBucketFilter(pair))
                                {
                                    lock (trainingData)
                                    {
                                        trainingData.Add(pair);
                                    }
                                }
                            }
                            else
                            {
                                RecordPair pair = new RecordPair
                                {
                                    IsMatch = false,
                                    Record1 = mrnRecords[c + 1],
                                    Record2 = mrnRecords[d]
                                };
                                if (PassesBigBucketFilter(pair))
                                {
                                    lock (trainingData)
                                    {
                                        trainingData.Add(pair);
                                    }
                                }
                            }

                        }
                    }
                }
            });

            return trainingData;
        }

        public static IEnumerable<RecordPair> GetAllNegativeRecordPairsForMRNData(string mrnRecordPath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();
            List<Record> mrnRecords = GetCleanedRecordsFromMRNFile(mrnRecordPath);

            for (int c = 0; c < mrnRecords.Count(); c += 2)
            {
                for (int d = 0; d < mrnRecords.Count; d += 2)
                {
                    if (c != d)
                    {
                        RecordPair nonMatchPair1 = new RecordPair
                        {
                            IsMatch = false,
                            Record1 = mrnRecords[c],
                            Record2 = mrnRecords[d]
                        };

                        yield return nonMatchPair1;

                        RecordPair nonMatchPair2 = new RecordPair
                        {
                            IsMatch = false,
                            Record1 = mrnRecords[c + 1],
                            Record2 = mrnRecords[d]
                        };

                        yield return nonMatchPair2;
                    }
                }
            }
        }

        public static List<RecordPair> GetHandPassedSets(string inputMoreFilePath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();

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
                        if (PassesBigBucketFilter(pair))
                        {
                            trainingData.Add(pair);
                        }
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
                                if (PassesBigBucketFilter(pair))
                                {
                                    trainingData.Add(pair);
                                }
                                //trainingData.Add(pair);
                            }
                        }
                    }
                }
            }

            return trainingData;
        }

        public static List<RecordPair> GetRejectedRecordPairs(string rejectFile)
        {
            List<RecordPair> trainingData = new List<RecordPair>();
            string[] rejectedLines = File.ReadAllLines(rejectFile);
            for (int c = 0; c < rejectedLines.Length; c += 3)
            {
                string line1 = rejectedLines[c];
                string line2 = rejectedLines[c + 1];

                RecordPair failurePair = new RecordPair();
                failurePair.Record1 = DataCleaner.CleanRecord(Record.FromString(line1));
                failurePair.Record2 = DataCleaner.CleanRecord(Record.FromString(line2));
                failurePair.IsMatch = false;
                if (PassesBigBucketFilter(failurePair))
                {
                    trainingData.Add(failurePair);
                }
            }

            return trainingData;
        }

        public static List<RecordPair> BuildTrainingData(string inputFilePath,
            string handTaggedData, string rejectedFilePath)
        {
            List<RecordPair> trainingData = new List<RecordPair>();

            Console.WriteLine("Building training data...");
            //for (int c = 0; c < allRecords.Count; c += 2)

            Console.Write("\tBuilding MRN-based data...");
            trainingData.AddRange(GetRandomPairsForMRNData(inputFilePath));
            Console.WriteLine("...done");

            Console.Write("\tBuilding hand-tagged data...");
            trainingData.AddRange(GetHandPassedSets(handTaggedData));
            Console.WriteLine("...done");

            Console.Write("\tBuilding rejected data...");
            trainingData.AddRange(GetRejectedRecordPairs(rejectedFilePath));
            Console.WriteLine("...done");

            Console.WriteLine("...done");

            return trainingData;
        }
    }
}
