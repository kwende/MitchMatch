using challenge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class EditDistance
    {
        public static List<row> FindClosestMatchesForRowInEntireDataSet(row input, row[] dataset, TransitiveClosure tc)
        {
            List<row> bestMatches = new List<row>();
            row[] closedSetForInput = tc.FindClosedSetForRow(input);

            // get the closest match value for this particular row 
            // to someone in his closed set. 
            double bestMatchMetric = 1.0;
            foreach (row row in closedSetForInput)
            {
                if (row.EnterpriseID != input.EnterpriseID)
                {
                    double distance = ComputeDistanceForRecordPair(input, row);
                    if (distance < bestMatchMetric)
                    {
                        bestMatchMetric = distance;
                    }
                }
            }

            // now look everywhere. 
            foreach (row row in dataset)
            {
                // don't match aginst self, and don't match against
                // what's already in the closed set. 
                if (row.EnterpriseID != input.EnterpriseID &&
                    !closedSetForInput.Any(n => n.EnterpriseID == row.EnterpriseID))
                {
                    double distance = ComputeDistanceForRecordPair(input, row);

                    if (distance == bestMatchMetric)
                    {
                        bestMatches.Add(row);
                    }
                    else if (distance < bestMatchMetric)
                    {
                        bestMatches.Clear();
                        bestMatches.Add(row);
                        bestMatchMetric = distance;
                    }
                }
            }

            return bestMatches;
        }

        public static void WriteWorstToDisk<T>(List<IGrouping<T, row>> groups, row[] data, string outputFile, double threshold)
        {
            List<Tuple<double, int, int>> tuples = challenge.Ben.ErrorScrubber.ReturnMaxErrorForMatchedGroups<T>(groups);

            using (StreamWriter sw = File.CreateText(outputFile))
            {
                foreach (Tuple<double, int, int> tuple in tuples)
                {
                    if (tuple.Item1 > threshold)
                    {
                        sw.WriteLine(data.Where(n => n.EnterpriseID == tuple.Item2).First().ToString());
                        sw.WriteLine(data.Where(n => n.EnterpriseID == tuple.Item3).First().ToString());
                        sw.WriteLine();
                    }
                }
            }
        }

        public static void GenerateScatterPlot<T>(List<IGrouping<T, row>> groups, row[] data, string outputFile)
        {
            List<Tuple<double, int, int>> tuples = challenge.Ben.ErrorScrubber.ReturnMaxErrorForMatchedGroups<T>(groups);

            using (StreamWriter sw = File.CreateText(outputFile))
            {
                foreach (Tuple<double, int, int> tuple in tuples)
                {
                    sw.WriteLine($"{tuple.Item2},{tuple.Item3},{tuple.Item1}");
                }
            }
        }

        public static double ComputeDistanceForRecordPair(row row1, row row2)
        {
            List<double> distances = new List<double>();
            distances.Add(ComputeNormalized(row1.FIRST, row2.FIRST));

            double lastNameDistance = ComputeNormalized(row1.LAST, row2.LAST);
            if (row1.GENDER.ToLower() == row2.GENDER.ToLower() && row1.GENDER.ToLower() == "f")
            {
                // a name change for a woman isn't as worrisome. 
                lastNameDistance *= .75;
            }

            distances.Add(lastNameDistance);
            if (row1.SSN != 0 && row2.SSN != 0)
            {
                distances.Add(ComputeNormalized(row1.SSN.ToString(), row2.SSN.ToString()));
            }
            else
            {
                distances.Add(.5);
            }

            distances.Add(ComputeNormalized(row1.DOB.ToShortDateString(), row2.DOB.ToShortDateString()));
            distances.Add(ComputeNormalized(row1.ZIP.ToString(), row2.ZIP.ToString()));

            if (row1.ADDRESS1 != "" && row1.ADDRESS2 != "")
            {
                distances.Add(ComputeNormalized(row1.ADDRESS1, row2.ADDRESS1));
            }
            else
            {
                distances.Add(.5);
            }

            if (row1.GENDER != "" && row2.GENDER != "")
            {
                distances.Add(ComputeNormalized(row1.GENDER, row2.GENDER));
            }
            else
            {
                distances.Add(.5);
            }

            return distances.Average();
        }

        public static double ComputeNormalized(string first, string second)
        {
            int editDistance = Compute(first, second);
            int maxLength = first.Length > second.Length ? first.Length : second.Length;

            return editDistance / (maxLength * 1.0);
        }

        public static int Compute(string first, string second)
        {
            int n = first.Length, m = second.Length;
            if (n == 0) return m;
            if (m == 0) return n;

            int curRow = 0, nextRow = 1;
            int[][] rows = new int[][] { new int[m + 1], new int[m + 1] };
            for (int j = 0; j <= m; ++j) rows[curRow][j] = j;

            for (int i = 1; i <= n; ++i)
            {
                rows[nextRow][0] = i;
                for (int j = 1; j <= m; ++j)
                {
                    int dist1 = rows[curRow][j] + 1;
                    int dist2 = rows[nextRow][j - 1] + 1;
                    int dist3 = rows[curRow][j - 1] +
                        (first[i - 1].Equals(second[j - 1]) ? 0 : 1);
                    rows[nextRow][j] = Math.Min(dist1, Math.Min(dist2, dist3));
                }
                // Swap the current and next rows
                if (curRow == 0)
                {
                    curRow = 1;
                    nextRow = 0;
                }

                else
                {
                    curRow = 0;
                    nextRow = 1;
                }
            }
            return rows[curRow][m];
        }
    }
}
