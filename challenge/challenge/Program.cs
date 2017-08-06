﻿using challenge.Ben;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class Program
    {
        static int[] _badSSNs;
        static Dictionary<int, row> _rowByEnterpriseId;
        static long[] _badPhoneNumbers;

        static void Main(string[] args)
        {
            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();

            Random random = new Random();

            var lines = File.ReadLines(@"C: \Users\jbrownkramer\Desktop\Data\data.csv");
            //var lines = File.ReadLines(@"C:/github/PMAC/FInalDataset.csv");
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var allTruePositiveData = allData.Where(r => r.EnterpriseID >= 15374761).ToArray();

            _rowByEnterpriseId = new Dictionary<int, row>();
            foreach (var r in allTruePositiveData)
            {
                _rowByEnterpriseId[r.EnterpriseID] = r;
            }

            _badSSNs = allTruePositiveData.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            _badPhoneNumbers = allTruePositiveData.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();



            Console.WriteLine(lines.Count() + " total rows"); // >= 15374761

            var fourMillion = allTruePositiveData.Where(r => r.MRN >= 4000000).ToArray();
            //Pair off and make a soft check on field to verify sameness
            Console.WriteLine(fourMillion.Count());

            for (int i = 0; i < fourMillion.Count(); i += 2)
            {
                var r = fourMillion[i];
                var s = fourMillion[i + 1];
                Add(r, s, ref matches);
            }


            var data = UnMatched(allTruePositiveData, matches);


            AddMatches(data, r => r.SSN, 4, (r1, r2) => true, ref matches);
            AddMatches(data, r => r.PHONE, 5, PhoneMatchConfidence, ref matches);
            AddMatches(data, r => r.LAST + r.FIRST + r.DOB.ToString("d"), 4, (r1, r2) => true, ref matches);

            AddMatches(data, r => r.DOB.ToString("d") + r.ADDRESS1, 4, AddressDOBMatchConfidence, ref matches);

            AddMatches(data, r => r.LAST + r.FIRST + r.ADDRESS1, 4, (r1, r2) => r1.ADDRESS1 != "" && r2.ADDRESS1 != "", ref matches);

            var remainingRows = UnMatched(data, matches);
            var fuzzyMatchResults = BipartiteMatch(remainingRows, remainingRows, FuzzyMatchNoFirst);
            AddMatchDictionary(fuzzyMatchResults, matches);

            //For what's left, brute force soft match on at least 2 of name, DOB, address


            var remainingRows2 = UnMatched(data, matches);
            var fuzzyMatch2Results = BipartiteMatch(remainingRows2, remainingRows2, FuzzyMatchAllowFirst);
            AddMatchDictionary(fuzzyMatch2Results, matches);


            Add(15811621, 15750288, ref matches);//
            Add(15802888, 15456558, ref matches);//
            Add(15510682, 15598625, ref matches);//
            Add(15562243, 15863734, ref matches);//
            Add(15843982, 15988253, ref matches);//
            Add(15447438, 16021452, ref matches);//
            Add(15566242, 15393356, ref matches);//
            Add(15869829, 15444537, ref matches);//
            Add(15483298, 15544065, ref matches);//
            Add(15380819, 15586885, ref matches);//
            Add(15474114, 15393886, ref matches);//
            Add(15476947, 15766192, ref matches);//
            Add(15671788, 15696806, ref matches);//
            Add(15476869, 15541825, ref matches);//
            Add(15460667, 15923220, ref matches);//
            Add(15688015, 15555730, ref matches);//

            MRNFalsePositiveAnalysis(matches, _rowByEnterpriseId);

            var tc = TransitiveClosure.Compute(matches, allTruePositiveData);


            var triplets = tc.ClosedRowSets.Where(s => s.Count() == 3).ToArray();
            Console.WriteLine(triplets.Count());
            Console.WriteLine(allTruePositiveData.Where(r => r.MRN == -1 && r.SSN == -1).Count());

            //Validate that every triplet has a non-MRN entry
            Console.WriteLine("Likely false positive triplets:");
            foreach (var triplet in triplets)
            {
                if (!triplet.Any(eid => _rowByEnterpriseId[eid].MRN == -1 && _rowByEnterpriseId[eid].PHONE == -1))
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
            var toHandVerify = UnMatched(data, matches);
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

            Console.ReadLine();
        }

        public static void MRNFalsePositiveAnalysis(Dictionary<int, List<int>> matches, Dictionary<int,row> rowByEnterpriseId)
        {
            List<Tuple<row, row>> tuples = new List<Tuple<row, row>>();

            int c = 0;
            foreach(var pair in matches)
            {
                Console.Write($"\r{c++}/{matches.Count()}");
                var r1 = rowByEnterpriseId[pair.Key];
                foreach(var r2Id in pair.Value)
                {
                    var r2 = rowByEnterpriseId[r2Id];

                    if (r1.MRN != -1 && r2.MRN != -1 && r1.MRN < r2.MRN && System.Math.Abs(r1.MRN - r2.MRN ) > 90000)
                    {
                        tuples.Add(new Tuple<row, row>(r1, r2));
                    }
                }
            }
            Console.WriteLine("\nPossible False Positive, due to large MRN spread");

            tuples = tuples.OrderBy(p => System.Math.Abs(p.Item1.MRN - p.Item2.MRN)).ToList();

            foreach(var pair in tuples)
            {
                RowLibrary.Print(pair.Item1);
                RowLibrary.Print(pair.Item2);
                Console.WriteLine(pair.Item2.MRN - pair.Item1.MRN + "\n");
            }
        }

        static bool M(row r, row s, Func<row, string> f)
        {
            string rs = f(r);
            string ss = f(s);

            if (rs != "" && ss != "" && rs == ss)
                return true;

            return false;
        }

        static bool SoftM(row r, row s, Func<row, string> f)
        {
            string rs = f(r);
            string ss = f(s);

            if (rs != "" && ss != "" && OneDifference(rs, ss))
                return true;

            return false;
        }

        static bool AddressDOBMatchConfidence(row r, row s)
        {
            if (r.ADDRESS1 == "" || s.ADDRESS1 == "")
                return false;

            if (!_badSSNs.Contains(r.SSN) && !_badSSNs.Contains(s.SSN) && OneDifference(r.SSN.ToString(), s.SSN.ToString()))
                return true;

            return (M(r, s, t => t.FIRST) || M(r, s, t => t.LAST));
        }



        static bool PhoneMatchConfidence(row r, row s)
        {
            if (!_badSSNs.Contains(r.SSN) && !_badSSNs.Contains(s.SSN) && OneDifference(r.SSN.ToString(), s.SSN.ToString()))
                return true;

            return SoftM(r, s, t => t.DOB.ToString()) || SoftM(r, s, t => t.FIRST);
        }

        public static bool FuzzyMatchNoFirst(row ri, row rj)
        {
            return FuzzyMatch(ri, rj, false);
        }

        public static bool FuzzyMatchAllowFirst(row ri, row rj)
        {
            return FuzzyMatch(ri, rj, true);
        }

        public static bool FuzzyMatch(row ri, row rj, bool useFirst)
        {
            int fieldAgreement = 0;

            if (!_badSSNs.Contains(ri.SSN) && !_badSSNs.Contains(rj.SSN) && OneDifference(ri.SSN.ToString(), rj.SSN.ToString()))
                fieldAgreement++;

            if (!_badPhoneNumbers.Contains(ri.PHONE) && !_badPhoneNumbers.Contains(rj.PHONE) && OneDifference(ri.PHONE.ToString(), rj.PHONE.ToString()))
                fieldAgreement++;

            if (KDifferences(ri.LAST, rj.LAST, 2))
                fieldAgreement++;

            if (FuzzyAddressMatch(ri, rj))
                fieldAgreement++;

            if (FuzzyDateEquals(ri.DOB, rj.DOB))
                fieldAgreement++;

            if (useFirst && KDifferences(ri.FIRST, rj.FIRST, 2))
                fieldAgreement++;

            return fieldAgreement >= 2;
        }

        public static bool ExactMatchInTwoFields(row ri, row rj)
        {
            int fieldAgreement = 0;

            if (!_badSSNs.Contains(ri.SSN) && !_badSSNs.Contains(rj.SSN) && ri.SSN == rj.SSN)
                fieldAgreement++;

            if (!_badPhoneNumbers.Contains(ri.PHONE) && !_badPhoneNumbers.Contains(rj.PHONE) && ri.PHONE.ToString() == rj.PHONE.ToString())
                fieldAgreement++;

            if (M(ri, rj, r => r.FIRST) || M(ri, rj, r => r.LAST))
                fieldAgreement++;

            if (M(ri, rj, r => r.ADDRESS1))
                fieldAgreement++;

            if (ri.DOB != default(DateTime) && rj.DOB != default(DateTime) && ri.DOB.ToString() == rj.DOB.ToString())
                fieldAgreement++;

            return fieldAgreement >= 2;
        }

        public static bool FuzzyAddressMatch(row a, row b)
        {
            if (a.ADDRESS1 == "" || b.ADDRESS1 == "")
                return false;
            if (a.ADDRESS1 == b.ADDRESS1)
                return true;

            var anums = NumericParts(a.ADDRESS1);
            var bnums = NumericParts(b.ADDRESS1);

            if (anums.Count != bnums.Count)
                return false;

            if (anums.Count == 0)
                return false;

            for (int i = 0; i < anums.Count; i++)
            {
                if (anums[i] != bnums[i])
                    return false;
            }

            return true;
        }

        public static List<string> NumericParts(string s)
        {
            List<string> toReturn = new List<string>();
            string current = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                {
                    current += s[i];
                }
                else if (current != "")
                {
                    toReturn.Add(current);
                    current = "";
                }
            }

            if (current != "")
            {
                toReturn.Add(current);
            }

            return toReturn;
        }

        public static Dictionary<int, List<int>> BipartiteMatch(IEnumerable<row> S, IEnumerable<row> T, Func<row, row, bool> isMatch, bool printPairs = false)
        {
            Dictionary<int, List<int>> toReturn = new Dictionary<int, List<int>>();

            int c = 0;
            foreach (var s in S)
            {
                if (!printPairs)
                    Console.Write($"\r{c++}/{S.Count()}");
                int nMatches = 0;
                foreach (var t in T)
                    if (s != t && isMatch(s, t))
                    {
                        nMatches++;
                        Add(s, t, ref toReturn);
                    }
                if (printPairs && nMatches == 1)
                    RowLibrary.Print(s);
            }
            Console.WriteLine();
            return toReturn;
        }

        static void AddMatchDictionary(Dictionary<int, List<int>> toAdd, Dictionary<int, List<int>> matches)
        {
            foreach (var pair in toAdd)
            {
                foreach (int x in pair.Value)
                {
                    Add(pair.Key, x, ref matches);
                }
            }
        }


        public static void Add(row a, row b, ref Dictionary<int, List<int>> matches)
        {
            Add(a.EnterpriseID, b.EnterpriseID, ref matches);
        }

        public static void Add(int a, int b, ref Dictionary<int, List<int>> matches)
        {
            AddOrdered(a, b, ref matches);
            AddOrdered(b, a, ref matches);
        }

        static void AddOrdered(int a, int b, ref Dictionary<int, List<int>> matches)
        {
            if (a == b)
                throw new ArgumentException("A record can't match itself");

            if (!matches.ContainsKey(a))
                matches[a] = new List<int>();

            matches[a].Add(b);

            matches[a] = matches[a].Distinct().ToList();
        }

        public static row[] UnMatched(IEnumerable<row> data, Dictionary<int, List<int>> matches)
        {
            return data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
        }

        static void AddMatches<T>(IEnumerable<row> data, Func<row, T> groupingValue, int sizeToThrowAway, Func<row, row, bool> softEquals, ref Dictionary<int, List<int>> matches)

        {
            var grouped = data.GroupBy(groupingValue);
            //Console.WriteLine(grouped.Where(g => g.Count() >= sizeToThrowAway).Count());
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
                if (group.Any(r => !softEquals(r, representative)))
                {
                    //Console.WriteLine(group.Key);
                    counter++;
                }
                else
                {
                    foreach (var r in group)
                    {
                        foreach (var s in group)
                        {
                            if (r != s)
                                Add(r, s, ref matches);
                        }
                    }
                }
            }

            //Console.WriteLine(counter);
        }

        public static bool PartialMatch(row a, row b)
        {
            return (a.FIRST != "" && a.FIRST == b.FIRST) || (a.LAST != "" && a.LAST == b.LAST) || DateSoftMatch(a.DOB, b.DOB);
        }

        public static bool DateSoftMatch(DateTime a, DateTime b)
        {
            if (a == default(DateTime) || b == default(DateTime))
                return false;

            return OneDifference(a.ToString("d"), b.ToString("d"));

        }

        public static bool FuzzyDateEquals(DateTime a, DateTime b)
        {
            if (OneOrOneDigit(a.Month, b.Month) && a.Day == b.Day && a.Year == b.Year)
                return true;

            if (a.Month == b.Month && OneOrOneDigit(a.Day, b.Day) && a.Year == b.Year)
                return true;

            if (a.Month == b.Month && a.Day == b.Day && OneOrOneDigit(a.Year, b.Year))
                return true;

            return false;
        }

        public static bool OneOrOneDigit(int a, int b)
        {
            if (System.Math.Abs(a - b) < 2)
                return true;

            return OneDifference(a.ToString(), b.ToString());
        }

        public static bool OneDifference(string sm, string sn)
        {
            return KDifferences(sm, sn, 1);
        }

        public static bool KDifferences(string sm, string sn, int k)
        {
            if (sm.Length != sn.Length)
                return false;

            int nd = 0;
            for (int i = 0; i < sn.Length; i++)
            {
                if (sm[i] != sn[i])
                    nd++;
            }

            return nd <= k;
        }

        static int MatchExtent(row a, row b)
        {
            int toReturn = 0;
            if (PositiveMatch(a.SSN, b.SSN))
                toReturn += 16;
            if (PositiveMatch(a.PHONE, b.PHONE))
                toReturn += 8;
            if (Name(a) == Name(b))
                toReturn += 4;

            return toReturn;

            if (a.ADDRESS1 == b.ADDRESS1)
                toReturn += 2;
            if (a.DOB == b.DOB)
                toReturn += 1;

            return toReturn;
        }

        static string Name(row a)
        {
            return a.FIRST + " " + a.LAST;
        }

        static bool PositiveMatch(long a, long b)
        {
            return a > 0 && b > 0 && a == b;
        }
    }


    public static class RowLibrary
    {
        static int ParseInt(string s)
        {
            string digits = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                    digits += s[i];
            }

            if (digits == "")
            {
                return -1;
            }


            return int.Parse(digits);
        }

        static long ParseLong(string s)
        {
            string digits = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                    digits += s[i];
            }

            if (digits == "")
            {
                return -1;
            }

            long toReturn;
            if (long.TryParse(digits, out toReturn))
            {
                return toReturn;
            }
            else
            {
                Console.WriteLine(s);
                return -1;
            }
        }

        static DateTime ParseDate(string s)
        {
            DateTime toReturn;
            if (s.Trim() == "")
                return default(DateTime);

            if (!DateTime.TryParse(s, out toReturn))
            {
                Console.WriteLine("Bad date : " + s);
                return default(DateTime);
            }
            else
            {
                return toReturn;
            }
        }

        static string[] Tokens(string line)
        {
            return line.Split(',');
        }

        public static row ParseRow(string line)
        {
            var tokens = Tokens(line);
            row toReturn = new row
            {
                EnterpriseID = ParseInt(tokens[0]),
                LAST = tokens[1].Trim(),
                FIRST = tokens[2].Trim(),
                MIDDLE = tokens[3].Trim(),
                SUFFIX = tokens[4].Trim(),
                DOB = ParseDate(tokens[5]),
                GENDER = tokens[6].Trim(),
                SSN = ParseInt(tokens[7]),
                ADDRESS1 = tokens[8].Trim(),
                ADDRESS2 = tokens[9].Trim(),
                ZIP = ParseInt(tokens[10]),
                MOTHERS_MAIDEN_NAME = tokens[11].Trim(),
                MRN = ParseInt(tokens[12]),
                CITY = tokens[13].Trim(),
                STATE = tokens[14].Trim(),
                PHONE = ParseLong(tokens[15]),
                PHONE2 = tokens[16].Trim(),
                EMAIL = tokens[17].Trim(),
                ALIAS = tokens[18].Trim()
            };

            return toReturn;
        }

        public static void Print(this row row)
        {
            string ssn = row.SSN <= 0 ? "   -  -    " : string.Format("{0:000-00-0000}", row.SSN);
            string phone = row.PHONE <= 0 ? "   -   -    " : string.Format("{0:000-000-0000}", row.PHONE);
            Console.WriteLine("{0,-15} {1, -15} {2,-20} {3, -3} AKA {14, -25} ({4,-1}): S:{5}  {6}  P:{7}. {9,25}, {10,5}, {11,10}, {12,2}, {13,5}. {17, -10}, {18}",
                row.FIRST,
                row.MIDDLE,
                row.LAST,
                row.SUFFIX,
                row.GENDER,
                ssn,
                row.DOB.ToString("dd/MM/yyyy"),
                phone,
                row.PHONE2,//
                row.ADDRESS1,
                row.ADDRESS2,
                row.CITY,
                row.STATE,
                row.ZIP,
                row.ALIAS,
                row.MOTHERS_MAIDEN_NAME,//
                row.EMAIL,//
                row.MRN,
                row.EnterpriseID);
        }
    }

    public class row
    {
        public int EnterpriseID;
        public string LAST;
        public string FIRST;
        public string MIDDLE;
        public string SUFFIX;
        public DateTime DOB;
        public string GENDER;
        public int SSN;
        public string ADDRESS1;
        public string ADDRESS2;
        public int ZIP;
        public string MOTHERS_MAIDEN_NAME;
        public int MRN;
        public string CITY;
        public string STATE;
        public long PHONE;
        public string PHONE2;
        public string EMAIL;
        public string ALIAS;

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
                FIRST,
                MIDDLE,
                LAST,
                SUFFIX,
                GENDER,
                SSN,
                DOB.ToString("dd/MM/yyyy"),
                PHONE,
                PHONE2,//
                ADDRESS1,
                ADDRESS2,
                CITY.Replace("\"",""),
                STATE.Replace("\"", ""),
                ZIP,
                MOTHERS_MAIDEN_NAME,//
                EMAIL,//
                MRN,
                EnterpriseID,
                ALIAS);
        }
    }
}
