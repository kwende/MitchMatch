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
            row[] remainingRows = data;
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();


            Console.WriteLine();
            Console.WriteLine("MRN");
            AddMRNMatches(data, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("SSN");
            var addedSSN = AddMatches(data, r => r.SSN, 4, ref matches, false, false, false);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("PHONE");
            var addedPhone = AddMatches(data, r => r.PHONE, 5, ref matches, true, true, false);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + DOB");
            var addedNameDOB = AddMatches(data, r => r.LAST + r.FIRST + r.DOB.ToString("d"), 4, ref matches, true, true, false);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + PHONE");
            var addedNamePhone = AddMatches(data, r => (r.PHONE <= 0 ? "" : r.LAST + r.FIRST + r.PHONE), 4, ref matches, false, false, false);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + ADDRESS");
            var addedNameAddress = AddMatches(data, r => r.LAST + r.FIRST + r.ADDRESS1, 4, ref matches, true, true, false);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("ADDRESS + DOB");
            var addedAddressDOB = AddMatches(data, r => r.ADDRESS1 + r.DOB.ToString("d"), 4, ref matches, true, true, false);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            Console.WriteLine();
            Console.WriteLine("SOFT MATCH");
            var addedSoftMatches = AddSoftMatches(remainingRows, ref matches, true);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);



            Console.WriteLine(remainingRows.Count());
            Console.ReadLine();
        }

        public static void AddMRNMatches(IEnumerable<row> data, ref Dictionary<int, List<int>> matches)
        {
            var fourMillion = data.Where(r => r.MRN >= 4000000).ToArray();
            //Pair off and make a soft check on field to verify sameness
            for (int i = 0; i < fourMillion.Count(); i += 2)
            {
                var r = fourMillion[i];
                var s = fourMillion[i + 1];
                challenge.Program.Add(r, s, ref matches);

                //if(M(r,s,t=>t.ADDRESS1) || M(r,s,t => t.ALIAS) || M(r,s,t => t.DOB.ToString("d")) || M(r,s,t=>t.EMAIL) || M(r,s,t => t.FIRST) || M(r,s,t=>t.LAST) || M(r,s,t=>t.MOTHERS_MAIDEN_NAME) || M(r,s,t=>t.PHONE.ToString()) || M(r,s,t=>t.SSN.ToString()))
                //{

                //}
                //else
                //{
                //    Console.WriteLine(s.EnterpriseID);
                //}
            }
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

        public static List<List<row>> AddSoftMatches(row[] remainingRows, ref Dictionary<int, List<int>> matches, bool printMatches = false)
        {
            List<List<row>> addedThisTime = new List<List<row>>();
            //For what's left, brute force soft match on at least 2 of name, DOB, address
            for (int i = 0; i < remainingRows.Count(); i++)
            {
                for (int j = 0; j < remainingRows.Count(); j++)
                {
                    if (i == j)
                        continue;

                    int fieldAgreement = 0;

                    var ri = remainingRows[i];
                    var rj = remainingRows[j];

                    if (challenge.Program.OneDifference(ri.LAST, rj.LAST))
                        fieldAgreement++;

                    if (challenge.Program.OneDifference(ri.ADDRESS1, rj.ADDRESS1))
                        fieldAgreement++;

                    if (challenge.Program.OneDifference(ri.DOB.ToString("d"), rj.DOB.ToString("d")))
                        fieldAgreement++;

                    if (fieldAgreement >= 2)
                    {
                        if (!matches.ContainsKey(ri.EnterpriseID))
                        {
                            challenge.Program.Add(ri, rj, ref matches);
                            addedThisTime.Add(new List<row> { ri, rj });
                            if (printMatches)
                            {
                                ri.Print();
                                rj.Print();
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
            return addedThisTime;
        }
    }
}
