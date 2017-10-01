using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public static class ErrorScrubber
    {
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
