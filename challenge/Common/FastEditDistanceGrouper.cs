using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    static class FastEditDistanceGrouper
    {
        public static RowMatchObject EditDistanceAtMostN(Row[] data, Func<Row, string> fieldSelector, int n)
        {
            //Start by grouping the data into fields
            Console.WriteLine("Grouping By Field Value");
            Dictionary<string, List<Row>> rowsByFieldValue = new Dictionary<string, List<Row>>();
            foreach (var d in data)
            {
                string field = fieldSelector(d);
                if (field == "")
                    continue;
                if (!rowsByFieldValue.ContainsKey(field))
                    rowsByFieldValue[field] = new List<Row>();

                rowsByFieldValue[field].Add(d);
            }

            var strings = rowsByFieldValue.Select(p => p.Key).ToArray();
            var stringMatches = EditDistanceAtMostN(strings, n);

            Dictionary<string, int> stringToArrayIndex = new Dictionary<string, int>();
            for (int i = 0; i < strings.Length; i++)
                stringToArrayIndex[strings[i]] = i;

            List<Row>[] rowsWithThisField = new List<Row>[strings.Length];
            for (int i = 0; i < strings.Length; i++)
                rowsWithThisField[i] = rowsByFieldValue[strings[i]];

            RowMatchObject toReturn = new RowMatchObject
            {
                Strings = strings,
                StringMatches = stringMatches,
                StringToArrayIndex = stringToArrayIndex,
                RowsWithThisField = rowsWithThisField,
                FieldSelector = fieldSelector
            };

            return toReturn;
        }

        public static Matches EditDistanceAtMostN(string[] strings, int n)
        {
            Matches toReturn = new Matches(strings.Count());
            //Every string matches itself
            for (int i = 0; i < strings.Length; i++)
                toReturn.AddMatch(i, i);

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
                        Console.Write($"{strings[neighbor]} ");
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int mii = r.Next(matchCount);
                        int mi = neighbors[mii];
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

    class EditDistanceMatchObject
    {
        public int Index { get; set; }
        public string Substring { get; set; }
        //These are stored in the indexing scheme for the original string
        public List<int> DeletedIndices { get; set; }
    }

    public class RowMatchObject
    {
        public string[] Strings { get; set; }
        public Matches StringMatches { get; set; }
        public Dictionary<string, int> StringToArrayIndex { get; set; }
        public List<Row>[] RowsWithThisField { get; set; }
        public Func<Row,string> FieldSelector { get; set; }
    }

    public class Matches
    {
        private List<int>[] _matchArray;


        public Matches(int n)
        {
            _matchArray = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                _matchArray[i] = new List<int>();
            }
        }

        public long Pairs()
        {
            long toReturn = 0;
            for (int i = 0; i < _matchArray.Length; i++)
                toReturn += _matchArray[i].Count();

            return toReturn / 2;
        }

        public void Clean()
        {
            for (int i = 0; i < _matchArray.Length; i++)
            {
                _matchArray[i] = _matchArray[i].Distinct().ToList();
            }
        }


        public void AddMatch(int i, int j)
        {
            _matchArray[i].Add(j);
            _matchArray[j].Add(i);
        }

        public bool HasMatch(int i, int j)
        {
            return _matchArray[i].Contains(j);
        }

        public List<int> Neighbors(int i)
        {
            return _matchArray[i];
        }
    }
}