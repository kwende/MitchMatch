using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// This class is for manipulating the Matches data contract
    /// </summary>
    public static class MatchesEngine
    {
        public static Matches NewMatches(int n)
        {
            Matches toReturn = new Matches();

            toReturn.MatchArray = new List<IndexDistancePair>[n];
            for (int i = 0; i < n; i++)
            {
                toReturn.MatchArray[i] = new List<IndexDistancePair>();
            }

            return toReturn;
        }

        public static Matches NewMatches(string filePath)
        {
            return Serializer.Deserialize<Matches>(filePath);
        }

        public static long Pairs(this Matches matches)
        {
            long toReturn = 0;
            for (int i = 0; i < matches.MatchArray.Length; i++)
                toReturn += matches.MatchArray[i].Count();

            return toReturn / 2;
        }

        public static void Clean(this Matches matches)
        {
            var matchArray = matches.MatchArray;
            for (int i = 0; i < matchArray.Length; i++)
            {
                List<IndexDistancePair> newList = new List<IndexDistancePair>();
                var groupedByIndex = matchArray[i].GroupBy(pair => pair.Index);
                foreach(var group in groupedByIndex)
                {
                    if (group.Count() == 1)
                        newList.Add(group.First());
                    else
                        newList.Add(new IndexDistancePair { Index = group.Key, Distance = group.Min(p => p.Distance) });
                }
                matchArray[i] = newList;
            }
        }


        public static void AddMatch(this Matches matches, int i, int j, double distance)
        {
            var matchArray = matches.MatchArray;
            matches.AddDirectedMatch(i, j, distance);
            matches.AddDirectedMatch(j, i, distance);
            matchArray[i].Add(new IndexDistancePair { Index = j, Distance = distance });
            matchArray[j].Add(new IndexDistancePair { Index = i, Distance = distance });
        }

        public static void AddDirectedMatch(this Matches matches, int i, int j, double distance)
        {
            var matchArray = matches.MatchArray;
            matchArray[i].Add(new IndexDistancePair { Index = j, Distance = distance });
        }

        public static bool HasMatch(this Matches matches, int i, int j)
        {
            var matchArray = matches.MatchArray;
            return matchArray[i].Any(pair => pair.Index == j);
        }

        public static List<IndexDistancePair> Neighbors(this Matches matches, int i)
        {
            var matchArray = matches.MatchArray;
            return matchArray[i];
        }

        public static void Serialize(this Matches matches, string path)
        {
            Serializer.Serialize(matches, path);
        }
    }
}
