using DecisionTreeLearner.NLP;
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
        static void TestTree3()
        {
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");
            Console.Beep();

            RecordPair match = trainingData.Where(m => m.Record1.SSN == "805474518" && m.IsMatch == true).First();
            match = NLP.DataCleaner.CleanRecordPair(match, "StreetSuffixes.csv");
            RecordPair noMatch = trainingData.Where(m => m.Record1.Phone1 == "8485746346" && m.Record2.Phone1 == "9292655229" && m.IsMatch == false).First();
            noMatch = NLP.DataCleaner.CleanRecordPair(noMatch, "StreetSuffixes.csv");

            FieldEnum[] fieldsOnWhichToTrain = new FieldEnum[]
            {
                    FieldEnum.Address1,
            };
            SplittingQuestion[] splittingQuestions = DecisionTreeBuilder.GenerateSplittingQuestions(fieldsOnWhichToTrain, 3);

            DecisionTreeBuilder builder = new DecisionTreeBuilder();
            builder.Train(new List<RecordPair>() { match, noMatch }, splittingQuestions, 1, 0, 3);
        }

        static void TestTree2()
        {
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");

            RecordPair match = trainingData.Where(m => m.Record1.Address1 == "1590 EAST NEW YORK" && m.IsMatch == true).First();
            RecordPair noMatch = trainingData.Where(m => m.Record1.Address1 == "11130 174TH STREET" && m.Record2.Address1 == "SEE 3009793" && m.IsMatch == false).First();

            FieldEnum[] fieldsOnWhichToTrain = new FieldEnum[]
            {
                    FieldEnum.SSN,
            };
            SplittingQuestion[] splittingQuestions = DecisionTreeBuilder.GenerateSplittingQuestions(fieldsOnWhichToTrain, 3);

            DecisionTreeBuilder builder = new DecisionTreeBuilder();
            builder.Train(new List<RecordPair>() { match, noMatch }, splittingQuestions, 1, 0, 3);
        }

        static void TestTree()
        {
            RecordPair matchingPair = new RecordPair();

            matchingPair.Record1 = Record.FromString("KRISTEN,,TQHKERSLEY,,F,866071061,12 / 12 / 1921,2673982600,973 - 376 - 2715 ^^ 267 - 398 - 2600,430 GRAND CONC,25,BRONX,NY,10451,,,4874700,15531659,KRISTEN TQHKERSLEY");
            matchingPair.Record2 = Record.FromString("KRISTEN,,TANKERSLEY,,F,0,12 / 12 / 1921,2673982600,267 - 398 - 2600,1025 BOYNTON AVENUE,2D,BRONX,NY,10472,,,4874701,15893245,KIRSTEN TQHKERSLEY");

            RecordPair notMatching = new RecordPair();
            notMatching.Record1 = Record.FromString("SHAEE,,ZEITNER,,F,838540142,13 / 09 / 1997,5183581057,518 - 358 - 1057,622 E 229TH STREET,1,BRONX,NY,10466,,SZEITNER @AMGGT.COM,4873699,15738928,XIMENA ZEITNER");
            notMatching.Record2 = Record.FromString("JIMENA,,ZEITNER,,F,838540142,13 / 09 / 1997,5183581057,518 - 358 - 1057,622 EAST 229TH STREET,1,BRONX,NY,10466,,,4873703,15821627,JIMENA ZEITNER");

            bool directionMatching = DecisionTreeBuilder.ComputeSplitDirection(new SplittingQuestion
            {
                Field = FieldEnum.SSN,
                MatchType = MatchTypeEnum.EmptyMatch,
                OneFieldValueIsEmpty = true,
            }, matchingPair);

            Debug.Assert(directionMatching == true);

            bool directionNotMatching = DecisionTreeBuilder.ComputeSplitDirection(new SplittingQuestion
            {
                Field = FieldEnum.SSN,
                MatchType = MatchTypeEnum.EmptyMatch,
                OneFieldValueIsEmpty = true,
            }, notMatching);

            Debug.Assert(directionMatching == false);

            return;
        }

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
            //return trainingData; 
            return DataCleaner.CleanRecordPairs(trainingData, "StreetSuffixes.csv");
        }

        static void Train(int numberOfTrees, string outputDirectory, double subsamplingPercentage,
            double minGain, int maximumEditDistance)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<RecordPair> trainingData = BuildTrainingData("mrns.csv");


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

                        //lock (forest)
                        //{
                        //    File.AppendAllText("c:/users/brush/desktop/falseNegatives.csv",
                        //                 pair.ToString() + "\n");
                        //}
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
            //TestTree();
            //TestTree2();
            //TestTree3();
            Train(1, "C:/users/brush/desktop/forest", 1, 0, 3);
            //TestOnTrainingData();
        }
    }
}
