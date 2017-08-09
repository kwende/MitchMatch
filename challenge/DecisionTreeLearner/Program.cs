using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
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
                if(c%100==0)
                {
                    Console.WriteLine(((c / (allRecords.Count * 1.0)) * 100) + "%"); 
                }

                trainingData.Add(new RecordPair
                {
                    IsMatch = true,
                    Record1 = allRecords[c],
                    Record2 = allRecords[c + 1],
                });

                for (int d = 0; d < allRecords.Count; d += 2)
                {
                    if (c != d)
                    {
                        trainingData.Add(new RecordPair
                        {
                            IsMatch = false,
                            Record1 = allRecords[c],
                            Record2 = allRecords[d]
                        });

                        trainingData.Add(new RecordPair
                        {
                            IsMatch = false,
                            Record1 = allRecords[c + 1],
                            Record2 = allRecords[d]
                        });
                    }
                }
            }

            return trainingData;
        }

        static void Main(string[] args)
        {
            DecisionTree tree = new DecisionTree();

            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");
        }
    }
}
