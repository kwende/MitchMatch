using challenge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class EditDistance
    {
        public static void WriteWorstToDisk<T>(List<IGrouping<T, Row>> groups, Row[] data, string outputFile, double threshold)
        {
            List<Tuple<double, int, int>> tuples = ErrorScrubber.ReturnMaxErrorForMatchedGroups<T>(groups);

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

        public static void GenerateScatterPlot<T>(List<IGrouping<T, Row>> groups, Row[] data, string outputFile)
        {
            List<Tuple<double, int, int>> tuples = ErrorScrubber.ReturnMaxErrorForMatchedGroups<T>(groups);

            using (StreamWriter sw = File.CreateText(outputFile))
            {
                foreach (Tuple<double, int, int> tuple in tuples)
                {
                    sw.WriteLine($"{tuple.Item2},{tuple.Item3},{tuple.Item1}");
                }
            }
        }

        public static double ComputeDistanceForRecordPair(Row row1, Row row2)
        {
            double distance1 = ComputeNormalized(row1.LAST, row2.LAST);
            double distance2 = ComputeNormalized(row1.FIRST, row2.FIRST);
            double distance3 = ComputeNormalized(row1.SSN.ToString(), row2.SSN.ToString());
            double distance4 = ComputeNormalized(row1.DOB.ToShortDateString(), row2.DOB.ToShortDateString());
            return (distance1 + distance2 + distance3 + distance4) / 4.0;
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
