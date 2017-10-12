using Common;
using DecisionTreeLearner.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class MatchingManager
    {
        private static List<string> _newMatchingRows = new List<string>();
        private static bool _printErrors = false;
        private static bool _printActuals = false;
        private static bool _printLargeGroupValues = false;

        public MatchingManager(bool printErrors, bool printActuals, bool printLargeGroupValues)
        {
            _printErrors = printErrors;
            _printActuals = printActuals;
            _printLargeGroupValues = printLargeGroupValues;
        }







        static void AddPairs(List<Tuple<int, int>> pairs, ref Dictionary<int, List<int>> pairDictionary)
        {
            foreach (var pair in pairs)
            {
                if (!pairDictionary.ContainsKey(pair.Item1))
                {
                    pairDictionary[pair.Item1] = new List<int>();
                }

                if (!pairDictionary.ContainsKey(pair.Item2))
                {
                    pairDictionary[pair.Item2] = new List<int>();
                }

                if (!pairDictionary[pair.Item1].Contains(pair.Item2))
                {
                    pairDictionary[pair.Item1].Add(pair.Item2);
                }

                if (!pairDictionary[pair.Item2].Contains(pair.Item1))
                {
                    pairDictionary[pair.Item2].Add(pair.Item1);
                }
            }
        }

        static void WritePairs(Dictionary<int, List<int>> pairDictionary, string path)
        {
            List<string> lines = new List<string>();
            foreach (var keyValuePair in pairDictionary)
            {
                foreach (var eid2 in keyValuePair.Value)
                {
                    if (keyValuePair.Key < eid2)
                    {
                        string line = $"{keyValuePair.Key},{eid2},1";
                        lines.Add(line);
                    }
                }
            }

            File.WriteAllLines(path, lines);
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

        public static bool IsSSNValid(int a)
        {
            return a > 0;
        }

        public static bool FuzzySSNNoConflict(int a, int b)
        {
            return !IsSSNValid(a) ||
                    !IsSSNValid(b) ||
                    FuzzySSNMatch(a, b);

        }

        public static bool FuzzySSNMatch(int a, int b)
        {
            return IsSSNValid(a) && IsSSNValid(b) && OneDifference(a.ToString(), b.ToString());
        }

        public static bool FuzzierSSNMatch(int a, int b)
        {
            return IsSSNValid(a) && IsSSNValid(b) && EditDistanceEngine.Compute(a.ToString(), b.ToString()) <= 2;
        }

        public static bool FuzzyPhoneMatch(int a, int b)
        {
            return IsPhoneValid(a) && IsPhoneValid(b) && OneDifference(a.ToString(), b.ToString());
        }

        public static bool IsPhoneValid(int a)
        {
            return a > 0;
        }

        public static bool NonemptyEquality(string a, string b)
        {
            return a != "" && b != "" && a == b;
        }

        public static bool OneOrOneDigit(int a, int b)
        {
            if (System.Math.Abs(a - b) < 2)
                return true;

            return EditDistanceEngine.Compute(a.ToString(), b.ToString()) <= 1;
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
            if (a == default(DateTime) || b == default(DateTime))
                return false;

            if (a.Month == b.Day && a.Day == b.Month && a.Year == b.Year)
                return true;
            if (a.Month == b.Month && a.Year == b.Year && (OneOrOneDigit(a.Day, b.Day) || TransposedDigit(a.Day, b.Day)))
                return true;
            if (a.Day == b.Day && a.Year == b.Year && (OneOrOneDigit(a.Month, b.Month) || TransposedDigit(a.Month, b.Month)))
                return true;
            if (a.Month == b.Month && a.Day == b.Day && (OneOrOneDigit(a.Year, b.Year) || TransposedDigit(a.Year, b.Year) || OffBy100(a.Year, b.Year)))
                return true;

            return false;
        }

        public static bool FuzzyPhoneMatch(long a, long b)
        {
            if (a == 0 || b == 0)
            {
                return false;
            }
            return OneDifference(a.ToString(), b.ToString());
        }

        public static bool FuzzyAddressMatchEditDistance(Row a, Row b)
        {
            if (a.ADDRESS1 == "" || b.ADDRESS1 == "")
                return false;
            if (a.ADDRESS1 == b.ADDRESS1)
                return true;

            int editDistance = EditDistanceEngine.Compute(a.ADDRESS1, b.ADDRESS1);

            return editDistance <= 1;
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

        public static bool FuzzyAddressMatchNumber(string addressA, string addressB)
        {
            if (addressA == "" || addressB == "")
                return false;
            if (addressA == addressB)
                return true;

            var anums = NumericParts(addressA);
            var bnums = NumericParts(addressB);

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

        public static bool FuzzyNameMatch(string af, string al, string bf, string bl)
        {
            return FuzzyStringMatch(af, bf) && FuzzyStringMatch(al, bl);
        }

        public static bool FuzzyStringMatch(string a, string b)
        {
            if (a == "" || b == "")
            {
                return false;
            }
            return OneDifference(a, b);
        }


        public static int SoftMatchCount(Row a, Row b, FieldInclusions toInclude)
        {
            int toReturn = 0;
            if (toInclude.SSN && FuzzySSNMatch(a.SSN, b.SSN))
                toReturn++;
            if (toInclude.Name && FuzzyStringMatch(a.FIRST + a.LAST, b.FIRST + b.LAST))
                toReturn++;
            if (toInclude.First && FuzzyStringMatch(a.FIRST, b.FIRST))
                toReturn++;
            if (toInclude.Last && FuzzyStringMatch(a.LAST, b.LAST))
                toReturn++;
            if (toInclude.DOB && FuzzyDateEquals(a.DOB, b.DOB))
                toReturn++;
            if (toInclude.Phone && FuzzyPhoneMatch(a.PHONE, b.PHONE))
                toReturn++;
            if (toInclude.Address && FuzzyAddressMatchEditDistance(a, b))
                toReturn++;
            if (toInclude.SSNSoft && FuzzySSNNoConflict(a.SSN, b.SSN)) // Counts SSN carrying no information as a match.
                toReturn++;
            if (toInclude.AddressSoft && FuzzyAddressMatchNumber(a.ADDRESS1, b.ADDRESS1))
                toReturn++;
            if (toInclude.Alias && FuzzyStringMatch(a.ALIAS, b.FIRST + " " + b.LAST) || (b.MIDDLE != "" && FuzzyStringMatch(a.ALIAS, b.FIRST + " " + b.MIDDLE + " " + b.LAST)) || FuzzyStringMatch(b.ALIAS, a.FIRST + " " + a.LAST) || (a.MIDDLE != "" && FuzzyStringMatch(b.ALIAS, a.FIRST + " " + a.MIDDLE + " " + a.LAST)))
                toReturn++;
            return toReturn;
        }

        public static string HardSelector(Row row, FieldInclusions toInclude)
        {
            string toReturn = "";
            if (toInclude.First || toInclude.Name)
            {
                if (row.FIRST == "")
                {
                    return "BADFORMAT";
                }
                toReturn += row.FIRST;
            }
            if (toInclude.Last || toInclude.Name)
            {
                if (row.LAST == "")
                {
                    return "BADFORMAT";
                }
                toReturn += row.LAST;
            }
            if (toInclude.SSN)
            {
                if (row.SSN == 0)
                {
                    return "BADFORMAT";
                }
                toReturn += row.SSN;
            }
            if (toInclude.DOB)
            {
                if (row.DOB == default(DateTime))
                {
                    return "BADFORMAT";
                }
                toReturn += row.DOB;
            }
            if (toInclude.Phone)
            {
                if (row.PHONE == 0)
                {
                    return "BADFORMAT";
                }
                toReturn += row.PHONE;
            }
            if (toInclude.Address)
            {
                if (row.ADDRESS1 == "")
                {
                    return "BADFORMAT";
                }
                toReturn += row.ADDRESS1;
            }
            if (toInclude.Email)
            {
                if (row.EMAIL == "")
                {
                    return "BADFORMAT";
                }
                toReturn += row.EMAIL;
            }
            return toReturn;
        }


        public List<int> AddMatches(string label, Row[] data, Func<Row, string> groupingValue, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches, int originalNumberOfMatches)
        {
            _newMatchingRows.Add($"STARTING {label} MATCHES");

            List<int> toReturn = new List<int>();

            Console.WriteLine();
            Console.WriteLine(label);

            var grouped = data.GroupBy(groupingValue);

            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;
            foreach (var group in grouped)
            {
                if (group.Count() > 100)
                {
                    if (_printLargeGroupValues)
                    {
                        Console.WriteLine(group.Key);
                    }
                    continue;
                }

                if (group.Count() < 2)
                {
                    continue;
                }
                if (group.Key == "BADFORMAT") // Skip unentered or cleaned data
                {
                    continue;
                }

                // Loop over each pair in the group, test soft equality, and add if appropriate
                foreach (Row row1 in group)
                {
                    foreach (Row row2 in group)
                    {
                        if (row2 != row1)
                        {
                            if (softEquals(row1, row2) >= softEqualsCount)
                            {
                                if (matches.AddMatch(row1, row2))
                                {
                                    addedCounter++;

                                    if (_printActuals)
                                    {
                                        //PrintingLibrary.PrintPair(row1, row2);
                                        _newMatchingRows.Add(row1.ToString());
                                        _newMatchingRows.Add(row2.ToString());
                                        _newMatchingRows.Add("");
                                    }
                                }

                            }
                            else
                            {
                                thrownOutCounter++;

                                if (_printErrors)
                                {
                                    PrintingLibrary.PrintPair(row1, row2);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");
            Console.WriteLine($"Cumulative Matches Found: {matches.NumberOfMatches - originalNumberOfMatches}");

            PrintingLibrary.PrintRemainingRowCount(data, matches);

            return toReturn;
        }


        public List<int> AddMatchesPhone2(string label, Func<Row, string> groupingValue, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches, int originalNumberOfMatches)
        {
            _newMatchingRows.Add($"STARTING {label} MATCHES");

            List<int> toReturn = new List<int>();

            Console.WriteLine();
            Console.WriteLine(label);

            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;

            foreach (Row[] pair in _phone2PairRows)
            {
                // Loop over each pair in the group, test soft equality, and add if appropriate
                Row row1 = pair[0];
                Row row2 = pair[1];

                if (groupingValue(row1) != "BADFORMAT" && groupingValue(row1) == groupingValue(row2))
                {
                    if (softEquals(row1, row2) >= softEqualsCount)
                    {
                        if (matches.AddMatch(row1, row2))
                        {
                            addedCounter++;

                            if (_printActuals)
                            {
                                //PrintingLibrary.PrintPair(row1, row2);
                                _newMatchingRows.Add(row1.ToString());
                                _newMatchingRows.Add(row2.ToString());
                                _newMatchingRows.Add("");
                            }
                        }
                    }
                    else
                    {
                        thrownOutCounter++;

                        if (_printErrors)
                        {
                            PrintingLibrary.PrintPair(row1, row2);
                        }
                    }
                }
                else
                {
                    thrownOutCounter++;

                    if (_printErrors)
                    {
                        PrintingLibrary.PrintPair(row1, row2);
                    }
                }
            }

            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");
            Console.WriteLine($"Cumulative Matches Found: {matches.NumberOfMatches - originalNumberOfMatches}");

            return toReturn;
        }

        public List<int> AddMatchesAlias(string label, Func<Row, string> groupingValue, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches, int originalNumberOfMatches)
        {
            _newMatchingRows.Add($"STARTING {label} MATCHES");

            List<int> toReturn = new List<int>();

            Console.WriteLine();
            Console.WriteLine(label);

            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;

            foreach (Row[] pair in _aliasPairs)
            {
                // Loop over each pair in the group, test soft equality, and add if appropriate
                Row row1 = pair[0];
                Row row2 = pair[1];

                if (groupingValue(row1) != "BADFORMAT" && groupingValue(row1) == groupingValue(row2) && row1.ALIAS != row1.FIRST && row1.ALIAS != row1.LAST && row2.ALIAS != row2.FIRST && row2.ALIAS != row2.LAST && row1.ALIAS.Contains(" "))
                {
                    if (softEquals(row1, row2) >= softEqualsCount)
                    {
                        if (matches.AddMatch(row1, row2))
                        {
                            addedCounter++;

                            if (_printActuals)
                            {
                                //PrintingLibrary.PrintPair(row1, row2);
                                _newMatchingRows.Add(row1.ToString());
                                _newMatchingRows.Add(row2.ToString());
                                _newMatchingRows.Add("");
                            }
                        }
                    }
                    else
                    {
                        thrownOutCounter++;

                        if (_printErrors)
                        {
                            PrintingLibrary.PrintPair(row1, row2);
                        }
                    }
                }
                else
                {
                    thrownOutCounter++;

                    if (_printErrors)
                    {
                        PrintingLibrary.PrintPair(row1, row2);
                    }
                }
            }

            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");
            Console.WriteLine($"Cumulative Matches Found: {matches.NumberOfMatches - originalNumberOfMatches}");

            return toReturn;
        }


        public static Dictionary<int, Row> EIDToRow(Row[] allData)
        {
            Dictionary<int, Row> eidToRow = new Dictionary<int, Row>();
            foreach (var row in allData)
                eidToRow[row.EnterpriseID] = row;

            return eidToRow;
        }

        List<Row[]> _phone2PairRows = new List<Row[]>();

        private void AddPhone2s(Row[] data)
        {
            List<Tuple<int, int>> toReturn = new List<Tuple<int, int>>();

            var phone2Pairs = File.ReadAllLines(@"phone1_phone2.txt").Concat(File.ReadAllLines(@"phone2.txt"));
            foreach (var pair in phone2Pairs)
            {
                var parts = pair.Split(',');
                int eid1 = int.Parse(parts[0]);
                int eid2 = int.Parse(parts[1]);



                Row row1 = _eidToRow[eid1];
                Row row2 = _eidToRow[eid2];

                _phone2PairRows.Add(new Row[] { row1, row2 });

            }
        }
        List<Row[]> _aliasPairs = new List<Row[]>();

        private void AddAliases(Row[] data)
        {
            List<Tuple<int, int>> toReturn = new List<Tuple<int, int>>();

            var phone2Pairs = File.ReadAllLines(@"alias.csv");
            foreach (var pair in phone2Pairs)
            {
                var parts = pair.Split(',');
                int eid1 = int.Parse(parts[0]);
                int eid2 = int.Parse(parts[1]);



                Row row1 = _eidToRow[eid1];
                Row row2 = _eidToRow[eid2];

                _aliasPairs.Add(new Row[] { row1, row2 });

            }
        }


        public List<int> AddFuzzyMatches(string file, Row[] data, Func<Row, string> groupingValue, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches, int originalNumberOfMatches)
        {
            _newMatchingRows.Add($"STARTING {file} MATCHES");

            var fuzzyMatches = File.ReadAllLines(@"FuzzyMatches/" + file);

            List<int> toReturn = new List<int>();

            Console.WriteLine();
            Console.WriteLine(file);

            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;
            foreach (var pair in fuzzyMatches)
            {
                var parts = pair.Split(',');
                int eid1 = int.Parse(parts[0]);
                int eid2 = int.Parse(parts[1]);

                Row row1 = _eidToRow[eid1];
                Row row2 = _eidToRow[eid2];

                if (groupingValue(row1) != "BADFORMAT")
                {
                    if (softEquals(row1, row2) >= softEqualsCount)
                    {
                        if (matches.AddMatch(row1, row2))
                        {
                            addedCounter++;

                            if (_printActuals)
                            {
                                //PrintingLibrary.PrintPair(row1, row2);
                                _newMatchingRows.Add(row1.ToString());
                                _newMatchingRows.Add(row2.ToString());
                                _newMatchingRows.Add("");
                            }
                        }

                    }
                    else
                    {
                        thrownOutCounter++;

                        if (_printErrors)
                        {
                            PrintingLibrary.PrintPair(row1, row2);
                        }
                    }
                }
            }

            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");
            Console.WriteLine($"Cumulative Matches Found: {matches.NumberOfMatches - originalNumberOfMatches}");

            PrintingLibrary.PrintRemainingRowCount(data, matches);

            return toReturn;
        }























        private static long[] BadPhones =
        {
            -1,
            0000000000,
            1111111111,
            2222222222,
            3333333333,
            4444444444,
            5555555555,
            6666666666,
            7777777777,
            8888888888,
            9999999999,
            1234567890,
        };

        static void Main2()
        {
            string[] allLines = File.ReadAllLines("c:/users/brush/desktop/FinalDataSet.csv").Skip(1).ToArray();

            string[] submission01 = File.ReadAllLines("c:/users/brush/desktop/submission01.csv");

            List<string> originalSubmissions = new List<string>();

            foreach (string line in submission01)
            {
                string[] parts = line.Split(',');

                int p1 = int.Parse(parts[0]);
                int p2 = int.Parse(parts[1]);

                if (p1 < p2)
                {
                    originalSubmissions.Add($"{p1},{p2}");
                }
                else
                {
                    originalSubmissions.Add($"{p2},{p1}");
                }
            }

            const int Phone1Field = 15;
            const int Phone2Field = 16;

            Dictionary<string, List<int>> phone1Key = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> phone2Key = new Dictionary<string, List<int>>();

            int count = 0;
            foreach (string line in allLines)
            {
                count++;

                if (count % 10000 == 0)
                {
                    Console.WriteLine($"{count}/{allLines.Length}");
                }

                string[] parts = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);

                if (parts[0] != "")
                {
                    int eid = int.Parse(parts[0]);

                    string phone1 = parts[Phone1Field].Replace("-", "");
                    string phone2 = parts[Phone2Field].Replace("-", "");
                    string phone3 = "";

                    if (phone2.Contains("^^"))
                    {
                        string[] partsToPhone2 = phone2.Split(new string[] { "^^" }, StringSplitOptions.None);

                        phone2 = partsToPhone2[0];
                        phone3 = partsToPhone2[1];
                    }

                    if (!string.IsNullOrEmpty(phone1))
                    {
                        if (!phone1Key.ContainsKey(phone1))
                        {
                            phone1Key.Add(phone1, new List<int>());
                        }

                        phone1Key[phone1].Add(eid);
                    }

                    if (!string.IsNullOrEmpty(phone2))
                    {
                        if (!phone2Key.ContainsKey(phone2))
                        {
                            phone2Key.Add(phone2, new List<int>());
                        }

                        phone2Key[phone2].Add(eid);
                    }

                    if (!string.IsNullOrEmpty(phone3))
                    {
                        if (!phone2Key.ContainsKey(phone3))
                        {
                            phone2Key.Add(phone3, new List<int>());
                        }

                        phone2Key[phone3].Add(eid);
                    }
                }
            }

            Console.Write("A");

            string[] key1 = phone1Key.Keys.ToArray();
            foreach (string key in key1)
            {
                phone1Key[key] = phone1Key[key].Distinct().ToList();
            }

            string[] key2 = phone2Key.Keys.ToArray();
            foreach (string key in key2)
            {
                phone2Key[key] = phone2Key[key].Distinct().ToList();
            }

            Console.Write("B");

            List<int> eidsWithPhone1AlsoInPhone2 = new List<int>();

            List<string> pairs = new List<string>();
            List<string> pairs2 = new List<string>();

            foreach (string key in phone1Key.Keys)
            {
                if (phone2Key.ContainsKey(key))
                {
                    int[] phone1Eids = phone1Key[key].ToArray();
                    int[] phone2Eids = phone2Key[key].ToArray();

                    foreach (int phone1Eid in phone1Eids)
                    {
                        foreach (int phone2Eid in phone2Eids)
                        {
                            if (phone1Eid != phone2Eid &&
                                !phone1Eids.Contains(phone2Eid))
                            {
                                string toWrite = null;

                                if (phone1Eid < phone2Eid)
                                {
                                    toWrite = $"{phone1Eid},{phone2Eid}";
                                }
                                else
                                {
                                    toWrite = $"{phone2Eid},{phone1Eid}";
                                }

                                if (!originalSubmissions.Contains(toWrite))
                                {
                                    pairs.Add(toWrite + ",1");
                                }
                            }
                        }
                    }
                }
            }

            Console.Write("C");

            foreach (string key in phone2Key.Keys)
            {
                List<int> eids = phone2Key[key];
                List<int> compareEids = new List<int>();

                if (phone1Key.ContainsKey(key))
                {
                    compareEids = phone1Key[key];
                }

                foreach (int eid1 in eids)
                {
                    foreach (int eid2 in eids)
                    {
                        if (eid1 != eid2 && !compareEids.Contains(eid1) &&
                            !compareEids.Contains(eid1))
                        {
                            string toWrite = null;

                            if (eid1 < eid2)
                            {
                                toWrite = $"{eid1},{eid2}";
                            }
                            else
                            {
                                toWrite = $"{eid2},{eid1}";
                            }

                            if (!originalSubmissions.Contains(toWrite))
                            {
                                pairs2.Add(toWrite + ",1");
                            }
                        }
                    }
                }
            }

            Console.Write("D");

            pairs = pairs.Distinct().ToList();
            pairs2 = pairs2.Distinct().ToList();

            File.WriteAllLines("C:/users/brush/desktop/phone1_phone2.txt",
                    pairs.ToArray());
            File.WriteAllLines("C:/users/brush/desktop/phone2.txt",
                pairs2.ToArray());

        }

        static void AliasMatches(Row[] data, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches, int originalNumberOfMatches)
        {
            Console.Write("ALIAS");

            Dictionary<string, List<int>> aliasEids = new Dictionary<string, List<int>>();
            Dictionary<string, List<Row>> names = new Dictionary<string, List<Row>>();

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i].FIRST != "" && data[i].LAST != "")
                {
                    if (!names.ContainsKey(data[i].FIRST + " " + data[i].LAST))
                    {
                        names.Add(data[i].FIRST + " " + data[i].LAST, new List<Row>());
                    }
                    names[data[i].FIRST + " " + data[i].LAST].Add(data[i]);
                    if (data[i].MIDDLE != "")
                    {
                        if (!names.ContainsKey(data[i].FIRST + " " + data[i].MIDDLE + " " + data[i].LAST))
                        {
                            names.Add(data[i].FIRST + " " + data[i].MIDDLE + " " + data[i].LAST, new List<Row>());
                        }
                        names[data[i].FIRST + " " + data[i].MIDDLE + " " + data[i].LAST].Add(data[i]);
                    }
                }
            }




            var aliasGroups = data.GroupBy(r => r.ALIAS);
            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;
            foreach (var aliasGroup in aliasGroups)
            {
                if (aliasGroup.Key.Contains(" ") && aliasGroup.Key != "JOHN DOE" && aliasGroup.Key != "JANE DOE")
                {
                    // matching aliases
                    foreach (Row row1 in aliasGroup)
                    {
                        foreach (Row row2 in aliasGroup)
                        {
                            if (row2 != row1)
                            {
                                if (softEquals(row1, row2) >= softEqualsCount)
                                {
                                    if (matches.AddMatch(row1, row2))
                                    {
                                        addedCounter++;

                                        if (_printActuals)
                                        {
                                            //PrintingLibrary.PrintPair(row1, row2);
                                            _newMatchingRows.Add(row1.ToString());
                                            _newMatchingRows.Add(row2.ToString());
                                            _newMatchingRows.Add("");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                thrownOutCounter++;

                                if (_printErrors)
                                {
                                    PrintingLibrary.PrintPair(row1, row2);
                                }
                            }

                        }
                    }


                    // matching names
                    if (names.ContainsKey(aliasGroup.Key))
                    //if (FuzzyStringMatch(aliasGroup.Key, nameGroup.Key))
                    {
                        foreach (Row row1 in aliasGroup)
                        {
                            foreach (Row row2 in names[aliasGroup.Key])
                            {
                                if (row2 != row1)
                                {
                                    if (softEquals(row1, row2) >= softEqualsCount)
                                    {
                                        if (matches.AddMatch(row1, row2))
                                        {
                                            addedCounter++;

                                            if (_printActuals)
                                            {
                                                //PrintingLibrary.PrintPair(row1, row2);
                                                _newMatchingRows.Add(row1.ToString());
                                                _newMatchingRows.Add(row2.ToString());
                                                _newMatchingRows.Add("");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    thrownOutCounter++;

                                    if (_printErrors)
                                    {
                                        PrintingLibrary.PrintPair(row1, row2);
                                    }
                                }

                            }
                        }
                    }
                }
            }



            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");
            Console.WriteLine($"Cumulative Matches Found: {matches.NumberOfMatches - originalNumberOfMatches}");

            PrintingLibrary.PrintRemainingRowCount(data, matches);
        }



        static void MaidenMatches(Row[] data, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches, int originalNumberOfMatches)
        {
            Console.Write("MAIDEN");

            Dictionary<string, List<int>> maidenEids = new Dictionary<string, List<int>>();
            Dictionary<string, List<Row>> names = new Dictionary<string, List<Row>>();
            Dictionary<string, List<Row>> middles = new Dictionary<string, List<Row>>();

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i].LAST != "")
                {
                    if (!names.ContainsKey(data[i].LAST))
                    {
                        names.Add(data[i].LAST, new List<Row>());
                    }
                    names[data[i].LAST].Add(data[i]);
                }
                if (data[i].MIDDLE != "")
                {
                    if (!middles.ContainsKey(data[i].MIDDLE))
                    {
                        middles.Add(data[i].MIDDLE, new List<Row>());
                    }
                    middles[data[i].MIDDLE].Add(data[i]);
                }
            }


            var maidenGroups = data.GroupBy(r => r.MOTHERS_MAIDEN_NAME);
            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;
            foreach (var maidenGroup in maidenGroups)
            {
                if (maidenGroup.Key != "")
                {
                    // matching maiden names
                    foreach (Row row1 in maidenGroup)
                    {
                        foreach (Row row2 in maidenGroup)
                        {
                            if (row2 != row1)
                            {
                                if (softEquals(row1, row2) >= softEqualsCount)
                                {
                                    if (matches.AddMatch(row1, row2))
                                    {
                                        addedCounter++;

                                        if (_printActuals)
                                        {
                                            //PrintingLibrary.PrintPair(row1, row2);
                                            _newMatchingRows.Add(row1.ToString());
                                            _newMatchingRows.Add(row2.ToString());
                                            _newMatchingRows.Add("");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                thrownOutCounter++;

                                if (_printErrors)
                                {
                                    PrintingLibrary.PrintPair(row1, row2);
                                }
                            }

                        }
                    }


                    // matching names
                    if (names.ContainsKey(maidenGroup.Key))
                    //if (FuzzyStringMatch(maidenGroup.Key, nameGroup.Key))
                    {
                        foreach (Row row1 in maidenGroup)
                        {
                            foreach (Row row2 in names[maidenGroup.Key])
                            {
                                if (row2 != row1)
                                {
                                    if (softEquals(row1, row2) >= softEqualsCount)
                                    {
                                        if (matches.AddMatch(row1, row2))
                                        {
                                            addedCounter++;

                                            if (_printActuals)
                                            {
                                                //PrintingLibrary.PrintPair(row1, row2);
                                                _newMatchingRows.Add(row1.ToString());
                                                _newMatchingRows.Add(row2.ToString());
                                                _newMatchingRows.Add("");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    thrownOutCounter++;

                                    if (_printErrors)
                                    {
                                        PrintingLibrary.PrintPair(row1, row2);
                                    }
                                }

                            }
                        }
                    }

                    // matching middles
                    if (middles.ContainsKey(maidenGroup.Key))
                    //if (FuzzyStringMatch(maidenGroup.Key, middles.Key))
                    {
                        foreach (Row row1 in maidenGroup)
                        {
                            foreach (Row row2 in middles[maidenGroup.Key])
                            {
                                if (row2 != row1)
                                {
                                    if (softEquals(row1, row2) >= softEqualsCount)
                                    {
                                        if (matches.AddMatch(row1, row2))
                                        {
                                            addedCounter++;

                                            if (_printActuals)
                                            {
                                                //PrintingLibrary.PrintPair(row1, row2);
                                                _newMatchingRows.Add(row1.ToString());
                                                _newMatchingRows.Add(row2.ToString());
                                                _newMatchingRows.Add("");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    thrownOutCounter++;

                                    if (_printErrors)
                                    {
                                        PrintingLibrary.PrintPair(row1, row2);
                                    }
                                }

                            }
                        }
                    }
                }

            }



            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");
            Console.WriteLine($"Cumulative Matches Found: {matches.NumberOfMatches - originalNumberOfMatches}");

            PrintingLibrary.PrintRemainingRowCount(data, matches);
        }









































        Dictionary<int, Row> _eidToRow;


        public void FindAllMatches(Row[] allData, ref ClosedSets newMatches)
        {
            _eidToRow = EIDToRow(allData);
            //AddPhone2s(allData);
            //AddAliases(allData);

            int originalNumberOfMatches = newMatches.NumberOfMatches;


            MaidenMatches(allData, 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                First = true,
                SSN = true,
                DOB = true,
                Phone = true,
                Address = true,
                Email = true,
            }), ref newMatches, originalNumberOfMatches);






            Console.WriteLine("Done!");
            if (_printActuals)
            {
                File.WriteAllLines("newMatches.csv", _newMatchingRows);
            }

            return;






            //******************  SOLID MATCHES   ******************//
            //OOO 76/76
            AddMatches("01. SSN + LAST", allData, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                Last = true,
            }), 0, (r1, r2) =>
                1, ref newMatches, originalNumberOfMatches);
            //OOO 13 / 13
            AddMatches("02. SSN + DOB", allData, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                DOB = true,
            }), 0, (r1, r2) =>
                1, ref newMatches, originalNumberOfMatches);
            //AddMatches("03. SSN + PHONE", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //    Phone = true,
            //}), 0, (r1, r2) =>
            //        1, ref newMatches, originalNumberOfMatches);
            //AddMatches("04. SSN + ADDRESS", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //    Address = true,
            //}), 0, (r1, r2) =>
            //    1, ref newMatches, originalNumberOfMatches);

            //AddMatches("05. NAME + DOB strong", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("06. NAME + PHONE (no sr/jr)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Phone = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddMatches("07. NAME + ADDRESS (no sr/jr)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddMatches("08. DOB + PHONE (no twin)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    Last = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddMatches("09. DOB + ADDRESS (no twin)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //    Address = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddMatches("10. PHONE + ADDRESS (no twin)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //    Address = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //}), ref newMatches, originalNumberOfMatches);

            ////XXXXXX AT MOST 29/319
            ////AddMatches("11. SSN + soft match", allData, r => HardSelector(r, new FieldInclusions
            ////{
            ////    SSN = true
            ////}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            ////{
            ////    Name = true,
            ////    Phone = true,
            ////    DOB = true,
            ////    Address = true,
            ////}), ref newMatches, originalNumberOfMatches); // Josh Code Review : Makes many of the SSN matches above redundant.

            //////******************  PROBABLY SOLID MATCHES   ******************//

            //AddMatches("12. NAME + PHONE (sr/jr)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("13. NAME + ADDRESS (sr/jr)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("14. DOB + PHONE (twin)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Last = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("15. DOB + ADDRESS (twin)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Address = true,
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Last = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("16. PHONE + ADDRESS (twin)", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Last = true,
            //}), ref newMatches, originalNumberOfMatches);


            //AddMatches("17. Name + 2 soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("18. DOB + 2 soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatches("19. Phone + 2 soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    DOB = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);
            AddMatches("20. Address + 2 soft", allData, r => HardSelector(r, new FieldInclusions
            {
                Address = true,
            }), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                DOB = true,
                Phone = true,
                Alias = true,
            }), ref newMatches, originalNumberOfMatches);  //Josh code review : This could match on address, first name, and DOB.  Maybe it should go in the weaker matches category?

            //////////******************  WEAKER MATCHES   ******************//

            //List<int> weakerMatchedIDs = new List<int>();
            //var weak =
            //AddMatches("21. PHONE + soft SSN", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //}), ref newMatches, originalNumberOfMatches);
            ////weakerMatchedIDs.AddRange(weak);

            ////weak =
            //AddMatches("22. NAME + DOB weaker", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSNSoft = true,
            //}), ref newMatches, originalNumberOfMatches);
            ////weakerMatchedIDs.AddRange(weak);

            ////weak =
            //AddMatches("23. NAME + PHONE weaker", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSNSoft = true,
            //}), ref newMatches, originalNumberOfMatches);
            ////weakerMatchedIDs.AddRange(weak);

            //weak =
            AddMatches("24. NAME + ADDRESS weaker", allData, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Address = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSNSoft = true,
            }), ref newMatches, originalNumberOfMatches);
            //weakerMatchedIDs.AddRange(weak);

            ////weak =
            //AddMatches("25. PHONE + soft NAME", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Name = true,
            //}), ref newMatches, originalNumberOfMatches);
            //weakerMatchedIDs.AddRange(weak);

            //weak =
            //AddMatches("26. PHONE + soft FIRST/DOB", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //}), ref newMatches, originalNumberOfMatches);
            //weakerMatchedIDs.AddRange(weak);

            ////******************  NEW SUPER-WEAK MATCHES   ******************//

            //AddMatches("27. FIRST + EMAIL + soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Email = true,
            //    First = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddMatches("28. EMAIL + soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Email = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);

            ////XXXXXX AT MOST 29/200ish
            //AddMatches("29. SSN + soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);

            AddMatches("30. NAME + soft", allData, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                DOB = true,
                Phone = true,
                Address = true,
            }), ref newMatches, originalNumberOfMatches);

            //AddMatches("31. Phone + soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    Name = true,
            //    DOB = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);

            // XXX AT MOST 49/10000ish
            //AddMatches("32. DOB + soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    Name = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);

            // XXX AT MOST 49/10000ish
            //AddMatches("33. Address + soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    Name = true,
            //    DOB = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);




            AddMatches("34. SSN + FIRST", allData, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                First = true,
            }), 0, (r1, r2) =>
                1, ref newMatches, originalNumberOfMatches);



            //AddMatchesPhone2("35. PHONE2 + SSN + soft", r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //}), 1, (r1, r2) =>
            //    SoftMatchCount(r1, r2, new FieldInclusions
            //    {
            //        First = true,
            //        Last = true,
            //        DOB = true,
            //        Address = true,
            //    }), ref newMatches, originalNumberOfMatches);

            //AddMatchesPhone2("36. PHONE2 + LAST", r => HardSelector(r, new FieldInclusions
            //{
            //    Last = true,
            //}), 0, (r1, r2) =>
            //    1, ref newMatches, originalNumberOfMatches);
            //AddMatchesPhone2("37. PHONE2 + FIRST", r => HardSelector(r, new FieldInclusions
            //{
            //    First = true,
            //}), 0, (r1, r2) =>
            //    1, ref newMatches, originalNumberOfMatches);
            //AddMatchesPhone2("38. PHONE2 + DOB", r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //}), 0, (r1, r2) =>
            //    1, ref newMatches, originalNumberOfMatches);
            //AddMatchesPhone2("39. PHONE2 + ADDRESS", r => HardSelector(r, new FieldInclusions
            //{
            //    Address = true,
            //}), 0, (r1, r2) =>
            //    1, ref newMatches, originalNumberOfMatches);
            AddMatchesPhone2("40. PHONE2 + 1 SOFT", r => HardSelector(r, new FieldInclusions
            { }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                First = true,
                Last = true,
                DOB = true,
                Address = true,
                Phone = true,
                Alias = true,
            }), ref newMatches, originalNumberOfMatches);

            //AddMatches("41. SSN + 2 soft", allData, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    First = true,
            //    Last = true,
            //    DOB = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);


            //AddMatchesPhone2("42. PHONE2 + NAME+ soft", r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //}), 0, (r1, r2) =>
            //    1, ref newMatches, originalNumberOfMatches);

            //AddMatchesPhone2("44. PHONE2 + DOB+ soft", r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    Last = true,
            //    Address = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);
            //AddMatchesPhone2("45. PHONE2 + ADDRESS + soft", r => HardSelector(r, new FieldInclusions
            //{
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    Last = true,
            //    DOB = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddFuzzyMatches("SSN-LASTand0of.csv", allData, r => HardSelector(r, new FieldInclusions
            //{
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddFuzzyMatches("SSN-DOBand0of.csv", allData, r => HardSelector(r, new FieldInclusions
            //{
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    First = true,
            //    Last = true,
            //    Phone = true,
            //    Address = true,
            //}), ref newMatches, originalNumberOfMatches);

            //AddFuzzyMatches("ADDRESS1and2ofSSN-FIRST-DOB-PHONE.csv", allData, r => HardSelector(r, new FieldInclusions
            //{
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    Last = true,
            //    DOB = true,
            //    Phone = true,
            //}), ref newMatches, originalNumberOfMatches);


            //AddMatchesAlias("46. Alias + soft", r => HardSelector(r, new FieldInclusions
            //{
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    Last = true,
            //    DOB = true,
            //    Phone = true,
            //    Address = true,
            //    Email = true,
            //}), ref newMatches, originalNumberOfMatches);


            AliasMatches(allData, 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                DOB = true,
                Phone = true,
                Address = true,
                Email = true,
            }), ref newMatches, originalNumberOfMatches);


            newMatches.AddMatch(_eidToRow[14491313], _eidToRow[14491309]);
            newMatches.AddMatch(_eidToRow[14155709], _eidToRow[14155711]);
            newMatches.AddMatch(_eidToRow[14363987], _eidToRow[14363990]);
            newMatches.AddMatch(_eidToRow[14756101], _eidToRow[14756103]);
            newMatches.AddMatch(_eidToRow[14310133], _eidToRow[14310135]);
            newMatches.AddMatch(_eidToRow[13121604], _eidToRow[13121610]);
            newMatches.AddMatch(_eidToRow[14414547], _eidToRow[14414548]);
            newMatches.AddMatch(_eidToRow[14205227], _eidToRow[14205222]);

            Console.WriteLine("Done!");
            if (_printActuals)
            {
                File.WriteAllLines("newMatches.csv", _newMatchingRows);
            }
        }


    }
}

