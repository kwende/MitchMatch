using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public static class FastFuzzyAddressGrouper
    {
        public static void sosijfo(IEnumerable<Row> allData)
        {
            var strings = BKTreeEngine.DistinctNonEmptyStrings(allData, d => d.ADDRESS1).ToList();
            var streetTypes = strings.GroupBy(s => s.Split(' ').Last()).ToArray();
            var streetTypePrefixes = streetTypes.Select(g => g.Select(s => s.Substring(0, s.Length - g.Key.Length)).ToArray()).ToArray();
            var streetTypeStrings = streetTypes.Select(g => g.Key).ToArray();
            var streetTypeNeighbors = FastEditDistanceGrouper.EditDistanceAtMostN(streetTypeStrings, 2);
            Console.WriteLine(streetTypeNeighbors.Pairs());
            for (int i = 0; i < streetTypeStrings.Length; i++)
            {
                foreach (var neighbor in streetTypeNeighbors.Neighbors(i))
                {
                    if (neighbor < i) //This will be handled elsewhere
                        continue;

                    int d = EditDistance.Compute(streetTypeStrings[i], streetTypeStrings[neighbor]);

                    FastEditDistanceGrouper.EditDistanceAtMostN(streetTypePrefixes[i], streetTypePrefixes[neighbor], 2 - d);
                }
            }

        }
    }
}
