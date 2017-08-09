using System;
using System.Collections.Generic;
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
            List<RecordPair> pairs, int level, double subsamplingPercentage, double minGainToBreak)
        {
            Console.WriteLine($"Level {level}. {splittingQuestions.Length} splitting questions on {pairs.Count} record pairs.");
            double currentEntropy = ComputeShannonEntropy(pairs);

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

                foreach (RecordPair pair in pairs)
                {
                    if (rand.NextDouble() < subsamplingPercentage)
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
                int matchCount = pairs.Count(n => n.IsMatch);
                int noMatchCount = pairs.Count(n => !n.IsMatch);

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

                List<RecordPair> bestLeftBucket = new List<RecordPair>(), bestRightBucket = new List<RecordPair>();
                foreach (RecordPair pair in pairs)
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

                parentNode.Question = bestQuestion;
                parentNode.LeftBranch = new DecisionTreeNode();
                parentNode.RightBranch = new DecisionTreeNode();

                RecurseAndPartition(parentNode.LeftBranch, splittingQuestions, bestLeftBucket, level + 1, subsamplingPercentage, minGainToBreak);
                RecurseAndPartition(parentNode.RightBranch, splittingQuestions, bestRightBucket, level + 1, subsamplingPercentage, minGainToBreak);
            }
        }

        public DecisionTree Train(List<RecordPair> trainingData)
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

            SplittingQuestion[] splittingQuestions = GenerateSplittingQuestions(fieldsOnWhichToTrain, 3);
            RecurseAndPartition(tree.Root, splittingQuestions, trainingData, 0, .1, 0);

            return tree;
        }
    }
}
