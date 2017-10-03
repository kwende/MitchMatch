using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FastEditDistanceGrouper : FastAbstractGrouper
    {
        public override Matches EditDistanceAtMostN(string[] strings, int n)
        {
            Matches toReturn = MatchesEngine.NewMatches(strings.Count());
            //Every string matches itself
            for (int i = 0; i < strings.Length; i++)
                toReturn.AddMatch(i, i, 0);

            Console.WriteLine("Creating the neighborhoods");
            List<EditDistanceMatchObject> neighborHood = new List<EditDistanceMatchObject>();
            for (int i = 0; i < strings.Length; i++)
            {
                Console.Write($"\r{i}/{strings.Length} neighborhoods computed");
                neighborHood.AddRange(DeleteN(strings[i], i, n));
            }
            Console.WriteLine();

            Console.WriteLine("Grouping by neighborhood");
            var grouped = neighborHood.GroupBy(edmo => edmo.Substring).ToArray();


            Console.WriteLine("Checking edit distance");
            int c = 0;
            foreach (var group in grouped)
            {
                Console.Write($"\r{c++}/{grouped.Length} edit distance groups checked");
                var groupArray = group.ToArray();
                if (group.Key == "")  //In this case, both of the original strings had length at most n, so they have edit distance at most n.  We are probably avoiding a lot of work on a huge component by doing this
                {
                    for (int i = 0; i < groupArray.Length; i++)
                        for (int j = i + 1; j < groupArray.Length; j++)
                            toReturn.AddMatch(groupArray[i].Index, groupArray[j].Index);
                }
                else
                {
                    for (int i = 0; i < groupArray.Length; i++)
                        for (int j = i + 1; j < groupArray.Length; j++)
                            if (EditDistance(groupArray[i], groupArray[j]) <= n)
                                toReturn.AddMatch(groupArray[i].Index, groupArray[j].Index);
                }
            }
            Console.WriteLine();

            Console.WriteLine("Cleaning string match object");
            toReturn.Clean();

            //ExploreStrings(strings, toReturn);

            return toReturn;
        }


        /// <summary>
        /// Returns pairs where first element is from first part and second element from second.  The indices for elements of T are offset by S.Length
        /// </summary>
        /// <param name="S"></param>
        /// <param name="T"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Matches EditDistanceAtMostN(string[] S, string[] T, int n)
        {
            Matches toReturn = MatchesEngine.NewMatches(S.Length);

            Console.WriteLine("Creating the neighborhoods");
            List<BipartiteEditDistanceMatchObject> neighborHood = new List<BipartiteEditDistanceMatchObject>();
            int c = 0;
            for (int i = 0; i < S.Length; i++)
            {
                Console.Write($"\r{c++}/{S.Length} S neighborhoods computed");
                var withoutParts = DeleteN(S[i], i, n);
                foreach (var edmo in withoutParts)
                {
                    neighborHood.Add(new BipartiteEditDistanceMatchObject { EditDistanceMatchObject = edmo, Part = 0 });
                }
            }

            c = 0;
            for (int i = 0; i < T.Length; i++)
            {
                Console.Write($"\r{c++}/{T.Length} T neighborhoods computed");
                var withoutParts = DeleteN(T[i], i, n);
                foreach (var edmo in withoutParts)
                {
                    neighborHood.Add(new BipartiteEditDistanceMatchObject { EditDistanceMatchObject = edmo, Part = 1 });
                }
            }

            Console.WriteLine();

            Console.WriteLine("Grouping by neighborhood");
            var grouped = neighborHood.GroupBy(edmo => edmo.EditDistanceMatchObject.Substring).ToArray();


            Console.WriteLine("Checking edit distance");
            c = 0;
            foreach (var group in grouped)
            {
                var groupS = group.Where(bedmo => bedmo.Part == 0).Select(bedmo => bedmo.EditDistanceMatchObject).ToArray();
                var groupT = group.Where(bedmo => bedmo.Part == 1).Select(bedmo => bedmo.EditDistanceMatchObject).ToArray();

                Console.Write($"\r{c++}/{grouped.Length} edit distance groups checked");
                if (group.Key == "")  //In this case, both of the original strings had length at most n, so they have edit distance at most n.  We are probably avoiding a lot of work on a huge component by doing this
                {
                    foreach (var s in groupS)
                        foreach (var t in groupT)
                            toReturn.AddDirectedMatch(s.Index, t.Index);
                }
                else
                {
                    foreach (var s in groupS)
                        foreach (var t in groupT)
                            if (EditDistance(s, t) <= n)
                                toReturn.AddDirectedMatch(s.Index, t.Index);
                }
            }
            Console.WriteLine();

            Console.WriteLine("Cleaning string match object");
            toReturn.Clean();

            //ExploreStrings(strings, toReturn);

            return toReturn;
        }


        /// <summary>
        /// Assumes substrings match
        /// </summary>
        /// <returns></returns>
        static int EditDistance(EditDistanceMatchObject e1, EditDistanceMatchObject e2)
        {
            int toReturn = 0;

            //We will verify that the union of deleted indices has size at most n

            //Indices to deleted indices.  It is also the index offset to the diff string.  I know that doesn't make sense, but it's meant to be a clue as to why the algorithm works.
            int i1 = 0, i2 = 0;

            while (i1 < e1.DeletedIndices.Count && i2 < e2.DeletedIndices.Count)
            {
                int nextString1Index = e1.DeletedIndices[i1] + i2;
                int nextString2Index = e2.DeletedIndices[i2] + i1;
                if (nextString1Index < nextString2Index)
                {
                    i1++;
                }
                else if (nextString2Index < nextString1Index)
                {
                    i2++;
                }
                else
                {
                    i1++;
                    i2++;
                }

                toReturn++;
            }

            toReturn += e1.DeletedIndices.Count - i1 + e2.DeletedIndices.Count - i2;

            return toReturn;
        }

        static void ExploreStrings(string[] strings, Matches matches)
        {
            Random r = new Random();

            while (true)
            {

                int si = r.Next(strings.Length);

                var neighbors = matches.Neighbors(si);
                int matchCount = neighbors.Count;

                Console.Write($"{strings[si]} : {matchCount} ");

                if (matchCount <= 5)
                {
                    foreach (var neighbor in neighbors)
                    {
                        Console.Write($"{strings[neighbor.Index]} ");
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int mii = r.Next(matchCount);
                        int mi = neighbors[mii].Index;
                        Console.Write($"{strings[mi]} ");
                    }
                }

                Console.WriteLine();

                Console.ReadLine();
            }
        }

        static List<EditDistanceMatchObject> DeleteN(string s, int index, int n)
        {
            List<EditDistanceMatchObject> toReturn = new List<EditDistanceMatchObject>();

            EditDistanceMatchObject rowAsEDMO = new EditDistanceMatchObject
            {
                Index = index,
                DeletedIndices = new List<int>(),
                Substring = s
            };

            toReturn.Add(rowAsEDMO);
            List<EditDistanceMatchObject> currentGeneration = toReturn;
            for (int i = 0; i < n; i++)
            {
                List<EditDistanceMatchObject> nextGeneration = new List<EditDistanceMatchObject>();

                foreach (var element in currentGeneration)
                {
                    nextGeneration.AddRange(DeleteOneMore(element));
                }

                toReturn.AddRange(nextGeneration);

                currentGeneration = nextGeneration;
            }

            return toReturn;
        }

        static List<EditDistanceMatchObject> DeleteOneMore(EditDistanceMatchObject edmo)
        {
            List<EditDistanceMatchObject> toReturn = new List<EditDistanceMatchObject>();

            int oldStringLength = edmo.Substring.Length;
            int oldDeletedIndexCount = edmo.DeletedIndices.Count;
            int minDeletionIndex = oldDeletedIndexCount == 0 ? 0 : edmo.DeletedIndices.Max() + 1;
            int maxDeletionIndex = oldStringLength + oldDeletedIndexCount - 1;

            for (int i = minDeletionIndex; i <= maxDeletionIndex; i++)
            {
                EditDistanceMatchObject toAdd = new EditDistanceMatchObject
                {
                    DeletedIndices = edmo.DeletedIndices.Select(index => index).ToList(),
                    Index = edmo.Index,
                    Substring = edmo.Substring.Remove(i - oldDeletedIndexCount, 1)
                };
                toAdd.DeletedIndices.Add(i);

                toReturn.Add(toAdd);
            }

            return toReturn;
        }
    }
}