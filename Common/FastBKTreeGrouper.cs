using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FastBKTreeGrouper : FastAbstractGrouper
    {
        public override Matches DistanceAtMostN(string[] strings, int n)
        {
            return EditDistanceAtMostN(strings, strings, n);
        }

        public static Matches EditDistanceAtMostN(string[] S, string[] T, int n)
        {
            Matches toReturn = MatchesEngine.NewMatches(S.Length);

            //Create BKTree
            var bkTree = BKTreeEngine.CreateBKTree(S.ToList());

            //Create lookup table
            Dictionary<string, int> stringToInt = new Dictionary<string, int>();
            for (int i = 0; i < S.Length; i++)
            {
                stringToInt[S[i]] = i;
            }

            int c = 0;
            object cLock = new object();
            object matchLock = new object();
            Parallel.For(0, T.Length, j =>
            {
                lock (cLock)
                {
                    Console.Write($"\r{c++}/{T.Length}");
                }

                var neighbors = BKTreeEngine.EditDistanceAtMostN(T[j], bkTree, n);
                foreach (var neighbor in neighbors)
                {
                    int i = stringToInt[neighbor];
                    lock (cLock)
                    {
                        toReturn.AddDirectedMatch(i, j, EditDistanceEngine.Compute(T[j],neighbor));
                    }
                }
            }
            );

            return toReturn;
        }
    }
}
