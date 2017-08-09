using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner
{
    class Program
    {
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

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");

            DecisionTreeBuilder treeBuilder = new DecisionTreeBuilder();
            DecisionTree tree = treeBuilder.Train(trainingData);

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fout = File.Create("C:/users/brush/desktop/tree.dat"))
            {
                bf.Serialize(fout, tree);
            }
            sw.Stop();

            Console.WriteLine($"Whole operation took {sw.ElapsedMilliseconds / 1000.0 / 60.0} minutes");
        }
    }
}
