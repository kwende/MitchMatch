using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class FastBKTreeGrouper : FastAbstractGrouper
    {
        public override Matches EditDistanceAtMostN(string[] strings, int n)
        {
            Matches toReturn = new Matches(strings.Length);

            //Create BKTree
            var bkTree = BKTreeEngine.CreateBKTree(strings.ToList());

            //Create lookup table
            Dictionary<string, int> stringToInt = new Dictionary<string, int>();
            for(int i = 0; i < strings.Length; i++)
            {
                stringToInt[strings[i]] = i;
            }

            int c = 0;
            object cLock = new object();
            object matchLock = new object();
            Parallel.For(0, strings.Length, i =>
            {
                lock (cLock)
                {
                    Console.Write($"\r{c++}/{strings.Length}");
                }

                var neighbors = BKTreeEngine.EditDistanceAtMostN(strings[i], bkTree, 2);
                foreach(var neighbor in neighbors)
                {
                    int j = stringToInt[neighbor];
                    if (i <= j)
                    {
                        lock (cLock)
                        {
                            toReturn.AddMatch(i, j);
                        }
                    }
                }
            }
            );

            return toReturn;
        }
    }
}
