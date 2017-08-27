﻿using DecisionTreeLearner.NLP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    public class DecisionTreeBuilder
    {
        private static double ComputeShannonEntropy(RecordPair[] pairs)
        {
            double entropy = 0.0;

            int numberInMatchColumn = 0;
            int numberInNoMatchColum = 0;

            for (int c = 0; c < pairs.Length; c++)
            {
                RecordPair pair = pairs[c];

                if (pair.IsMatch)
                {
                    numberInMatchColumn++;
                }
                else
                {
                    numberInNoMatchColum++;
                }
            }

            double totalLength = pairs.Length;

            double ratio = numberInMatchColumn / totalLength;
            entropy = -(ratio * System.Math.Log(ratio, 2));

            ratio = numberInNoMatchColum / totalLength;
            entropy += -(ratio * System.Math.Log(ratio, 2));

            return entropy;
        }

        private static double ComputeShannonEntropy(int numberInMatchColumn, int numberInNoMatchColumn)
        {
            double entropy = 0.0;
            double totalLengthAsDouble = numberInMatchColumn + numberInNoMatchColumn;

            if (totalLengthAsDouble > 0)
            {
                double ratio = 0.0;

                if (numberInMatchColumn > 0)
                {
                    ratio = numberInMatchColumn / totalLengthAsDouble;
                    entropy = -(ratio * System.Math.Log(ratio, 2));
                }

                if (numberInNoMatchColumn > 0)
                {
                    ratio = numberInNoMatchColumn / totalLengthAsDouble;
                    entropy += -(ratio * System.Math.Log(ratio, 2));
                }
            }

            return entropy;
        }

        private static double ComputeGain(double comparisonEntropy, double leftEntropy, int leftLength, double rightEntropy, int rightLength)
        {
            double totalLength = leftLength + rightLength;

            double gain = comparisonEntropy - (leftEntropy * leftLength / totalLength + rightEntropy * rightLength / totalLength);
            return gain;
        }

        private static double ComputeGain(double comparisonEntropy, RecordPair[] left, RecordPair[] right)
        {
            double leftEntropy = ComputeShannonEntropy(left);
            double rightEntropy = ComputeShannonEntropy(right);

            double leftLength = left.Length;
            double rightLength = right.Length;
            double totalLength = leftLength + rightLength;

            double gain = comparisonEntropy - (leftEntropy * leftLength / totalLength + rightEntropy * rightLength / totalLength);
            return gain;
        }

        public static SplittingQuestion[] GenerateSplittingQuestions(FieldEnum[] fields, int maximumEditDistance)
        {
            List<SplittingQuestion> splittingQuestions = new List<SplittingQuestion>();

            splittingQuestions.Add(new SplittingQuestion()
            {
                Field = FieldEnum.Address1,
                MatchType = MatchTypeEnum.SoftMatch,
                BothFieldValuesAreEmpty = false,
                OneFieldValueIsEmpty = false
            });

            splittingQuestions.Add(new SplittingQuestion()
            {
                Field = FieldEnum.DOB,
                MatchType = MatchTypeEnum.SoftMatch,
                BothFieldValuesAreEmpty = false,
                OneFieldValueIsEmpty = false
            });

            splittingQuestions.Add(new SplittingQuestion()
            {
                Field = FieldEnum.Gender,
                MatchType = MatchTypeEnum.IsFemale,
                BothFieldValuesAreEmpty = false,
                OneFieldValueIsEmpty = false
            });

            foreach (FieldEnum field in fields)
            {
                splittingQuestions.Add(new SplittingQuestion
                {
                    Field = field,
                    MatchType = MatchTypeEnum.EmptyMatch,
                    BothFieldValuesAreEmpty = true,
                    OneFieldValueIsEmpty = false,
                });

                splittingQuestions.Add(new SplittingQuestion
                {
                    Field = field,
                    MatchType = MatchTypeEnum.EmptyMatch,
                    BothFieldValuesAreEmpty = false,
                    OneFieldValueIsEmpty = true,
                });

                for (int editDistance = 0; editDistance < maximumEditDistance; editDistance++)
                {
                    splittingQuestions.Add(new SplittingQuestion
                    {
                        Field = field,
                        MatchType = MatchTypeEnum.EditDistance,
                        MaximumEditDistance = editDistance,
                    });
                }
            }

            return splittingQuestions.ToArray();
        }

        public static bool ComputeSplitDirection(SplittingQuestion question, RecordPair pair)
        {
            bool matches = false;

            string column1 = pair.Record1.Cache[(int)question.Field];
            string column2 = pair.Record2.Cache[(int)question.Field];

            switch (question.MatchType)
            {
                case MatchTypeEnum.EditDistance:
                    matches = MatchTypeMatcher.BasedOnEditDistance(question, column1, column2);
                    break;
                case MatchTypeEnum.EmptyMatch:
                    matches = MatchTypeMatcher.BasedOnEmptyFields(question, column1, column2);
                    break;
                case MatchTypeEnum.SoftMatch:
                    if (question.Field == FieldEnum.Address1)
                    {
                        matches = MatchTypeMatcher.BasedOnAddressSoftMatch(question, column1, column2);
                    }
                    else if (question.Field == FieldEnum.DOB)
                    {
                        matches = MatchTypeMatcher.BasedOnDateSoftMatch(question, column1, column2);
                    }
                    break;
                case MatchTypeEnum.IsFemale:
                    matches = MatchTypeMatcher.BasedOnIsFemale(question, column1, column2);
                    break;
                default:
                    throw new ArgumentException();
            }
            return matches;
        }

        private void RecurseAndPartition(DecisionTreeNode parentNode, SplittingQuestion[] splittingQuestions,
            RecordPair[] allPairs, int level, double subsamplingPercentage, double minGainToBreak)
        {
            Console.WriteLine($"Level {level}. {splittingQuestions.Length} splitting questions on {allPairs.Length} record pairs.");
            double currentEntropy = ComputeShannonEntropy(allPairs);

            // find the best splitting question. 
            double highestGain = 0.0;
            SplittingQuestion bestQuestion = null;
            int numberDone = 0;
            int displayLeft = Console.CursorLeft;
            int displayTop = Console.CursorTop;

            #region DEBUGLOOP
            //foreach (SplittingQuestion splittingQuestion in splittingQuestions)
            //{
            //    List<RecordPair> leftBucket = new List<RecordPair>();
            //    List<RecordPair> rightBucket = new List<RecordPair>();

            //    Random rand = new Random();

            //    int matchesInLeft = 0, noMatchesInLeft = 0,
            //        matchesInRight = 0, noMatchesInRight = 0;

            //    foreach (RecordPair pair in allPairs)
            //    {
            //        if (rand.NextDouble() <= subsamplingPercentage)
            //        {
            //            bool goLeft = ComputeSplitDirection(splittingQuestion, pair);

            //            if (goLeft)
            //            {
            //                if (pair.IsMatch)
            //                {
            //                    matchesInLeft++;
            //                }
            //                else
            //                {
            //                    noMatchesInLeft++;
            //                }
            //            }
            //            else
            //            {
            //                if (pair.IsMatch)
            //                {
            //                    matchesInRight++;
            //                }
            //                else
            //                {
            //                    noMatchesInRight++;
            //                }
            //            }
            //        }
            //    }

            //    double leftEntropy = ComputeShannonEntropy(matchesInLeft, noMatchesInLeft);
            //    double rightEntropy = ComputeShannonEntropy(matchesInRight, noMatchesInRight);

            //    double gain = ComputeGain(currentEntropy, leftEntropy, (noMatchesInLeft + matchesInLeft),
            //        rightEntropy, (matchesInRight + noMatchesInRight));

            //    lock (splittingQuestions)
            //    {
            //        if (gain > highestGain)
            //        {
            //            highestGain = gain;
            //            bestQuestion = splittingQuestion;
            //        }
            //    }

            //    lock (splittingQuestions)
            //    {
            //        numberDone++;
            //        Console.SetCursorPosition(displayLeft, displayTop);
            //        Console.WriteLine($"{(int)((numberDone / (splittingQuestions.Length * 1.0)) * 100)}%");
            //    }
            //}
            #endregion

            #region PARALLELLOOP
            //Console.WriteLine("Parallel mode...."); 
            //foreach (SplittingQuestion splittingQuestion in splittingQuestions)
            Parallel.ForEach(splittingQuestions, splittingQuestion =>
            {
                List<RecordPair> leftBucket = new List<RecordPair>();
                List<RecordPair> rightBucket = new List<RecordPair>();

                //Random rand = new Random();

                int matchesInLeft = 0, noMatchesInLeft = 0,
                    matchesInRight = 0, noMatchesInRight = 0;

                int pairNumber = 0;
                foreach (RecordPair pair in allPairs)
                {
                    //if(pairNumber%10000==0)
                    //    Console.WriteLine($"{pairNumber} of {allPairs.Length}");
                    pairNumber++;
                    //if (rand.NextDouble() <= subsamplingPercentage)
                    {
                        bool goLeft = ComputeSplitDirection(splittingQuestion, pair);

                        if (goLeft)
                        {
                            if (pair.IsMatch)
                            {
                                matchesInLeft++;
                            }
                            else
                            {
                                noMatchesInLeft++;
                            }
                        }
                        else
                        {
                            if (pair.IsMatch)
                            {
                                matchesInRight++;
                            }
                            else
                            {
                                noMatchesInRight++;
                            }
                        }
                    }
                }

                double leftEntropy = ComputeShannonEntropy(matchesInLeft, noMatchesInLeft);
                double rightEntropy = ComputeShannonEntropy(matchesInRight, noMatchesInRight);

                double gain = ComputeGain(currentEntropy, leftEntropy, (noMatchesInLeft + matchesInLeft),
                    rightEntropy, (matchesInRight + noMatchesInRight));

                lock (splittingQuestions)
                {
                    if (gain > highestGain)
                    {
                        highestGain = gain;
                        bestQuestion = splittingQuestion;
                    }
                }
                lock (splittingQuestions)
                {
                    numberDone++;
                    //Console.SetCursorPosition(displayLeft, displayTop);
                    //Console.WriteLine($"{(int)((numberDone / (splittingQuestions.Length * 1.0)) * 100)}%");
                }
            });
            #endregion

            if (highestGain <= minGainToBreak)
            {

                parentNode.IsLeaf = true;
                int matchCount = allPairs.Count(n => n.IsMatch);
                int noMatchCount = allPairs.Count(n => !n.IsMatch);

                if (matchCount > noMatchCount)
                {
                    parentNode.IsMatch = true;
                }
                else
                {
                    parentNode.IsMatch = false;
                }

                if (matchCount > 0 && noMatchCount > 0)
                {
                    foreach (RecordPair noHomoPair in allPairs)
                    {
                        Console.WriteLine(noHomoPair);
                        bool goLeft = ComputeSplitDirection(new SplittingQuestion
                        {
                            BothFieldValuesAreEmpty = true,
                            Field = FieldEnum.SSN,
                            MatchType = MatchTypeEnum.EmptyMatch
                        }, noHomoPair);
                        Console.WriteLine("\t\tGo left: " + goLeft.ToString());
                    }

                    Console.WriteLine("\tNOHOMO!");
                    StringBuilder sb = new StringBuilder();
                    foreach (RecordPair pair in allPairs)
                    {
                        sb.AppendLine(pair.ToString());
                    }

                    File.AppendAllText($"C:/users/brush/desktop/nohomo/{Guid.NewGuid().ToString().Replace("-", "")}.csv",
                        sb.ToString());
                }

                Console.WriteLine("\tGain limit met. Anything reaching this leaf will be labeled as " + (parentNode.IsMatch ? "match." : "no match"));
            }
            else
            {
                Console.WriteLine($"\tBest question at this level is {bestQuestion}");

                List<RecordPair> bestLeftBucket = new List<RecordPair>(), bestRightBucket = new List<RecordPair>();
                Parallel.ForEach(allPairs, pair =>
                {
                    bool goLeft = ComputeSplitDirection(bestQuestion, pair);

                    if (goLeft)
                    {
                        lock (bestLeftBucket)
                        {
                            bestLeftBucket.Add(pair);
                        }
                    }
                    else
                    {
                        lock (bestRightBucket)
                        {
                            bestRightBucket.Add(pair);
                        }
                    }
                });

                parentNode.Question = bestQuestion;
                parentNode.LeftBranch = new DecisionTreeNode();
                parentNode.RightBranch = new DecisionTreeNode();

                RecurseAndPartition(parentNode.LeftBranch, splittingQuestions, bestLeftBucket.ToArray(),
                    level + 1, subsamplingPercentage, minGainToBreak);
                RecurseAndPartition(parentNode.RightBranch, splittingQuestions, bestRightBucket.ToArray(),
                    level + 1, subsamplingPercentage, minGainToBreak);
            }
        }

        private static bool RecurseAndCheckIsMatch(DecisionTreeNode parentNode, RecordPair pair, bool debug)
        {
            if (parentNode.IsLeaf)
            {
                return parentNode.IsMatch;
            }
            else
            {
                bool goesLeft = ComputeSplitDirection(parentNode.Question, pair);

                if (debug)
                {
                    Console.WriteLine($"Question: {parentNode.Question} Answer: {goesLeft}.");
                }

                if (goesLeft)
                {
                    return RecurseAndCheckIsMatch(parentNode.LeftBranch, pair, debug);
                }
                else
                {
                    return RecurseAndCheckIsMatch(parentNode.RightBranch, pair, debug);
                }
            }
        }

        public static bool IsMatch(RecordPair pair, DecisionTree[] forest, bool debug)
        {
            int positives = 0;
            foreach (DecisionTree tree in forest)
            {
                if (RecurseAndCheckIsMatch(tree.Root, pair, debug))
                    positives++;
            }

            return (positives / (forest.Length) * 1.0) > .5;
        }

        private static PropertyInfo[] _propInfoCache;

        public DecisionTree Train(List<RecordPair> trainingData,
            SplittingQuestion[] splittingQuestions, double subsamplingPercentage,
            double minGain, int maximumEditDistance)
        {
            //PropertyInfo property = recordType.GetProperty(question.Field.ToString());
            //TODO: do this better. major hack. 
            Console.WriteLine("Learning...");

            DecisionTree tree = new DecisionTree();
            tree.Root = new DecisionTreeNode();

            RecurseAndPartition(tree.Root, splittingQuestions, trainingData.ToArray(),
                0, subsamplingPercentage, minGain);

            return tree;
        }
    }
}
