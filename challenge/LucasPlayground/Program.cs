using challenge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            Random random = new Random();

            var lines = File.ReadLines(@"C:/github/PMAC/FInalDataset.csv");
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var data = allData.Where(r => r.EnterpriseID >= 15374761).ToArray();
            Console.WriteLine(lines.Count() + " total rows"); // >= 15374761
            Console.WriteLine("Remaining: " + data.Length);

            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();


            Console.WriteLine();
            Console.WriteLine("SSN");
            var addedSSN = AddMatches(data, r => r.SSN, 4, ref matches, false, false, false);
            Console.WriteLine("Remaining: " + data.Where(r => !matches.ContainsKey(r.EnterpriseID)).Count());

            Console.WriteLine();
            Console.WriteLine("PHONE");
            var addedPhone = AddMatches(data, r => r.PHONE, 5, ref matches, true, false, false);
            Console.WriteLine("Remaining: " + data.Where(r => !matches.ContainsKey(r.EnterpriseID)).Count());

            Console.WriteLine();
            Console.WriteLine("NAME + DOB");
            var addedNameDOB = AddMatches(data, r => r.LAST + r.FIRST + r.DOB.ToString("d"), 4, ref matches, true, true, false);
            Console.WriteLine("Remaining: " + data.Where(r => !matches.ContainsKey(r.EnterpriseID)).Count());

            Console.WriteLine();
            Console.WriteLine("NAME + PHONE");
            var addedNamePhone = AddMatches(data, r => (r.PHONE <= 0 ? "" : r.LAST + r.FIRST + r.PHONE), 4, ref matches, false, false, false);
            Console.WriteLine("Remaining: " + data.Where(r => !matches.ContainsKey(r.EnterpriseID)).Count());

            Console.WriteLine();
            Console.WriteLine("NAME + ADDRESS");
            var addedNameAddress = AddMatches(data, r => r.LAST + r.FIRST + r.ADDRESS1, 4, ref matches, true, true, false);
            Console.WriteLine("Remaining: " + data.Where(r => !matches.ContainsKey(r.EnterpriseID)).Count());

            Console.WriteLine();
            Console.WriteLine("ADDRESS + DOB");
            var addedAddressDOB = AddMatches(data, r => r.ADDRESS1 + r.DOB.ToString("d"), 4, ref matches, true, true, false);
            Console.WriteLine("Remaining: " + data.Where(r => !matches.ContainsKey(r.EnterpriseID)).Count());

            //AddMatches(data, r => r.DOB.ToString("d") + r.ADDRESS1, 4, (r1, r2) => true, ref matches);

            //AddMatches(data, r => r.LAST + r.FIRST + r.ADDRESS1, 4, (r1, r2) => true, ref matches);

            var remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine(remainingRows.Count());
            Console.ReadLine();
        }

        public static row[] FilterToRows<T>(IEnumerable<row> data, Func<row, T> selector, T desiredValue) where T : IComparable
        {
            List<row> toReturn = new List<row>();
            foreach (row row in data)
            {
                T actualValue = selector(row);
                if (actualValue.Equals(desiredValue))
                {
                    toReturn.Add(row);
                }
            }
            return toReturn.ToArray();
        }

        public static List<IGrouping<T, row>> AddMatches<T>(IEnumerable<row> data, Func<row, T> groupingValue, int sizeToThrowAway, ref Dictionary<int, List<int>> matches, bool requirePartialMatch = true, bool printErrors = false, bool printActuals = false)
        {
            List<IGrouping<T, row>> addedThisTime = new List<IGrouping<T, row>>();

            var grouped = data.GroupBy(groupingValue);
            Console.WriteLine("Too large: " + grouped.Where(g => g.Count() >= sizeToThrowAway).Count());
            int counter = 0;
            foreach (var group in grouped)
            {
                if (group.Count() >= sizeToThrowAway) //These are all garbage
                {
                    //Console.WriteLine(group.Key);
                    continue;
                }
                if (group.Count() < 2)
                {
                    continue;
                }

                var representative = group.First();
                if (requirePartialMatch && group.Any(r => !challenge.Program.PartialMatch(r, representative)))
                {
                    if (printErrors)
                    {
                        foreach (row row in group)
                        {
                            row.Print();
                        }
                        Console.WriteLine();
                    }
                    counter++;
                }
                else
                {
                    bool addGroup = false;
                    foreach (var r in group)
                    {
                        if (!matches.ContainsKey(r.EnterpriseID))
                        {
                            matches[r.EnterpriseID] = new List<int>();
                            addGroup = true;

                        }
                        matches[r.EnterpriseID] = matches[r.EnterpriseID].Union(group.Select(r2 => r2.EnterpriseID)).ToList();
                    }
                    if (addGroup)
                    {
                        addedThisTime.Add(group);
                        if (printActuals)
                        {
                            foreach (row row in group)
                            {
                                row.Print();
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }

            Console.WriteLine("Not partially matched: " + counter);
            return addedThisTime;
        }
    }
}
