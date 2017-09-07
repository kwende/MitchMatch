using DecisionTreeLearner.Data;
using DecisionTreeLearner.NLP;
using DecisionTreeLearner.Testers;
using DecisionTreeLearner.DataTypes;
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
using DecisionTreeLearner.Tree;
using DecisionTreeLearner.DataTypes.MySQL;
using DecisionTreeLearner.ForJosh;

namespace DecisionTreeLearner
{
    class Program
    {
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
                trainingData = DataLoader.BuildTrainingData("mrns.csv", "more.csv", "rejected.txt");

                if (trainingData.Any(n => n.Record1 == null || n.Record2 == null))
                {
                    Console.WriteLine("YUP, ITS HERE");
                }
            }
            else if (option == 2)
            {
                Console.Write("Nohomo file path:");
                string filePath = Console.ReadLine().Replace("\"", "");
                trainingData = DataLoader.LoadTrainingDataFromNoHomoFile(filePath);
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

                SplittingQuestion[] splittingQuestions = DecisionTreeBuilder.GenerateSplittingQuestions(maximumEditDistance);

                DecisionTreeBuilder treeBuilder = new DecisionTreeBuilder();


                List<Tuple<SplittingQuestion, bool>> preComputedQuestions = new List<Tuple<SplittingQuestion, bool>>();

                preComputedQuestions.Add(new Tuple<SplittingQuestion, bool>(
                    new SplittingQuestion
                    {
                        Field = FieldEnum.DOB,
                        MatchType = MatchTypeEnum.EditDistance,
                        MaximumEditDistance = 0,
                    }, false));
                preComputedQuestions.Add(new Tuple<SplittingQuestion, bool>(
                    new SplittingQuestion
                    {
                        Field = FieldEnum.MRN,
                        MatchType = MatchTypeEnum.MRNDistance,
                        MRNMaxDistance = 100,
                    }, false));
                preComputedQuestions.Add(new Tuple<SplittingQuestion, bool>(
                    new SplittingQuestion
                    {
                        Field = FieldEnum.LastName,
                        MatchType = MatchTypeEnum.EditDistance,
                        MaximumEditDistance = 1
                    }, false));
                preComputedQuestions.Add(new Tuple<SplittingQuestion, bool>(
                    new SplittingQuestion
                    {
                        Field = FieldEnum.DOB,
                        MatchType = MatchTypeEnum.EditDistance,
                        MaximumEditDistance = 1,
                    }, false));
                preComputedQuestions.Add(new Tuple<SplittingQuestion, bool>(null, false));

                DecisionTree tree = treeBuilder.Train(trainingDataSubset, splittingQuestions,
                    subsamplingPercentage, minGain, null);

                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fout = File.Create(Path.Combine(outputDirectory, $"tree{c}.dat")))
                {
                    bf.Serialize(fout, tree);
                }
            }

            sw.Stop();

            Console.WriteLine($"Whole operation took {sw.ElapsedMilliseconds / 1000.0 / 60.0} minutes");
        }

        static void DoJoshInThe(List<List<int>> butt)
        {
            AutoPasser.DoIt(butt);
        }


        static void Main(string[] args)
        {
            //List<List<int>> butt = new List<List<int>>();

            //butt.Add(new List<int>(new int[] { 15924699, 15998364, 15776341 }));
            //butt.Add(new List<int>(new int[] { 15409759, 15719372, 15603834 }));
            //butt.Add(new List<int>(new int[] { 15605703, 15666227, 15436724 }));

            //DoJoshInThe(butt);

            //using (FileStream fin = File.OpenRead(@"D:\allNodes\b65b18fb8ef84d25b1dc4737666ea719"))
            //{
            //    byte[] buffer = new byte[1024];
            //    fin.Read(buffer, 0, buffer.Length);

            //    string lines = ASCIIEncoding.ASCII.GetString(buffer);

            //    File.WriteAllText("C:/users/brush/desktop/fart.txt",
            //        lines);

            //}

            //EndNodeMLMatchFinders.Find(); 
            //string response = Regex.Replace("SCOT   DREYER      5", " + ", " ", RegexOptions.None);

            //IterativeLearner.DoIt("misfits.txt", "mrns.csv", "more.csv", "rejected.txt");

            //Train(1, "C:/users/brush/desktop/forest", 1, 0, 3);
            //TestOnTrainingData();
            //TrainedDataTesters.TestOnLucasClosedSets("D:/repos/mitchmatch/closedsets.txt", "C:/users/brush/desktop/finaldataset.csv", "C:/users/brush/desktop/forest");

            //Testers.TestSplitDirection.Test();



            //Testers.ListAllMatches.List();

            //Testers.TrainedDataTesters.SearchForFalseNegatives(
            //    "D:/repos/mitchmatch/closedsets.txt",
            //    "C:/users/brush/desktop/finaldataset.csv",
            //    "C:/users/brush/desktop/forest",
            //    "D:/mitchMatchFalseNegativeSearchResults.txt",
            //    "D:/mitchMatchFalsenegatievSearchState.txt");
        }
    }
}
