using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FastMRNGrouper : FastAbstractGrouper
    {
        const int _bucketSize = 100;

        public override Matches DistanceAtMostN(string[] strings, int n)
        {
            return MatchByRadius(strings.Select(s => int.Parse(s)).ToArray(), n, _bucketSize);
        }

        public static Matches MatchByRadius(int[] a, int r, int bucketSize)
        {
            var toReturn = MatchesEngine.NewMatches(a.Length);

            IndexedMRN[] imrns = a.Select((mrn, i) => new IndexedMRN { Index = i, MRN = mrn }).ToArray();
            var sorted = imrns.OrderBy(x => x.MRN).ToArray();

            int start = 0, end = 0;

            while (end < sorted.Length)
            {
                Console.Write($"\r{start}/{a.Length} MRN Progress");
                int diff = sorted[end].MRN - sorted[start].MRN;
                if (diff <= r)
                {

                    for (int i = start; i <= end; i++)
                    {
                        int diff2 = sorted[end].MRN - sorted[i].MRN;
                        int metric = diff2 == 0 ? 0 : diff2 / bucketSize + 1;
                        toReturn.AddMatch(sorted[i].Index, sorted[end].Index, metric);
                    }
                    end++;
                }
                else
                {
                    start++;
                }
            }
            Console.WriteLine();

            return toReturn;
        }
    }
}
