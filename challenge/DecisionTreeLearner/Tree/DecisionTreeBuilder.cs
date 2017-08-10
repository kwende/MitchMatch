using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    public class DecisionTreeBuilder
    {
        private static double ComputeShannonEntropy(List<RecordPair> pairs)
        {
            double entropy = 0.0;

            int numberInMatchColumn = 0;
            int numberInNoMatchColum = 0;

            for (int c = 0; c < pairs.Count; c++)
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

            double totalLength = pairs.Count;

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

            double ratio = numberInMatchColumn / totalLengthAsDouble;
            entropy = -(ratio * System.Math.Log(ratio, 2));

            ratio = numberInNoMatchColumn / totalLengthAsDouble;
            entropy += -(ratio * System.Math.Log(ratio, 2));

            return entropy;
        }

        private static double ComputeGain(double comparisonEntropy, double leftEntropy, int leftLength, double rightEntropy, int rightLength)
        {
            double totalLength = leftLength + rightLength;

            double gain = comparisonEntropy - (leftEntropy * leftLength / totalLength + rightEntropy * rightLength / totalLength);
            return gain;
        }

        private static double ComputeGain(double comparisonEntropy, List<RecordPair> left, List<RecordPair> right)
        {
            double leftEntropy = ComputeShannonEntropy(left);
            double rightEntropy = ComputeShannonEntropy(right);

            double leftLength = left.Count;
            double rightLength = right.Count;
            double totalLength = leftLength + rightLength;

            double gain = comparisonEntropy - (leftEntropy * leftLength / totalLength + rightEntropy * rightLength / totalLength);
            return gain;
        }

        private static SplittingQuestion[] GenerateSplittingQuestions(FieldEnum[] fields, int maximumEditDistance)
        {
            List<SplittingQuestion> splittingQuestions = new List<SplittingQuestion>();

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

        private static bool ComputeSplitDirection(SplittingQuestion question, RecordPair pair)
        {
            bool matches = false;

            string column1 = pair.Record1.Parts[(int)question.Field];
            string column2 = pair.Record2.Parts[(int)question.Field];

            switch (question.MatchType)
            {
                case MatchTypeEnum.EditDistance:
                    {
                        int editDistance = NLP.EditDistance.Compute(column1, column2);

                        if (editDistance <= question.MaximumEditDistance)
                        {
                            matches = true;
                        }
                    }
                    break;
                case MatchTypeEnum.EmptyMatch:
                    {
                        if (question.OneFieldValueIsEmpty)
                        {
                            if (string.IsNullOrEmpty(column1))
                            {
                                matches = !string.IsNullOrEmpty(column2);
                            }
                            else
                            {
                                matches = string.IsNullOrEmpty(column2);
                            }
                        }
                        else if (question.BothFieldValuesAreEmpty)
                        {
                            matches = string.IsNullOrEmpty(column1) && string.IsNullOrEmpty(column2);
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
            return matches;
        }

        private void RecurseAndPartition(DecisionTreeNode parentNode, SplittingQuestion[] splittingQuestions,
            List<RecordPair> allPairs, int level, double subsamplingPercentage, double minGainToBreak)
        {
            Console.WriteLine($"Level {level}. {splittingQuestions.Length} splitting questions on {allPairs.Count} record pairs.");
            double currentEntropy = ComputeShannonEntropy(allPairs);

            // find the best splitting question. 
            double highestGain = 0.0;
            SplittingQuestion bestQuestion = null;
            int numberDone = 0;
            int displayLeft = Console.CursorLeft;
            int displayTop = Console.CursorTop;
            Parallel.ForEach(splittingQuestions, splittingQuestion =>
            {
                lock (splittingQuestions)
                {
                    numberDone++;
                    Console.SetCursorPosition(displayLeft, displayTop);
                    Console.WriteLine($"{(int)((numberDone / (splittingQuestions.Length * 1.0)) * 100)}%");
                }

                List<RecordPair> leftBucket = new List<RecordPair>();
                List<RecordPair> rightBucket = new List<RecordPair>();

                Random rand = new Random();

                int matchesInLeft = 0, noMatchesInLeft = 0,
                    matchesInRight = 0, noMatchesInRight = 0;

                foreach (RecordPair pair in allPairs)
                {
                    if (rand.NextDouble() <= subsamplingPercentage)
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
            });

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

                Console.WriteLine("Gain limit met. Anything reaching this leaf will " + (parentNode.IsMatch ? " match." : " not match"));
            }
            else
            {
                Console.WriteLine($"Best question at this level is {bestQuestion}");

                Console.Write("Copying data..."); 
                List<RecordPair> bestLeftBucket = new List<RecordPair>(), bestRightBucket = new List<RecordPair>();
                foreach (RecordPair pair in allPairs)
                {
                    bool goLeft = ComputeSplitDirection(bestQuestion, pair);

                    if (goLeft)
                    {
                        bestLeftBucket.Add(pair);
                    }
                    else
                    {
                        bestRightBucket.Add(pair);
                    }
                }
                Console.WriteLine("...done"); 

                parentNode.Question = bestQuestion;
                parentNode.LeftBranch = new DecisionTreeNode();
                parentNode.RightBranch = new DecisionTreeNode();

                RecurseAndPartition(parentNode.LeftBranch, splittingQuestions, bestLeftBucket, 
                    level + 1, subsamplingPercentage, minGainToBreak);
                bestLeftBucket = null;
                RecurseAndPartition(parentNode.RightBranch, splittingQuestions, bestRightBucket, 
                    level + 1, subsamplingPercentage, minGainToBreak);
                bestRightBucket = null;
            }
        }

        private static bool RecurseAndCheckIsMatch(DecisionTreeNode parentNode, RecordPair pair)
        {
            if (parentNode.IsLeaf)
            {
                return parentNode.IsMatch;
            }
            else
            {
                bool goesLeft = ComputeSplitDirection(parentNode.Question, pair);

                if (goesLeft)
                {
                    return RecurseAndCheckIsMatch(parentNode.LeftBranch, pair);
                }
                else
                {
                    return RecurseAndCheckIsMatch(parentNode.RightBranch, pair);
                }
            }
        }

        public static bool IsMatch(RecordPair pair, DecisionTree[] forest)
        {
            int positives = 0; 
            foreach(DecisionTree tree in forest)
            {
                if (RecurseAndCheckIsMatch(tree.Root, pair))
                    positives++; 
            }

            return (positives / (forest.Length) * 1.0) > .5; 
        }

        public DecisionTree Train(List<RecordPair> trainingData, double subsamplingPercentage, 
            double minGain, int maximumEditDistance)
        {
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

            Console.WriteLine("Learning...");

            DecisionTree tree = new DecisionTree();
            tree.Root = new DecisionTreeNode();

            SplittingQuestion[] splittingQuestions = GenerateSplittingQuestions(fieldsOnWhichToTrain, maximumEditDistance);
            RecurseAndPartition(tree.Root, splittingQuestions, trainingData, 0, subsamplingPercentage, minGain);

            return tree;
        }
    }
}
