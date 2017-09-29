﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class BKTreeEngine
    {
        public static BKTree CreateBKTree(List<string> strings)
        {
            var toReturn = CreateBKTree(strings, 0, strings.Count);
            Console.WriteLine();
            return toReturn;
        }

        private static BKTree CreateBKTree(List<string> strings, int inserted, int originalStringCount)
        {
            if (strings.Count() == 0)
                return null;

            List<string>[] buckets = new List<string>[2 * strings.Max(s => s.Length) + 1];
            for (int i = 0; i < buckets.Length; i++)
                buckets[i] = new List<string>();

            Random r = new Random();

            int rootIndex = r.Next(strings.Count());
            string rootString = strings[rootIndex];
            BKTree toReturn = new BKTree();
            toReturn.StringValue = rootString;
            Console.Write($"\r{++inserted}/{originalStringCount} strings added to BK tree");

            for (int i = 0; i < strings.Count; i++)
            {
                if (i == rootIndex)
                    continue;

                string currentString = strings[i];

                int d = EditDistance.Compute(rootString, currentString);
                buckets[d].Add(currentString);
            }

            int maxIndex;
            for (maxIndex = buckets.Length - 1; maxIndex >= 0; maxIndex--)
            {
                if (buckets[maxIndex].Count != 0)
                    break;
            }

            toReturn.Children = new BKTree[maxIndex + 1];

            for (int i = 0; i <= maxIndex; i++)
            {
                toReturn.Children[i] = CreateBKTree(buckets[i],inserted,originalStringCount);
                inserted += buckets[i].Count;
            }

            return toReturn;
        }

        public static List<string> EditDistanceAtMostN(string s, BKTree bkTree, int n)
        {
            List<string> toReturn = new List<string>();

            if (bkTree == null)
                return toReturn;

            int d = EditDistance.Compute(s, bkTree.StringValue);
            if (d <= n)
            {
                toReturn.Add(bkTree.StringValue);
                int maxIndex = System.Math.Min(n - d, bkTree.Children.Length - 1);
                for(int i = 0; i <= maxIndex; i++)
                {
                    toReturn.AddRange(AllChildren(bkTree.Children[i]));
                }
            }




            int start = System.Math.Max(System.Math.Max(0, d - n), n - d + 1);
            int end = System.Math.Min(d + n, bkTree.Children.Length - 1);

            for(int i = start; i <= end; i++)
            {
                toReturn.AddRange(EditDistanceAtMostN(s, bkTree.Children[i], n));
            }

            return toReturn;
        }

        public static List<string> AllChildren(BKTree tree)
        {
            if (tree == null)
                return new List<string>();

            List<string> toReturn = new List<string> { tree.StringValue };
            for(int i = 0; i < tree.Children.Length; i++)
            {
                toReturn.AddRange(AllChildren(tree.Children[i]));
            }

            return toReturn;
        }

        public static string[] DistinctNonEmptyStrings(IEnumerable<Row> data, Func<Row, string> fieldSelector)
        {
            return data.Select(d => fieldSelector(d)).Distinct().Where(s => s != "").ToArray();
        }
    }

    public class BKTree
    {
        public string StringValue;
        public BKTree[] Children;
    }
}
