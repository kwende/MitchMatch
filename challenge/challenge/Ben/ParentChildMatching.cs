using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class ParentChildMatching
    {
        private List<row[]> _clusters;

        public ParentChildMatching()
        {
            _clusters = new List<row[]>();
        }

        public void AddMatchClusters<T>(List<IGrouping<T, row>> addedClusters)
        {
            foreach (IGrouping<T, row> cluster in addedClusters)
            {
                _clusters.Add(cluster.ToArray());
            }
        }

        public void AddMatchClusters(List<List<row>> addedClusters)
        {
            foreach (List<row> cluster in addedClusters)
            {
                _clusters.Add(cluster.ToArray());
            }
        }

        public List<Tuple<row, row>> FindParentChildrenInExistingMatches()
        {
            const int MinimumNumberOfYearsForParentChild = 15;
            const int MaximumNumberOfYearsForParentChild = 45;

            List<Tuple<row, row>> ret = new List<Tuple<row, row>>();

            foreach (row[] cluster in _clusters)
            {
                List<row> toSearch = new List<row>(cluster);
                while (toSearch.Count > 0)
                {
                    row row1 = toSearch[0];

                    for (int c = 1; c < toSearch.Count; c++)
                    {
                        row row2 = toSearch[c];

                        double dobDifferenceInYears = System.Math.Abs(((row1.DOB - row2.DOB).TotalDays)) / 364.0;

                        if (row1.FIRST.ToLower() == row2.FIRST.ToLower() &&
                            row1.LAST.ToLower() == row2.LAST.ToLower() &&
                            row1.ADDRESS1.ToLower() == row2.ADDRESS1.ToLower() &&
                            dobDifferenceInYears > MinimumNumberOfYearsForParentChild &&
                            dobDifferenceInYears < MaximumNumberOfYearsForParentChild)
                        {
                            ret.Add(new Tuple<row, row>(row1, row2));
                        }
                    }

                    toSearch.Remove(row1);
                }
            }

            return ret;
        }

        public static List<BetterMatch> RandomlyLookForBetterRecordsForPairs(row[] dataSet, List<Tuple<row, row>> parentChildMatches, double percentSampling, TransitiveClosure tc)
        {
            List<BetterMatch> betterMatches = new List<BetterMatch>();

            Random rand = new Random();
            int count = 0;
            //foreach (Tuple<row, row> parentChildMatch in parentChildMatches)
            Parallel.ForEach(parentChildMatches, row =>
            {
                if (rand.NextDouble() <= percentSampling)
                {
                    row row1 = row.Item1;
                    row row2 = row.Item2;

                    List<row> bestMatchesForRow1 = EditDistance.FindClosestMatchesForRowInEntireDataSet(row1, dataSet, tc);
                    List<row> bestMatchesForRow2 = EditDistance.FindClosestMatchesForRowInEntireDataSet(row2, dataSet, tc);

                    if (bestMatchesForRow1.Count > 0 && !bestMatchesForRow1.Contains(row2))
                    {
                        BetterMatch betterMatch = new BetterMatch
                        {
                            TheRow = row1,
                            TheOriginalMatch = row2,
                            ABetterMatch = bestMatchesForRow1[0],
                            BetterMatchDistance = EditDistance.ComputeDistanceForRecordPair(row1, bestMatchesForRow1[0]),
                            OriginalMatchDistance = EditDistance.ComputeDistanceForRecordPair(row1, row2),
                        };
                        lock(betterMatches)
                        {
                            betterMatches.Add(betterMatch);
                        }
                    }

                    if (bestMatchesForRow2.Count > 0 && !bestMatchesForRow2.Contains(row1))
                    {
                        BetterMatch betterMatch = new BetterMatch
                        {
                            TheRow = row2,
                            TheOriginalMatch = row1,
                            ABetterMatch = bestMatchesForRow2[0],
                            BetterMatchDistance = EditDistance.ComputeDistanceForRecordPair(row2, bestMatchesForRow2[0]),
                            OriginalMatchDistance = EditDistance.ComputeDistanceForRecordPair(row1, row2)
                        };
                        lock(betterMatches)
                        {
                            betterMatches.Add(betterMatch);
                        }
                    }
                }

                lock(rand)
                {
                    count++; 
                    Console.WriteLine($"{count}/{parentChildMatches.Count}");
                }
            }); 
            return betterMatches;
        }
    }
}
