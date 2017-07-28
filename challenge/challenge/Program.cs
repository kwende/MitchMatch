using challenge.Ben;
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

            if (rs != "" && ss != "" && OneDifference(rs,ss))
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

        static void Main(string[] args)
        {
            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();

            Random random = new Random();

            //var lines = File.ReadLines(@"C: \Users\jbrownkramer\Desktop\Data\data.csv");
            var lines = File.ReadLines(@"C:/github/PMAC/FInalDataset.csv");
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var allTruePositiveData = allData.Where(r => r.EnterpriseID >= 15374761).ToArray();
            Console.WriteLine(lines.Count() + " total rows"); // >= 15374761

            _badSSNs = allTruePositiveData.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();

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

            //For what's left, brute force soft match on at least 2 of name, DOB, address
            for (int i = 0; i < remainingRows.Count(); i++)
            {
                Console.Write("\r" + i + "/" + remainingRows.Count());
                for (int j = 0; j < remainingRows.Count(); j++)
                {
                    if (i == j)
                        continue;

                    int fieldAgreement = 0;

                    var ri = remainingRows[i];
                    var rj = remainingRows[j];

                    if (!_badSSNs.Contains(ri.SSN) && !_badSSNs.Contains(rj.SSN) && OneDifference(ri.SSN.ToString(), rj.SSN.ToString()))
                        fieldAgreement++;

                    if (KDifferences(ri.LAST, rj.LAST, 2))
                        fieldAgreement++;

                    if (FuzzyAddressMatch(ri, rj))
                        fieldAgreement++;

                    if (FuzzyDateEquals(ri.DOB, rj.DOB))
                        fieldAgreement++;

                    if (fieldAgreement >= 2)
                    {
                        if (!matches.ContainsKey(ri.EnterpriseID))
                        {
                            Add(ri, rj, ref matches);
                        }
                    }
                }
            }

            var remainingRows2 = UnMatched(data, matches);

            for (int i = 0; i < remainingRows2.Count(); i++)
            {
                Console.Write("\r" + i + "/" + remainingRows.Count());
                for (int j = 0; j < remainingRows2.Count(); j++)
                {
                    if (i == j)
                        continue;

                    int fieldAgreement = 0;

                    var ri = remainingRows2[i];
                    var rj = remainingRows2[j];

                    if (!_badSSNs.Contains(ri.SSN) && !_badSSNs.Contains(rj.SSN) && OneDifference(ri.SSN.ToString(), rj.SSN.ToString()))
                        fieldAgreement++;

                    if (KDifferences(ri.LAST, rj.LAST, 2))
                        fieldAgreement++;

                    if (M(ri, rj, r => r.FIRST))
                        fieldAgreement++;

                    if (FuzzyAddressMatch(ri, rj))
                        fieldAgreement++;

                    if (FuzzyDateEquals(ri.DOB, rj.DOB))
                        fieldAgreement++;

                    if (fieldAgreement >= 2)
                    {
                        if (!matches.ContainsKey(ri.EnterpriseID))
                        {
                            Add(ri, rj, ref matches);
                        }
                    }
                }
            }

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

            var tc = TransitiveClosure.Compute(matches, allTruePositiveData);
            var bigComponents = tc.ClosedRowSets.Where(s => s.Count() > 3);
            Console.WriteLine("\n" + bigComponents.Count());
            Console.WriteLine(tc.ClosedRowSets.Max(s => s.Count()));
            Console.WriteLine(bigComponents.Sum(s => s.Count()));

            foreach (var component in bigComponents)
            {
                Console.WriteLine("\n");
                foreach (int id in component)
                {
                    RowLibrary.Print(allTruePositiveData.Where(r => r.EnterpriseID == id).First());
                }
                Console.WriteLine("\n");
            }

            var toHandVerify = UnMatched(data, matches);
            foreach (var row in toHandVerify)
            {
                RowLibrary.Print(row);
            }

            Console.WriteLine("");
            Console.WriteLine(matches.Count() + " matched entries");


            for (int i = 0; i < 10; i++)
            {
                int nextTry = random.Next(data.Count());
                while (matches.ContainsKey(data[nextTry].EnterpriseID))
                {
                    nextTry = random.Next(data.Count());
                }
                Console.WriteLine(data[nextTry].EnterpriseID);
            }

            int nPaired = 0;
            int nUniquelyPaired = 0;




            for (int i = 0; i < data.Count(); i++)
            {
                int maxExtent = 0;
                int nMaxMatches = 0;
                for (int j = 0; j < data.Count(); j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    int matchExtent = MatchExtent(data[i], data[j]);

                    if (matchExtent > maxExtent)
                    {
                        maxExtent = matchExtent;
                        nMaxMatches = 0;
                    }

                    if (matchExtent == maxExtent && matchExtent > 0)
                    {
                        nMaxMatches++;
                    }
                }

                if (nMaxMatches > 0)
                    nPaired++;
                else
                {
                    Console.WriteLine(data[i].EnterpriseID);
                }

                if (nMaxMatches == 1)
                    nUniquelyPaired++;

                if (i % 100 == 0)
                    Console.Write("\r" + (i + 1) + " " + nPaired + " " + nUniquelyPaired);
            }
            Console.ReadLine();
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

        static List<string> NumericParts(string s)
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
            if (!matches.ContainsKey(a))
                matches[a] = new List<int>();

            matches[a].Add(b);

            matches[a] = matches[a].Distinct().ToList();
        }

        static row[] UnMatched(IEnumerable<row> data, Dictionary<int, List<int>> matches)
        {
            return data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
        }

        static void AddMatches<T>(IEnumerable<row> data, Func<row, T> groupingValue, int sizeToThrowAway, Func<row, row, bool> softEquals, ref Dictionary<int, List<int>> matches)

        {
            var grouped = data.GroupBy(groupingValue);
            Console.WriteLine(grouped.Where(g => g.Count() >= sizeToThrowAway).Count());
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
                        if (!matches.ContainsKey(r.EnterpriseID))
                        {
                            matches[r.EnterpriseID] = new List<int>();
                        }
                        matches[r.EnterpriseID] = matches[r.EnterpriseID].Union(group.Select(r2 => r2.EnterpriseID)).ToList();
                    }
                }
            }

            Console.WriteLine(counter);
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

        static bool OneOrOneDigit(int a, int b)
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
            return string.Format("{0,-15}, {1, -15}, {2,-20}, {3, -3}, {14, -25}, {4,-1}, {5},  {6},  {7}, {9,25}, {10,5}, {11,10}, {12,2}, {13,5}, {17, -10}, {18}",
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
                CITY,
                STATE,
                ZIP,
                ALIAS,
                MOTHERS_MAIDEN_NAME,//
                EMAIL,//
                MRN,
                EnterpriseID);
        }
    }
}
