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

            for (int c = 0; c < lines.Length; c += 3)
            {
                string record1 = lines[c];
                string record2 = lines[c];
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
