using DecisionTreeLearner.Attributes;
using DecisionTreeLearner.DataTypes;
using DecisionTreeLearner.NLP;
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

        public static SplittingQuestion[] GenerateSplittingQuestions(int maximumEditDistance)
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

            splittingQuestions.Add(new SplittingQuestion()
            {
                Field = FieldEnum.Address1,
                MatchType = MatchTypeEnum.IsHomeless,
                BothFieldValuesAreEmpty = false,
                OneFieldValueIsEmpty = false
            });


            splittingQuestions.Add(new SplittingQuestion()
            {
                Field = FieldEnum.Address1,
                MatchType = MatchTypeEnum.LivesInMassResidence,
                BothFieldValuesAreEmpty = false,
                OneFieldValueIsEmpty = false
            });

            int[] mrnDistances = new int[]
            {
                100,
                500,
                1000,
                10000,
                90000,
            };

            for (int c = 0; c < mrnDistances.Length; c++)
            {
                splittingQuestions.Add(new SplittingQuestion
                {
                    Field = FieldEnum.MRN,
                    MatchType = MatchTypeEnum.MRNDistance,
                    MRNMaxDistance = mrnDistances[c],
                });
            }

            Type fieldEnumType = typeof(FieldEnum);
            foreach (FieldEnum field in fieldEnumType.GetEnumValues().OfType<FieldEnum>())
            {
                if (typeof(FieldEnum).GetMember(field.ToString())[0].GetCustomAttributes().Any(n => n.GetType() == typeof(EditDistanceCapableAttribute)))
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
            }

            Console.WriteLine("I will be asking the following questions:");
            foreach (SplittingQuestion question in splittingQuestions)
            {
                Console.WriteLine("\t" + question);
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
                case MatchTypeEnum.MRNDistance:
                    matches = MatchTypeMatcher.BasedOnMRNDistance(question, pair);
                    break;
                case MatchTypeEnum.LivesInMassResidence:
                    matches = MatchTypeMatcher.BasedOnLivesInMassResidence(question, pair);
                    break;
                case MatchTypeEnum.IsHomeless:
                    matches = MatchTypeMatcher.BasedOnIsHomeless(question, pair);
                    break;
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
                    matches = MatchTypeMatcher.BasedOnIsFemale(question, pair);
                    break;
                default:
                    throw new ArgumentException();
            }
            return matches;
        }

        private void RecurseAndPartition(DecisionTreeNode parentNode, SplittingQuestion[] splittingQuestions,
            RecordPair[] allPairs, int level, double subsamplingPercentage, double minGainToBreak,
            Stack<Tuple<SplittingQuestion, bool>> splittingQuestionsThatGotUsHere)
        {
            Console.WriteLine($"Level {level}. {splittingQuestions.Length} splitting questions on {allPairs.Length} record pairs.");

            // find the best splitting question. 
            SplittingQuestion bestQuestion = null;
            int numberDone = 0;
            int displayLeft = Console.CursorLeft;
            int displayTop = Console.CursorTop;

            bool reachedLeafNode = false;

            // is this precomputed? if not, then we need to compute it. 
            if (parentNode.Question == null)
            {
                double highestGain = 0.0;
                double currentEntropy = ComputeShannonEntropy(allPairs);

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

                reachedLeafNode = highestGain <= minGainToBreak;

                if (reachedLeafNode)
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
                }

                Console.WriteLine("\tGain limit met. Anything reaching this leaf will be labeled as " + (parentNode.IsMatch ? "match." : "no match"));
            }
            else
            {
                // otherwise it's precomputed, just take the current question as the "best question"
                bestQuestion = parentNode.Question;
                reachedLeafNode = parentNode.IsLeaf;

                Console.WriteLine("\tPrecomputed. Anything reaching this leaf will be labeled as " + (parentNode.IsMatch ? "match." : "no match"));
            }

            //if (reachedLeafNode)
            //{


            //    StringBuilder sb = new StringBuilder(1024);
            //    sb.AppendLine($"Level {level}, IsMatch {parentNode.IsMatch}");
            //    sb.AppendLine("Questions:");
            //    foreach (Tuple<SplittingQuestion, bool> questionAnswer in splittingQuestionsThatGotUsHere)
            //    {
            //        sb.AppendLine($"\t{questionAnswer.Item1}:{questionAnswer.Item2}");
            //    }
            //    sb.AppendLine($"match: {matchCount}, nomatch: {noMatchCount}");
            //    File.WriteAllText(
            //        $"c:/users/brush/desktop/treeresults/{Guid.NewGuid().ToString().Replace("-", "")}",
            //        sb.ToString());

            //    Console.WriteLine("\tGain limit met. Anything reaching this leaf will be labeled as " + (parentNode.IsMatch ? "match." : "no match"));
            //}
            if (!reachedLeafNode)
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
                if (parentNode.LeftBranch == null)
                {
                    parentNode.LeftBranch = new DecisionTreeNode();
                }

                if (parentNode.RightBranch == null)
                {
                    parentNode.RightBranch = new DecisionTreeNode();
                }

                splittingQuestionsThatGotUsHere.Push(new Tuple<SplittingQuestion, bool>(bestQuestion, true));
                RecurseAndPartition(parentNode.LeftBranch, splittingQuestions, bestLeftBucket.ToArray(),
                    level + 1, subsamplingPercentage, minGainToBreak, splittingQuestionsThatGotUsHere);
                splittingQuestionsThatGotUsHere.Pop();

                splittingQuestionsThatGotUsHere.Push(new Tuple<SplittingQuestion, bool>(bestQuestion, false));
                RecurseAndPartition(parentNode.RightBranch, splittingQuestions, bestRightBucket.ToArray(),
                    level + 1, subsamplingPercentage, minGainToBreak, splittingQuestionsThatGotUsHere);
                splittingQuestionsThatGotUsHere.Pop();
            }
        }

        private static bool RecurseAndCheckIsMatch(DecisionTreeNode parentNode, RecordPair pair, TreeLogger logger)
        {
            if (parentNode.IsLeaf)
            {
                if (logger != null)
                {
                    logger.FinalResultIsMatch = parentNode.IsMatch;
                    Console.WriteLine($"Is Match {parentNode.IsMatch}");
                }

                return parentNode.IsMatch;
            }
            else
            {
                bool goesLeft = ComputeSplitDirection(parentNode.Question, pair);

                if (logger != null)
                {
                    Console.WriteLine($"Question: {parentNode.Question} Answer: {goesLeft}.");
                    logger.SplittingQuestionsToTheBottom.Add(new Tuple<SplittingQuestion, bool>(parentNode.Question, goesLeft));
                }

                if (goesLeft)
                {
                    return RecurseAndCheckIsMatch(parentNode.LeftBranch, pair, logger);
                }
                else
                {
                    return RecurseAndCheckIsMatch(parentNode.RightBranch, pair, logger);
                }
            }
        }

        public static bool ReplayDecision(RecordPair pair, List<Tuple<SplittingQuestion, bool>> rules)
        {
            bool good = true;

            foreach (Tuple<SplittingQuestion, bool> rule in rules)
            {
                bool response = ComputeSplitDirection(rule.Item1, pair);

                if (response != rule.Item2)
                {
                    good = false;
                    break;
                }
            }

            return good;
        }

        public static bool IsMatch(RecordPair pair, DecisionTree[] forest, TreeLogger logger)
        {
            int positives = 0;
            foreach (DecisionTree tree in forest)
            {
                if (RecurseAndCheckIsMatch(tree.Root, pair, logger))
                    positives++;
            }

            return (positives / (forest.Length) * 1.0) > .5;
        }

        private static void PrefillTree(DecisionTreeNode parent, List<Tuple<SplittingQuestion, bool>> preComputedBranches, int level)
        {
            if (level < preComputedBranches.Count)
            {
                Tuple<SplittingQuestion, bool> preComputedBranch = preComputedBranches[level];

                // are we at the end? 
                if (preComputedBranch.Item1 == null)
                {
                    // we are at the end. 
                    parent.IsLeaf = true;
                    parent.IsMatch = preComputedBranch.Item2;
                }
                else
                {
                    // we aren't at the end. 

                    parent.Question = preComputedBranch.Item1;

                    if (preComputedBranch.Item2) // go left. 
                    {
                        parent.LeftBranch = new DecisionTreeNode();
                        PrefillTree(parent.LeftBranch, preComputedBranches, level + 1);
                    }
                    else // go right. 
                    {
                        parent.RightBranch = new DecisionTreeNode();
                        PrefillTree(parent.RightBranch, preComputedBranches, level + 1);
                    }
                }
            }
        }

        private static PropertyInfo[] _propInfoCache;

        public DecisionTree Train(List<RecordPair> trainingData,
            SplittingQuestion[] splittingQuestions, double subsamplingPercentage,
            double minGain, List<Tuple<SplittingQuestion, bool>> preComputedBranches)
        {
            //PropertyInfo property = recordType.GetProperty(question.Field.ToString());
            //TODO: do this better. major hack. 
            Console.WriteLine("Learning...");

            DecisionTree tree = new DecisionTree();
            tree.Root = new DecisionTreeNode();

            if (preComputedBranches != null)
            {
                PrefillTree(tree.Root, preComputedBranches, 0);
            }

            RecurseAndPartition(tree.Root, splittingQuestions, trainingData.ToArray(),
                0, subsamplingPercentage, minGain, new Stack<Tuple<SplittingQuestion, bool>>());

            return tree;
        }
    }
}
