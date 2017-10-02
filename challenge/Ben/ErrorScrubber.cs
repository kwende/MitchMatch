using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public static class ErrorScrubber
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

        public static List<Tuple<double, int, int>> ReturnMaxErrorForMatchedGroups<T>(List<IGrouping<T, Row>> matches)
        {
            List<Tuple<double, int, int>> ret = new List<Tuple<double, int, int>>();
            foreach (IGrouping<T, Row> match in matches)
            {
                Row[] rows = match.ToArray();
                double maxDifference = -1.0f;
                int row1Id = -1, row2Id = -1;
                foreach (Row row1 in rows)
                {
                    foreach (Row row2 in rows)
                    {
                        if (row1 != row2)
                        {
                            double distance = EditDistance.ComputeDistanceForRecordPair(row1, row2);
                            if (distance > maxDifference)
                            {
                                maxDifference = distance;
                                row1Id = row1.EnterpriseID;
                                row2Id = row2.EnterpriseID;
                            }
                        }
                    }
                }
                ret.Add(new Tuple<double, int, int>(maxDifference, row1Id, row2Id));
            }

            return ret;
        }
    }
}
