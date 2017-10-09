using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class ProbabilityEngine
    {
        public static ConditionedDistribution ComputeConditionedDistribution(List<List<int>> knownMatches, RowMatchObject fieldMatches, int totalRowCount)
        {
            int largestDistance = fieldMatches.Matches.MatchArray.Max(l => l.Max(e => e.Distance));

            var distanceCounts = DistanceCounts(fieldMatches, totalRowCount, largestDistance);
            var distanceCountsGivenMatched = DistanceCountsGivenMatched(knownMatches, fieldMatches, largestDistance);

            long[] distanceCountsGivenUnmatched = new long[largestDistance + 2];
            for(int i = 0; i < largestDistance + 2; i++)
            {
                distanceCountsGivenUnmatched[i] = distanceCounts[i] - distanceCountsGivenMatched[i];
            }

            ConditionedDistribution toReturn = new ConditionedDistribution();
            toReturn.GivenMatch = CountsToDistribution(distanceCountsGivenMatched);
            toReturn.GivenNoMatch = CountsToDistribution(distanceCountsGivenUnmatched);


            return toReturn;
        }

        public static long[] DistanceCounts(RowMatchObject fieldMatches, long totalRowCount, int largestDistance)
        {
            long[] toReturn = new long[largestDistance + 3];

            var matchArray = fieldMatches.Matches.MatchArray;
            //Handle not blank and not over the max distance threshold
            for (int i = 0; i < matchArray.Length; i++)
            {
                var l = matchArray[i];
                foreach(var p in l)
                {
                    int j = p.Index;
                    if (i != j)
                        toReturn[p.Distance] += ((long)(fieldMatches.IndexToEids[i].Count)) * ((long)(fieldMatches.IndexToEids[j].Count));
                    else
                        toReturn[p.Distance] += ((long)(fieldMatches.IndexToEids[i].Count)) * ((long)(fieldMatches.IndexToEids[j].Count - 1));
                }
            }

            
            long notBlank = fieldMatches.IndexToEids.Sum(l => l.Count);

            //Handle over the max distance threshold;
            toReturn[largestDistance + 1] = notBlank * (notBlank - 1) - toReturn.Sum();

            //Divide by two
            for (int i = 0; i < toReturn.Length; i++)
                toReturn[i] /= 2;

            return toReturn;
        }


        public static long[] DistanceCountsGivenMatched(List<List<int>> knownMatches, RowMatchObject fieldMatches, int largestDistance)
        {
            long[] toReturn = new long[largestDistance + 2];

            var fieldMatchArray = fieldMatches.Matches.MatchArray;
            var eidToIndex = fieldMatches.EidToIndex;

            foreach (var set in knownMatches)
            {
                for (int i = 0; i < set.Count; i++)
                    for (int j = i + 1; j < set.Count; j++)
                    {
                        int ii = fieldMatches.EidToIndex[set[i]];
                        int ij = fieldMatches.EidToIndex[set[j]];

                        if (ii == -1 || ij == -1)
                        {
                            continue;
                        }

                        bool pairFound = false;
                        IndexDistancePair foundPair = default(IndexDistancePair);

                        foreach (var pair in fieldMatchArray[ii])
                        {
                            if (pair.Index == ij)
                            {
                                pairFound = true;
                                foundPair = pair;
                                break;
                            }
                        }

                        if (pairFound)
                        {
                            toReturn[foundPair.Distance]++;
                        }
                        else
                        {
                            //In this case, we give the maximum distance;
                            toReturn[largestDistance + 1]++;
                        }
                    }
            }

            return toReturn;
        }

        /// <summary>
        /// Scale so that the probabilities sum to 1
        /// </summary>
        /// <param name="d"></param>
        private static ProbabilityDistribution CountsToDistribution(long[] counts)
        {
            ProbabilityDistribution toReturn = new ProbabilityDistribution();
            toReturn.Probability = new double[counts.Length];

            double total = counts.Sum();
            for(int i = 0; i < counts.Length; i++)
            {
                toReturn.Probability[i] = ((double)counts[i])/((double)total);
            }

            return toReturn;
        }

        public static ConditionedDistribution SeparateGenderCalculation(Row[] allData, List<List<int>> matches)
        {
            ConditionedDistribution toReturn = new ConditionedDistribution();

            var eidToRow = new Dictionary<int, Row>();
            foreach(var row in allData)
            {
                eidToRow[row.EnterpriseID] = row;
            }


            long[] givenMatch = new long[3];
            foreach(var set in matches)
            {
                for (int i = 0; i < set.Count; i++)
                    for (int j = i + 1; j < set.Count; j++)
                    {
                        var ri = eidToRow[set[i]];
                        var rj = eidToRow[set[j]];

                        if (ri.GENDER == "" || rj.GENDER == "")
                            givenMatch[0]++;
                        else if (ri.GENDER == rj.GENDER)
                            givenMatch[1]++;
                        else
                            givenMatch[2]++;
                    }
            }

            long blank = allData.Count(r => r.GENDER == "");
            long male = allData.Count(r => r.GENDER == "M");
            long female = allData.Count(r => r.GENDER == "F");

            long[] givenNoMatch = new long[3];
            givenNoMatch[0] = blank * (blank - 1)/2 + blank * (male + female) - givenMatch[0];
            givenNoMatch[1] = male * (male - 1)/2 + female * (female - 1)/2 - givenMatch[1];
            givenNoMatch[2] = male * female - givenMatch[2];

            toReturn.GivenMatch = CountsToDistribution(givenMatch);
            toReturn.GivenNoMatch = CountsToDistribution(givenNoMatch);

            return toReturn;
        }

        public static double ProbabilityofMatch(Row a, Row b, ProfileDistribution distribution)
        {
            var profile = ProfileEngine.CreateProfile(a, b);
            return ProbabilityofMatch(profile, distribution);
        }

        public static double ProbabilityofMatch(int[] profile, ProfileDistribution distribution)
        {
            double product = distribution.PMatch;
            var factors = distribution.Factors;

            for (int i = 0; i < profile.Length; i++)
            {
                if (profile[i] < 0) //Skip over fields where we can't evaluate the strength of the match
                    continue;

                product *= factors[i].GivenMatch.Probability[profile[i]]/factors[i].GivenNoMatch.Probability[profile[i]];
            }

            return product / (1d + product);
        }
    }


    [Serializable]
    public class ProbabilityDistribution
    {
        public double[] Probability;
    }

    [Serializable]
    public class ConditionedDistribution
    {
        public ProbabilityDistribution GivenMatch;
        public ProbabilityDistribution GivenNoMatch;
    }
}
