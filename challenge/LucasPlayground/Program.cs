﻿using challenge;
using challenge.Ben;
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
        private static long[] _badPhones = new long[0];
        private static string[] _badAddresses = new string[0];
        private static DateTime[] _badDOBs = new DateTime[0];

        static IEnumerable<string> GetLines()
        {
            IEnumerable<string> lines = null;
            if (Environment.UserName.ToLower().Contains("sabalka"))
            {
                lines = File.ReadLines(@"C:/github/PMAC/FInalDataset.csv");
            }
            else if (Environment.UserName.ToLower().Contains("brush") ||
                Environment.UserName.ToLower().Contains("ben"))
            {
                lines = File.ReadLines(@"C:/users/ben/desktop/FInalDataset.csv");
            }

            return lines;
        }

        static void Main(string[] args)
        {
            Random random = new Random();

            var lines = GetLines();
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var data = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();

            Console.WriteLine(lines.Count() + " total rows"); // >= 15374761
            row[] remainingRows = data;
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();

            _badSSNs = data.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            _badDOBs = new DateTime[] { new DateTime(1900, 1, 1) };
            _badPhones = data.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();
            _badAddresses = data.GroupBy(r => r.ADDRESS1).Where(g => !g.Key.Contains(' ') && g.Count() > 2).Select(g => g.Key).ToArray();

            CleanData(ref data);


            //******************       MRN       ******************//
            Console.WriteLine();
            Console.WriteLine("MRN");
            AddMRNMatches(data, ref matches);
            //AddMRNMatchesBen(data, ref matches); 

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************       SSN       ******************//
            Console.WriteLine();
            Console.WriteLine("SSN");
            var addedSSN = AddMatches(data, r => r.SSN, 4, (r1, r2) =>
                    FuzzyStringMatch(r1.FIRST, r2.FIRST) ||
                    FuzzyStringMatch(r1.LAST, r2.LAST) ||
                    FuzzyPhoneMatch(r1.PHONE, r2.PHONE) ||
                    FuzzyDateEquals(r1.DOB, r2.DOB) ||
                    challenge.Program.FuzzyAddressMatch(r1, r2),
                ref matches);

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************  NAME + PHONE  ******************//
            Console.WriteLine();
            Console.WriteLine("NAME + PHONE");
            var addedNamePhone = AddMatches(data, r =>
                {
                    return (r.LAST != "" ? (r.PHONE > 0 ? r.LAST + r.FIRST + r.PHONE : "NOPHONE") : "NONAME");
                }, 4, (r1, r2) =>
                    !IsSSNValid(r1.SSN) ||
                    !IsSSNValid(r2.SSN) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN) ||
                    FuzzyDateEquals(r1.DOB, r2.DOB) ||
                    challenge.Program.FuzzyAddressMatch(r1, r2),
                ref matches);


            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************   NAME + DOB   ******************//
            Console.WriteLine();
            Console.WriteLine("NAME + DOB");
            var addedNameDOB = AddMatches(data, r =>
                {
                    return (r.LAST != "" ? (r.DOB != default(DateTime) ? r.LAST + r.FIRST + r.DOB.ToString("d") : "NODOB") : "NONAME");
                }, 4, (r1, r2) =>
                    !IsSSNValid(r1.SSN) ||
                    !IsSSNValid(r2.SSN) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN) ||
                    FuzzyPhoneMatch(r1.PHONE, r2.PHONE) ||
                    challenge.Program.FuzzyAddressMatch(r1, r2),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //****************** NAME + ADDRESS ******************//
            Console.WriteLine();
            Console.WriteLine("NAME + ADDRESS");
            var addedNameAddress = AddMatches(data, r =>
            {
                return (r.LAST != "" ? (r.ADDRESS1 != "" ? r.LAST + r.FIRST + r.ADDRESS1 : "NOADDRESS") : "NONAME");
            }, 4, (r1, r2) =>
                FuzzyPhoneMatch(r1.PHONE, r2.PHONE) ||
                FuzzySSNMatch(r1.SSN, r2.SSN) ||
                FuzzyDateEquals(r1.DOB, r2.DOB),
            ref matches);

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************     PHONE      ******************//
            Console.WriteLine();
            Console.WriteLine("PHONE");
            var addedPhone = AddMatches(data, r => r.PHONE, 5, (r1, r2) =>
                    FuzzyStringMatch(r1.FIRST, r2.FIRST) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN) ||
                    challenge.Program.DateSoftMatch(r1.DOB, r2.DOB),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //****************** ADDRESS + DOB  ******************//
            Console.WriteLine();
            Console.WriteLine("ADDRESS + DOB");
            var addedAddressDOB = AddMatches(data, r =>
                {
                    return (r.ADDRESS1 != "" ? (r.DOB != default(DateTime) ? r.DOB.ToString("d") + r.ADDRESS1 : "NODOB") : "NOADDRESS");
                }, 4, (r1, r2) =>
                    //(r1.LAST != "" && r1.LAST == r2.LAST) || 
                    //(r1.FIRST != "" && r1.FIRST == r2.FIRST) ||
                    FuzzyStringMatch(r1.FIRST, r2.FIRST) ||
                    FuzzySSNMatch(r1.SSN, r2.SSN) ||
                    (FuzzyStringMatch(r1.LAST, r2.LAST) && (r1.SSN == 0 || r2.SSN == 0)),
                ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************   SOFT MATCH   ******************//
            Console.WriteLine();
            Console.WriteLine("SOFT MATCH");
            var addedSoftMatches = AddSoftMatches(remainingRows, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************  HAND REMOVE   ******************//
            Console.WriteLine();
            Console.WriteLine("HAND REMOVED");
            var removedHandMatched = RemoveHandErrors(data, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine(remainingRows.Count());


            //******************  SOFT MATCH2   ******************//
            Console.WriteLine();
            Console.WriteLine("SOFT MATCH2");
            var addedSoftMatches2 = AddSoftMatches2(remainingRows, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);


            //******************   HAND MATCH   ******************//
            Console.WriteLine();
            Console.WriteLine("HAND MATCHED");
            var addedHandMatched = AddHandMatches(data, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine(remainingRows.Count());


            //****************** FINAL SOFTMATCH ******************//
            Console.WriteLine();
            Console.WriteLine("FINAL SOFTMATCH");
            var addedSoftMatchesFinal = AddSoftMatchesFinal(data, remainingRows, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            PrintAnalysis(matches, data);

            //Console.WriteLine("M/F matches");
            //int countMF = 0, countMFBad = 0;
            //List<List<row>> bad = new List<List<row>>();
            //var tc = TransitiveClosure.Compute(matches, data);
            //foreach (var match in tc.ClosedRowSets)
            //{
            //    List<row> rows = new List<row>();
            //    bool hasM = false;
            //    bool hasF = false;
            //    foreach (var id in match)
            //    {
            //        row row = data.Where(r => r.EnterpriseID == id).First();
            //        rows.Add(row);
            //        if (row.GENDER == "M")
            //        {
            //            hasM = true;
            //        }
            //        else if (row.GENDER == "F")
            //        {
            //            hasF = true;
            //        }
            //    }
            //    if (hasM && hasF)
            //    {
            //        countMF++;
            //        for (int i = 1; i < rows.Count; i++)
            //        {
            //            if (!(FuzzySSNMatch(rows[i - 1].SSN, rows[i].SSN) || FuzzyStringMatch(rows[i - 1].FIRST, rows[i].LAST)) || !FuzzyStringMatch(rows[i - 1].LAST, rows[i].LAST))
            //            {
            //                bad.Add(rows);
            //                if (_printActuals)
            //                {
            //                    PrintRows(rows);
            //                }
            //                countMFBad++;
            //                break;
            //            }
            //        }
            //    }
            //}
            //Console.WriteLine($"M/F count: {countMF} / {countMFBad}");

            SaveResults(matches, data);

            Console.ReadLine();
        }

        private static void SaveResults(Dictionary<int, List<int>> matches, row[] data)
        {
            string path = "";
            if (Environment.UserName.ToLower().Contains("sabalka"))
            {
                path = @"C:/github/MitchMatch/challenge/";
            }
            else if (Environment.UserName.ToLower().Contains("brush") ||
                Environment.UserName.ToLower().Contains("ben"))
            {
                path = @"C:/users/ben/desktop/";
            }
            path += "answers.csv";

            File.Delete(path);
            //File.Create("C:/github/MitchMatch/challenge/answers.csv");
            List<string> allLines = new List<string>();
            var tc = TransitiveClosure.Compute(matches, data);
            foreach (var match in tc.ClosedRowSets)
            {
                int[] matchOrdered = match.OrderBy(id => id).ToArray();
                for (int i = 0; i < matchOrdered.Length - 1; i++)
                {
                    for (int j = 0; j < matchOrdered.Length - 1; j++)
                    {
                        allLines.Add($"{matchOrdered[i]}, {matchOrdered[j]}, 1");
                    }
                }
            }
            File.AppendAllLines(path, allLines);
            Console.WriteLine($"{allLines.Count} lines written to {path} .");
        }



        private static void PrintAnalysis(Dictionary<int, List<int>> matches, row[] allTruePositiveData)
        {

            var tc = TransitiveClosure.Compute(matches, allTruePositiveData);

            var triplets = tc.ClosedRowSets.Where(s => s.Count() == 3).ToArray();

            //Validate that every triplet has a non-MRN entry
            Console.WriteLine("Likely false positive triplets:");
            Dictionary<int, row> _rowByEnterpriseId = new Dictionary<int, row>();
            foreach (var r in allTruePositiveData)
            {
                _rowByEnterpriseId[r.EnterpriseID] = r;
            }

            foreach (var triplet in triplets)
            {
                if (!triplet.Any(eid => _rowByEnterpriseId[eid].MRN == -1 && _rowByEnterpriseId[eid].PHONE == 0))
                {
                    Console.WriteLine();
                    foreach (var eid in triplet)
                    {
                        RowLibrary.Print(_rowByEnterpriseId[eid]);
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Possible false negatives, likely matches triple");
            var noSSNnoMRN = allTruePositiveData.Where(r => r.MRN == -1 && r.SSN == -1);
            foreach (var r in noSSNnoMRN)
            {
                if (!triplets.Any(t => t.Contains(r.EnterpriseID)))
                {
                    RowLibrary.Print(r);
                }
            }

            Console.WriteLine("\nUnmatched");
            var toHandVerify = challenge.Program.UnMatched(allTruePositiveData, matches);
            foreach (var row in toHandVerify)
            {
                RowLibrary.Print(row);
            }

            ////Generate 10 random triplets
            //for (int i = 0; i < 30; i++)
            //{
            //    int j = random.Next(triplets.Length);
            //    Console.WriteLine("\n");
            //    foreach(int eid in triplets[j])
            //    {
            //        RowLibrary.Print(_rowByEnterpriseId[eid]);
            //    }
            //}

            var bigComponents = tc.ClosedRowSets.Where(s => s.Count() > 3);

            Console.WriteLine($"{bigComponents.Count()} Large Components:");
            foreach (var component in bigComponents)
            {
                Console.WriteLine("\n");
                foreach (int id in component)
                {
                    RowLibrary.Print(allTruePositiveData.Where(r => r.EnterpriseID == id).First());
                }
                Console.WriteLine("\n");
            }

            Console.WriteLine("");
            Console.WriteLine(matches.Count() + " matched entries");
        }

        private static void CleanData(ref row[] data)
        {
            foreach (row row in data)
            {
                if (_badSSNs.Contains(row.SSN))
                {
                    row.SSN = 0;
                }
                if (_badPhones.Contains(row.PHONE))
                {
                    row.PHONE = 0;
                }
                if (_badAddresses.Contains(row.ADDRESS1))
                {
                    row.ADDRESS1 = "";
                }
            }
        }


        #region Printing
        private static bool _printLargeGroupValues = false;
        private static bool _printErrors = false;
        private static bool _printActuals = false;
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
        #endregion

        #region Soft Matching
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

        public static bool OneDifference(string sm, string sn)
        {
            return challenge.Program.KDifferences(sm, sn, 1);
        }

        public static bool OneOrOneDigit(int a, int b)
        {
            if (System.Math.Abs(a - b) < 2)
                return true;

            return OneDifference(a.ToString(), b.ToString());
        }

        public static bool TransposedDigit(int a, int b)
        {
            string sa = a.ToString();
            string sb = b.ToString();

            if (sa.Length != sb.Length)
            {
                return false;
            }

            bool possibleTransposition = true;
            bool transpositionDetected = false;
            for (int i = 0; i < sa.Length; i++)
            {
                if (sa[i] != sb[i])
                {
                    if (!transpositionDetected && i + 1 < sa.Length && sa[i] == sb[i + 1] && sb[i] == sa[i + 1])
                    {
                        transpositionDetected = true;
                        i++;
                    }
                    else
                    {
                        possibleTransposition = false;
                    }
                }
            }
            return transpositionDetected && possibleTransposition;
        }

        public static bool OffBy100(int a, int b)
        {
            return a == b - 100 || b == a - 100;
        }

        public static bool FuzzyDateEquals(DateTime a, DateTime b)
        {
            if (OneOrOneDigit(a.Month, b.Month) && a.Day == b.Day && a.Year == b.Year)
                return true;

            if (a.Month == b.Month && OneOrOneDigit(a.Day, b.Day) && a.Year == b.Year)
                return true;

            if (a.Month == b.Month && a.Day == b.Day && (OneOrOneDigit(a.Year, b.Year) || TransposedDigit(a.Year, b.Year) || OffBy100(a.Year, b.Year)))
                return true;

            if (a.Month == b.Day && a.Day == b.Month && a.Year == b.Year)
            {
                return true;
            }

            return false;
        }

        public static bool FuzzyPhoneMatch(long a, long b)
        {
            if (a == 0 || b == 0)
            {
                return false;
            }
            return challenge.Program.OneDifference(a.ToString(), a.ToString());
        }

        public static bool FuzzyStringMatch(string a, string b)
        {
            if (a == "" || b == "")
            {
                return false;
            }
            return challenge.Program.OneDifference(a, b);
        }
        #endregion

        #region Matching
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

            Console.WriteLine($"Thrown out: {counter}");
            return addedThisTime;
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
                    if (FuzzyStringMatch(ri.LAST, rj.LAST))
                    {
                        fieldAgreement++;

                        if (FuzzySSNMatch(ri.SSN, rj.SSN))
                            fieldAgreement++;

                        if (challenge.Program.FuzzyAddressMatch(ri, rj))
                            fieldAgreement++;

                        if (FuzzyDateEquals(ri.DOB, rj.DOB))
                            fieldAgreement++;
                    }
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

                    if (FuzzyDateEquals(ri.DOB, rj.DOB))
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

        public static List<List<row>> AddSoftMatchesFinal(row[] data, row[] remainingRows, ref Dictionary<int, List<int>> matches)
        {
            List<List<row>> addedThisTime = new List<List<row>>();

            for (int i = 0; i < remainingRows.Count(); i++)
            {
                for (int j = 0; j < data.Count(); j++)
                {
                    var ri = remainingRows[i];
                    var rj = data[j];
                    if (ri.EnterpriseID == rj.EnterpriseID)
                    {
                        continue;
                    }

                    int fieldAgreement = 0;

                    if (FuzzySSNMatch(ri.SSN, rj.SSN))
                        fieldAgreement++;

                    if (challenge.Program.KDifferences(ri.LAST, rj.LAST, 2))
                        fieldAgreement++;

                    if (challenge.Program.FuzzyAddressMatch(ri, rj))
                        fieldAgreement++;

                    if (FuzzyDateEquals(ri.DOB, rj.DOB))
                        fieldAgreement++;


                    if (NonemptyEquality(ri.FIRST, rj.FIRST))
                        fieldAgreement++;

                    if (fieldAgreement >= 1)
                    {
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

        private static List<List<row>> RemoveHandErrors(row[] data, ref Dictionary<int, List<int>> matches)
        {
            List<int[]> groups = new List<int[]>
            {
                new int[] { 15976198, 15988294, 15477018, 15922527 },
                //new int[] { 15943042, 15624558, 15836726, 15861073 },
                new int[] { 15736643, 15696925, 15884622, 15795257 },
                new int[] { 15755689, 15943418, 15455018, 15784375 },
                new int[] { 15567009, 15807734, 15799555, 15429342 },
            };

            List<List<row>> removedThisTime = new List<List<row>>();

            foreach (int[] row in groups)
            {
                row a = data.Where(r => r.EnterpriseID == row[0]).FirstOrDefault();
                row b = data.Where(r => r.EnterpriseID == row[1]).FirstOrDefault();
                row c = data.Where(r => r.EnterpriseID == row[2]).FirstOrDefault();
                row d = data.Where(r => r.EnterpriseID == row[3]).FirstOrDefault();

                Remove(a, b, ref matches);
                Remove(a, c, ref matches);
                Remove(a, d, ref matches);
                Remove(b, c, ref matches);
                Remove(b, d, ref matches);
                Remove(c, d, ref matches);
            }


            List<int[]> pairs = new List<int[]>
            {
                new int[] { 15976198, 15988294 },
                new int[] { 15477018, 15922527 },

                new int[] { 15943042, 15624558, 15836726, 15861073 },

                new int[] { 15736643, 15696925 },
                new int[] { 15736643, 15884622 },
                new int[] { 15696925, 15884622 },//
                new int[] { 15795257, 15467089 },
                new int[] { 15795257, 15459635 },

                new int[] { 15755689, 15943418 },
                new int[] { 15755689, 15455018 },
                new int[] { 15943418, 15455018 },//
                new int[] { 15784375, 15468600 },
                new int[] { 15784375, 15692153 },


                new int[] { 15567009, 15807734 },
                new int[] { 15799555, 15429342 },
            };

            foreach (int[] pair in pairs)
            {
                row a = data.Where(row => row.EnterpriseID == pair[0]).FirstOrDefault();
                row b = data.Where(row => row.EnterpriseID == pair[1]).FirstOrDefault();

                challenge.Program.Add(a, b, ref matches);
            }


            return removedThisTime;
        }
        #endregion
    }
}
