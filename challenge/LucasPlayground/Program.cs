﻿using challenge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground
{
    class Program
    {
        private static int[] _badSSNs = new int[0];

        static void Main(string[] args)
        {
            Random random = new Random();

            var lines = File.ReadLines(@"C:/users/brush/desktop/FInalDataset.csv");
            //var lines = File.ReadLines(@"C:/github/PMAC/FInalDataset.csv");
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var data = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();
            Console.WriteLine(lines.Count() + " total rows"); // >= 15374761
            row[] remainingRows = data;
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();

            _badSSNs = data.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();

            Console.WriteLine();
            Console.WriteLine("MRN");
            AddMRNMatches(data, ref matches);
            //AddMRNMatchesBen(data, ref matches); 


            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("SSN");
            var addedSSN = AddMatches(data, r => r.SSN, 4, (r1, r2) =>
                    (r1.FIRST != "" && challenge.Program.OneDifference(r1.FIRST, r2.FIRST)) ||
                    (r1.LAST != "" && challenge.Program.OneDifference(r1.LAST, r2.LAST)) ||
                    challenge.Program.OneDifference(r1.PHONE.ToString(), r2.PHONE.ToString()) ||
                    challenge.Program.FuzzyDateEquals(r1.DOB, r2.DOB) ||
                    challenge.Program.FuzzyAddressMatch(r1, r2),
                ref matches);

            //List<Tuple<double, int, int>> tuples = challenge.Ben.ErrorScrubber.ReturnMaxErrorForMatchedGroups(addedSSN);

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/social_matches.csv"))
            //{
            //    foreach (Tuple<double, int, int> tuple in tuples)
            //    {
            //        sw.WriteLine($"{tuple.Item2},{tuple.Item3},{tuple.Item1}"); 
            //    }
            //}

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            Console.WriteLine();
            Console.WriteLine("NAME + PHONE");
            var addedNamePhone = AddMatches(data, r =>
                {
                    return (r.LAST != "" ? (r.PHONE > 0 ? r.LAST + r.FIRST + r.PHONE : "NOPHONE") : "NONAME");
                }, 4, (r1, r2) =>
                    challenge.Program.FuzzyDateEquals(r1.DOB, r2.DOB) ||
                    challenge.Program.FuzzyAddressMatch(r1, r2) ||
                    !IsSSNValid(r1.SSN) ||
                    !IsSSNValid(r2.SSN) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + DOB");
            var addedNameDOB = AddMatches(data, r =>
                {
                    return (r.LAST != "" ? (r.DOB != default(DateTime) ? r.LAST + r.FIRST + r.DOB.ToString("d") : "NODOB") : "NONAME");
                }, 4, (r1, r2) =>
                    challenge.Program.OneDifference(r1.PHONE.ToString(), r2.PHONE.ToString()) ||
                    challenge.Program.FuzzyAddressMatch(r1, r2) ||
                    !IsSSNValid(r1.SSN) ||
                    !IsSSNValid(r2.SSN) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            /////////////////////////////////////
            Console.WriteLine();
            Console.WriteLine("NAME + ADDRESS");

            // TODO: Change condition on fuzzydateequals to allow simple transpose of two digits and century? 

            var addedNameAddress = AddMatches(data, r =>
            {
                return (r.LAST != "" ? (r.ADDRESS1 != "" ? r.LAST + r.FIRST + r.ADDRESS1 : "NOADDRESS") : "NONAME");
            }, 4, (r1, r2) =>
                challenge.Program.FuzzyDateEquals(r1.DOB, r2.DOB) ||
                FuzzySSNMatch(r1.SSN, r2.SSN),
                ref matches);

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            //List<Tuple<double, int, int>> tuples = challenge.Ben.ErrorScrubber.ReturnMaxErrorForMatchedGroups<string>(addedNameAddress);

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/name+address_matches.csv"))
            //{
            //    foreach (Tuple<double, int, int> tuple in tuples)
            //    {
            //        if (tuple.Item1 > .4)
            //        {
            //            sw.WriteLine(data.Where(n => n.EnterpriseID == tuple.Item2).First().ToString());
            //            sw.WriteLine(data.Where(n => n.EnterpriseID == tuple.Item3).First().ToString());
            //            sw.WriteLine();
            //            //sw.WriteLine($"{tuple.Item2},{tuple.Item3},{tuple.Item1}");
            //        }
            //        //sw.WriteLine($"{tuple.Item2},{tuple.Item3},{tuple.Item1}");
            //    }
            //}

            /////////////////////////////////////

            Console.WriteLine();
            Console.WriteLine("PHONE");
            var addedPhone = AddMatches(data, r => r.PHONE, 5, (r1, r2) =>
                    (r1.FIRST != "" && challenge.Program.OneDifference(r1.FIRST, r2.FIRST)) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN) ||
                    challenge.Program.DateSoftMatch(r1.DOB, r2.DOB),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("ADDRESS + DOB");
            var addedAddressDOB = AddMatches(data, r =>
                {
                    return (r.ADDRESS1 != "" ? (r.DOB != default(DateTime) ? r.DOB.ToString("d") + r.ADDRESS1 : "NODOB") : "NOADDRESS");
                }, 4, (r1, r2) =>
                    (r1.FIRST != "" && challenge.Program.OneDifference(r1.FIRST, r2.FIRST)) ||
                    !IsSSNValid(r1.SSN) ||
                    !IsSSNValid(r2.SSN) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);



            Console.WriteLine();
            Console.WriteLine("SOFT MATCH");
            var addedSoftMatches = AddSoftMatches(remainingRows, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            Console.WriteLine();
            Console.WriteLine("SOFT MATCH2");
            var addedSoftMatches2 = AddSoftMatches2(remainingRows, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("HAND MATCHED");
            var addedHandMatched = AddHandMatches(data, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine(remainingRows.Count());
            Console.ReadLine();


            Console.WriteLine();
            Console.WriteLine("HAND REMOVED");
            var removedHandMatched = RemoveHandErrors(data, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine(remainingRows.Count());
            Console.ReadLine();
        }

        private static int _printCount = 0;
        private static void PrintCheckCount()
        {
            if (++_printCount % 50 == 0)
            {
                Console.WriteLine("continue printing? ");
                string answer = Console.ReadLine();
                if (answer != "Y" && answer != "y")
                {
                    _printErrors = false;
                    _printActuals = false;
                }
            }
        }
        private static void PrintRows(IEnumerable<row> rows)
        {
            foreach (row row in rows)
            {
                row.Print();
            }
            Console.WriteLine();
            PrintCheckCount();
        }

        private static void PrintPair(row a, row b)
        {
            a.Print();
            b.Print();
            Console.WriteLine();
            PrintCheckCount();
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
            }
        }

        public static void AddMRNMatchesBen(IEnumerable<row> data, ref Dictionary<int, List<int>> matches)
        {
            var fourMillion = data.Where(r => r.MRN >= 4000000).ToArray();
            //Pair off and make a soft check on field to verify sameness
            using (StreamWriter sw = File.CreateText("C:/users/ben/desktop/errors.csv"))
            {
                for (int i = 0; i < fourMillion.Count(); i += 2)
                {
                    var r = fourMillion[i];
                    var s = fourMillion[i + 1];
                    challenge.Program.Add(r, s, ref matches);

                    double editDistance = challenge.Ben.EditDistance.ComputeDistanceForRecordPair(r, s);
                    if (editDistance > .6)
                    {
                        sw.WriteLine(s.ToString());
                        sw.WriteLine(r.ToString());
                        sw.WriteLine();
                    }
                }
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

        private static bool _printLargeGroupValues = false;
        private static bool _printErrors = false;
        private static bool _printActuals = false;

        static List<IGrouping<T, row>> AddMatches<T>(IEnumerable<row> data, Func<row, T> groupingValue, int sizeToThrowAway, Func<row, row, bool> softEquals, ref Dictionary<int, List<int>> matches)
        {
            List<IGrouping<T, row>> addedThisTime = new List<IGrouping<T, row>>();

            var grouped = data.GroupBy(groupingValue);
            Console.WriteLine("Too large: " + grouped.Where(g => g.Count() >= sizeToThrowAway).Count());
            int counter = 0;
            foreach (var group in grouped)
            {
                if (group.Count() >= sizeToThrowAway) //These are all garbage
                {
                    if (_printLargeGroupValues)
                    {
                        Console.WriteLine(group.Key + $" (size {group.Count()})");
                    }
                    continue;
                }
                if (group.Count() < 2)
                {
                    continue;
                }

                var representative = group.First();
                if (group.Any(r => !softEquals(r, representative)))
                {
                    if (_printErrors)
                    {
                        PrintRows(group);
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
                        if (_printActuals)
                        {
                            PrintRows(group);
                        }
                    }
                }
            }

            Console.WriteLine(counter);
            return addedThisTime;
        }

        public static bool IsSSNValid(int a)
        {
            return a > 0 && !_badSSNs.Contains(a);
        }

        public static bool FuzzySSNMatch(int a, int b)
        {
            return IsSSNValid(a) && IsSSNValid(b) && challenge.Program.OneDifference(a.ToString(), b.ToString());
        }

        public static bool NonemptyEquality(string a, string b)
        {
            return a != "" && b != "" && a == b;
        }

        public static List<List<row>> AddSoftMatches(row[] remainingRows, ref Dictionary<int, List<int>> matches)
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

                    if (FuzzySSNMatch(ri.SSN, rj.SSN))
                        fieldAgreement++;

                    if (challenge.Program.KDifferences(ri.LAST, rj.LAST, 2))
                        fieldAgreement++;

                    if (challenge.Program.FuzzyAddressMatch(ri, rj))
                        fieldAgreement++;

                    if (challenge.Program.FuzzyDateEquals(ri.DOB, rj.DOB))
                        fieldAgreement++;

                    if (fieldAgreement >= 2)
                    {
                        if (!matches.ContainsKey(ri.EnterpriseID))
                        {
                            challenge.Program.Add(ri, rj, ref matches);
                            addedThisTime.Add(new List<row> { ri, rj });
                            if (_printActuals)
                            {
                                PrintPair(ri, rj);
                            }
                        }
                        else if (!matches.ContainsKey(rj.EnterpriseID))
                        {
                            challenge.Program.Add(ri, rj, ref matches);
                            addedThisTime.Add(new List<row> { ri, rj });
                            if (_printActuals)
                            {
                                PrintPair(ri, rj);
                            }
                        }
                    }
                }
            }
            return addedThisTime;
        }

        public static List<List<row>> AddSoftMatches2(row[] remainingRows, ref Dictionary<int, List<int>> matches)
        {
            List<List<row>> addedThisTime = new List<List<row>>();

            for (int i = 0; i < remainingRows.Count(); i++)
            {
                for (int j = 0; j < remainingRows.Count(); j++)
                {
                    if (i == j)
                        continue;

                    int fieldAgreement = 0;

                    var ri = remainingRows[i];
                    var rj = remainingRows[j];

                    if (FuzzySSNMatch(ri.SSN, rj.SSN))
                        fieldAgreement++;

                    if (challenge.Program.KDifferences(ri.LAST, rj.LAST, 2))
                        fieldAgreement++;

                    if (challenge.Program.FuzzyAddressMatch(ri, rj))
                        fieldAgreement++;

                    if (challenge.Program.FuzzyDateEquals(ri.DOB, rj.DOB))
                        fieldAgreement++;


                    if (NonemptyEquality(ri.FIRST, rj.FIRST))
                        fieldAgreement++;


                    if (fieldAgreement >= 2)
                    {
                        if (!matches.ContainsKey(ri.EnterpriseID))
                        {
                            challenge.Program.Add(ri, rj, ref matches);
                            addedThisTime.Add(new List<row> { ri, rj });
                            if (_printActuals)
                            {
                                PrintPair(ri, rj);
                            }
                        }
                        else if (!matches.ContainsKey(rj.EnterpriseID))
                        {
                            challenge.Program.Add(ri, rj, ref matches);
                            addedThisTime.Add(new List<row> { ri, rj });
                            if (_printActuals)
                            {
                                PrintPair(ri, rj);
                            }
                        }
                    }
                }
            }
            return addedThisTime;
        }

        private static List<List<row>> AddHandMatches(row[] remainingRows, ref Dictionary<int, List<int>> matches)
        {
            List<int[]> pairs = new List<int[]>
            {
                new int[] {15811621, 15750288},
                new int[] {15802888, 15456558},
                new int[] {15510682, 15598625},
                new int[] {15562243, 15863734},
                new int[] {15843982, 15988253},
                new int[] {15447438, 16021452},
                new int[] {15566242, 15393356},
                new int[] {15869829, 15444537},
                new int[] {15483298, 15544065},
                new int[] {15380819, 15586885},
                new int[] {15474114, 15393886},
                new int[] {15476947, 15766192},
                new int[] {15671788, 15696806},
                new int[] {15476869, 15541825},
                new int[] {15460667, 15923220},
                new int[] {15688015, 15555730},
            };

            List<List<row>> addedThisTime = new List<List<row>>();

            foreach (int[] pair in pairs)
            {
                row a = remainingRows.Where(row => row.EnterpriseID == pair[0]).FirstOrDefault();
                row b = remainingRows.Where(row => row.EnterpriseID == pair[1]).FirstOrDefault();

                challenge.Program.Add(a, b, ref matches);
                addedThisTime.Add(new List<row> { a, b });
                if (_printActuals)
                {
                    PrintPair(a, b);
                }
            }

            return addedThisTime;
        }

        public static void Remove(row a, row b, ref Dictionary<int, List<int>> matches)
        {
            Remove(a.EnterpriseID, b.EnterpriseID, ref matches);
        }

        public static void Remove(int a, int b, ref Dictionary<int, List<int>> matches)
        {
            RemoveOrdered(a, b, ref matches);
            RemoveOrdered(b, a, ref matches);
        }

        static void RemoveOrdered(int a, int b, ref Dictionary<int, List<int>> matches)
        {
            if (matches.ContainsKey(a))
            {
                matches[a].Remove(b);
                if (matches[a].Count == 0)
                {
                    matches.Remove(a);
                }
            }
        }

        private static List<List<row>> RemoveHandErrors(row[] remainingRows, ref Dictionary<int, List<int>> matches)
        {
            List<int[]> pairs = new List<int[]>
            {
                //new int[] {15688015, 15555730},
            };

            List<List<row>> removedThisTime = new List<List<row>>();

            foreach (int[] pair in pairs)
            {
                row a = remainingRows.Where(row => row.EnterpriseID == pair[0]).FirstOrDefault();
                row b = remainingRows.Where(row => row.EnterpriseID == pair[1]).FirstOrDefault();

                Remove(a, b, ref matches);
                removedThisTime.Add(new List<row> { a, b });
                if (_printActuals)
                {
                    PrintPair(a, b);
                }
            }

            return removedThisTime;
        }
    }
}
