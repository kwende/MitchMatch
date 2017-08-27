using DecisionTreeLearner.NLP;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Data
{
    public static class DataLoader
    {
        public static List<Record> LoadFinalDataSet(string finalDataSetPath)
        {
            List<Record> ret = new List<Record>();
            IEnumerable<string> lines = File.ReadLines(finalDataSetPath);
            foreach (string line in lines)
            {
                string[] bits = line.Split(',');
                if (int.Parse(bits[0]) > 15374761)
                {
                    ret.Add(DataCleaner.CleanRecord(Record.FromFinalDatasetString(line)));
                }
            }
            return ret;
        }

        public static List<List<Record>> LoadClosedSets(string closedSetPath, string finalDataSetPath)
        {
            List<Record> finalDataSet = LoadFinalDataSet(finalDataSetPath);
            return LoadClosedSets(closedSetPath, finalDataSet);
        }

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

        public static List<RecordPair> BuildTrainingData(string inputFilePath, string inputMoreFilePath)
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
    }
}
