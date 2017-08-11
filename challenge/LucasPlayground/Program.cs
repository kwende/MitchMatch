using challenge;
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

        static void Main(string[] args)
        {
            // Load Data
            var lines = GetLines();
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var data = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();

            // Clean Data
            _badSSNs = data.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            _badDOBs = new DateTime[] { new DateTime(1900, 1, 1) };
            _badPhones = data.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();
            _badAddresses = data.GroupBy(r => r.ADDRESS1).Where(g => !g.Key.Contains(' ') && g.Count() > 2).Select(g => g.Key).ToArray();

            CleanData(ref data);

            DisplayPossibleMatches(data);


            // Process Data
            Console.WriteLine(lines.Count() + " total rows"); // >= 15374761
            row[] remainingRows = data;
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();


            //******************  SOLID MATCHES   ******************//
            AddMRNMatches(data, ref matches);

            AddMatches("SSN + LAST", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                Last = true,
            }), 0, (r1, r2) =>
                1);
            AddMatches("SSN + DOB", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                DOB = true,
            }), 0, (r1, r2) =>
                1);
            AddMatches("SSN + PHONE", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                Phone = true,
            }), 0, (r1, r2) =>
                    1);
            AddMatches("SSN + ADDRESS", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                SSN = true,
                Address = true,
            }), 0, (r1, r2) =>
                1);

            AddMatches("NAME + DOB strong", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                DOB = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                Phone = true,
                Address = true,
            }));
            AddMatches("NAME + PHONE (no sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Phone = true
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                DOB = true,
            }));

            AddMatches("NAME + ADDRESS (no sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Address = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                DOB = true,
            }));

            AddMatches("DOB + PHONE (no twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                First = true,
                DOB = true,
                Phone = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
            }));
            AddMatches("DOB + ADDRESS (no twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                DOB = true,
                Address = true
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
            }));

            AddMatches("PHONE + ADDRESS (no twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Phone = true,
                Address = true
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
            }));


            AddMatches("SSN + soft match", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                SSN = true
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Name = true,
                Phone = true,
                DOB = true,
                Address = true,
            }));

            //******************  PROBABLY SOLID MATCHES   ******************//

            AddMatches("NAME + PHONE (sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Phone = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Address = true,
            }));
            AddMatches("NAME + ADDRESS (sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Address = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Phone = true,
            }));
            AddMatches("DOB + PHONE (twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                First = true,
                DOB = true,
                Phone = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Last = true,
                Address = true,
            }));
            AddMatches("DOB + ADDRESS (twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Address = true,
                DOB = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Last = true,
                Phone = true,
            }));
            AddMatches("PHONE + ADDRESS (twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Phone = true,
                Address = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Last = true,
            }));


            AddMatches("Name + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
            }), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                DOB = true,
                Phone = true,
                Address = true,
            }));
            AddMatches("DOB + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                DOB = true,
            }), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                Phone = true,
                Address = true,
            }));
            AddMatches("Phone + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Phone = true,
            }), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                DOB = true,
                Address = true,
            }));
            AddMatches("Address + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Address = true,
            }), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                DOB = true,
                Phone = true,
            }));


            //******************  WEAKER MATCHES   ******************//

            List<int> weakerMatchedIDs = new List<int>();
            var weak =
            AddMatches("PHONE + soft SSN", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Phone = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
            }));
            weakerMatchedIDs.AddRange(weak);

            weak =
            AddMatches("NAME + DOB weaker", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                DOB = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSNSoft = true,
            }));
            weakerMatchedIDs.AddRange(weak);

            weak =
            AddMatches("NAME + PHONE weaker", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Phone = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSNSoft = true,
            }));
            weakerMatchedIDs.AddRange(weak);

            weak =
            AddMatches("NAME + ADDRESS weaker", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Name = true,
                Address = true,
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSNSoft = true,
            }));
            weakerMatchedIDs.AddRange(weak);

            weak =
            AddMatches("PHONE + soft NAME", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Phone = true
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                Name = true,
            }));
            weakerMatchedIDs.AddRange(weak);

            weak =
            AddMatches("PHONE + soft FIRST/DOB", data, ref matches, r => HardSelector(r, new FieldInclusions
            {
                Phone = true
            }), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            {
                First = true,
                DOB = true,
            }));
            weakerMatchedIDs.AddRange(weak);

            //******************  FUZZY MATCHES   ******************//
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            var addedHarderSoftMatches1 = AddSoftMatches("HARDER SOFT MATCH1", remainingRows, remainingRows, ref matches, 3, (ri, rj) => HarderAgreementCount(ri, rj));
            PrintRemainingRowCount(data, matches);

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            var addedHarderSoftMatches2 = AddSoftMatches("HARDER SOFT MATCH2", remainingRows, remainingRows, ref matches, 2, (ri, rj) => HarderAgreementCount(ri, rj));
            PrintRemainingRowCount(data, matches);

            var removedHandMatched = RemoveHandErrors(data, ref matches);

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            var addedEasierSoftMatches1 = AddSoftMatches("EASIER SOFT MATCH1", remainingRows, remainingRows, ref matches, 2, (ri, rj) => EasierAgreementCount(ri, rj));
            PrintRemainingRowCount(data, matches);

            var addedHandMatched = AddHandMatches(data, ref matches);

            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            var addedEasierSoftMatches2 = AddSoftMatches("EASIER SOFT MATCH2", remainingRows, data, ref matches, 2, (ri, rj) => EasierAgreementCount(ri, rj));
            PrintRemainingRowCount(data, matches);


            weakerMatchedIDs.AddRange(addedHarderSoftMatches1.SelectMany(group => group.Select(row => row.EnterpriseID)));
            weakerMatchedIDs.AddRange(addedHarderSoftMatches2.SelectMany(group => group.Select(row => row.EnterpriseID)));
            weakerMatchedIDs.AddRange(addedEasierSoftMatches1.SelectMany(group => group.Select(row => row.EnterpriseID)));
            weakerMatchedIDs.AddRange(addedEasierSoftMatches2.SelectMany(group => group.Select(row => row.EnterpriseID)));

            weakerMatchedIDs = weakerMatchedIDs.Distinct().ToList();

            //TransitiveClosure tc = TransitiveClosure.Compute(matches, data);
            //bool[] alreadyTakenCareOf = new bool[weakerMatchedIDs.Count];
            //List<List<int>> possibleBadSets = new List<List<int>>();
            //for (int i = 0; i < weakerMatchedIDs.Count; i++)
            //{
            //    if (!alreadyTakenCareOf[i])
            //    {
            //        row row = data.Where(r => r.EnterpriseID == weakerMatchedIDs[i]).First();
            //        row[] group = tc.FindClosedSetForRow(row);
            //        List<int> toAdd = new List<int>();
            //        foreach (row r in group)
            //        {
            //            toAdd.Add(r.EnterpriseID);
            //            if (weakerMatchedIDs.IndexOf(r.EnterpriseID) >= 0)
            //            {
            //                alreadyTakenCareOf[weakerMatchedIDs.IndexOf(r.EnterpriseID)] = true;
            //            }
            //        }
            //        possibleBadSets.Add(toAdd);
            //    }
            //}
            //using (StreamWriter sw = File.CreateText("C:/users/lsabalka/desktop/closedsets.txt"))
            //{
            //    foreach (List<int> closedSet in possibleBadSets)
            //    {
            //        sw.WriteLine(string.Join(",", closedSet));
            //    }
            //}


            PrintAnalysis(matches, data);

            //WeakMatches(matches, data);


            //CompareToJosh(data, matches);



            //TransitiveClosure tc = TransitiveClosure.Compute(matches, data);

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/closedsets.txt"))
            //{
            //    foreach (List<int> closedSet in tc.ClosedRowSets)
            //    {
            //        sw.WriteLine(string.Join(",", closedSet)); 
            //    }
            //}

            //SaveResults(matches, data);

            Console.ReadLine();
        }

        static void DisplayPossibleMatches(IEnumerable<row> data)
        {
            while(true)
            {
                string line = Console.ReadLine();
                int eid;
                if (int.TryParse(line, out eid))
                {
                    var row = data.Where(r => r.EnterpriseID == eid).FirstOrDefault();
                    if (row == null)
                    {
                        Console.WriteLine("Could not find that enterprise ID");
                        continue;
                    }
                    RowLibrary.Print(row);
                    var softmatches = data.Where(d => EasiestAgreementCount(d, row) >= 2);
                    foreach(var match in softmatches)
                    {
                        if (row == match)
                            continue;
                        RowLibrary.Print(match);
                    }
                }
                else
                {
                    Console.WriteLine("Please enter an integer");
                    continue;
                }
            }
        }

        //private static void WeakMatches(row[] data, Dictionary<int, List<int>> matches)
        //{
        //    Dictionary<int, List<row>> rowDictionary = MatchesToRowDictionary(data, matches);

        //    List<List<row>> possibleErrors = new List<List<row>>();
        //    foreach(var set in rowDictionary)
        //    {
        //        List<row> rows = set.Value;
        //        bool strongMatch = false;
        //        int fieldAgreement = 0;
        //        for (int i = 0; i < rows.Count - 1; i++)
        //        {
        //            for (int j = i + 1; j < rows.Count; j++)
        //            {
        //                if()

        //                if (FuzzyStringMatch(rows[i].LAST, rows[j].LAST))
        //                {
        //                    fieldAgreement++;

        //                    if (FuzzySSNMatch(rows[i].SSN, rows[j].SSN))
        //                        fieldAgreement++;

        //                    if (challenge.Program.FuzzyAddressMatch(rows[i], rows[j]))
        //                        fieldAgreement++;

        //                    if (FuzzyDateEquals(rows[i].DOB, rows[j].DOB))
        //                        fieldAgreement++;
        //                }
        //            }
        //        }
        //    }


        //}


        //private static Dictionary<int, List<row>> MatchesToRowDictionary(row[] data, Dictionary<int, List<int>> matches)
        //{
        //    Dictionary<int, List<row>> rowDictionary = new Dictionary<int, List<row>>();

        //    TransitiveClosure tc = TransitiveClosure.Compute(matches, data);

        //    foreach (List<int> set in tc.ClosedRowSets)
        //    {
        //        List<row> rows = new List<row>();
        //        foreach (int id in set)
        //        {
        //            rows.Add(data.First(row => row.EnterpriseID == id));
        //        }
        //        foreach (int id in set)
        //        {
        //            rowDictionary.Add(id, rows);
        //        }
        //    }
        //    return rowDictionary;
        //}

        //private static void CompareToJosh(row[] data, Dictionary<int, List<int>> matches)
        //{
        //    Dictionary<int, List<int>> joshMatches = challenge.Program.Doit(null);

        //    Dictionary<int, List<row>> rowDictionary = MatchesToRowDictionary(data, matches);
        //    Dictionary<int, List<row>> joshRowDictionary = MatchesToRowDictionary(data, joshMatches);

        //    int discrepancyCount = 0;
        //    foreach (var set in rowDictionary)
        //    {
        //        bool print = false;
        //        List<row> printJosh = new List<row>();
        //        if (joshRowDictionary.ContainsKey(set.Key))
        //        {
        //            List<row> joshSet = joshRowDictionary[set.Key];
        //            foreach (row row in set.Value)
        //            {
        //                if (!joshSet.Contains(row))
        //                {
        //                    printJosh = joshSet;
        //                    print = true;
        //                    break;
        //                }
        //            }
        //            foreach (row row in joshSet)
        //            {
        //                if (!set.Value.Contains(row))
        //                {
        //                    printJosh = joshSet;
        //                    print = true;
        //                    break;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            print = true;
        //        }

        //        if (print)
        //        {
        //            discrepancyCount++;
        //            Console.WriteLine("Lucas/Ben:");
        //            PrintRows(set.Value);
        //            Console.WriteLine("Josh:");
        //            PrintRows(printJosh);
        //            Console.WriteLine();
        //        }
        //    }
        //    Console.WriteLine($"Discrepancy count: {discrepancyCount}");

        //}


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
                lines = File.ReadLines(@"C:/users/brush/desktop/FInalDataset.csv");
            }
            else if (Environment.UserName.ToLower().Contains("jbrownkramer") ||
                Environment.UserName.ToLower().Contains("josh"))
            {
                lines = File.ReadLines(@"C:\Users\jbrownkramer\Desktop\Data\data.csv");
            }


            return lines;
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
            Dictionary<int, row> rowByEnterpriseId = new Dictionary<int, row>();
            foreach (var r in allTruePositiveData)
            {
                rowByEnterpriseId[r.EnterpriseID] = r;
            }
            challenge.Program.MRNFalsePositiveAnalysis(matches, rowByEnterpriseId);

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

            Console.WriteLine("Possible false negatives, likely matches triple:");
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


        #region Printing
        private static bool _printLargeGroupValues = false;
        private static bool _printErrors = false;
        private static bool _printActuals = false;
        private static bool _printModifieds = false;
        private static int _printCount = 0;
        private static void PrintCheckCount()
        {
            if ((++_printCount + 25) % 50 == 0)
            {
                Console.WriteLine("continue printing? ");
                string answer = Console.ReadLine();
                if (answer != "Y" && answer != "y")
                {
                    _printErrors = false;
                    _printActuals = false;
                    _printModifieds = false;
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

        public static void PrintRemainingRowCount(IEnumerable<row> data, Dictionary<int, List<int>> matches)
        {
            int remaining = 0;
            foreach (var row in data)
            {
                if (!matches.ContainsKey(row.EnterpriseID))
                {
                    remaining++;
                }
            }
            Console.WriteLine($"Remaining: {remaining}");
        }
        #endregion

        #region Hard Matching
        public class FieldInclusions
        {
            public bool First;
            public bool Last;
            public bool SSN;
            public bool DOB;
            public bool Phone;
            public bool Address;

            public bool Name;
            public bool SSNSoft;
            public bool AddressSoft;
        }

        public static string HardSelector(row row, FieldInclusions toInclude)
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
            return toReturn;
        }
        #endregion

        #region Soft Matching

        public static int SoftMatchCount(row a, row b, FieldInclusions toInclude)
        {
            int toReturn = 0;
            if (toInclude.SSN && FuzzySSNMatch(a.SSN, b.SSN))
                toReturn++;
            if ((toInclude.Name || toInclude.First) && FuzzyStringMatch(a.FIRST, b.FIRST))
                toReturn++;
            if ((toInclude.Name || toInclude.Last) && FuzzyStringMatch(a.LAST, b.LAST))
                toReturn++;
            if (toInclude.DOB && FuzzyDateEquals(a.DOB, b.DOB))
                toReturn++;
            if (toInclude.Phone && FuzzyPhoneMatch(a.PHONE, b.PHONE))
                toReturn++;
            if (toInclude.Address && FuzzyAddressMatchEditDistance(a, b))
                toReturn++;
            if (toInclude.SSNSoft && FuzzySSNNoConflict(a.SSN, b.SSN))
                toReturn++;
            if (toInclude.AddressSoft && challenge.Program.FuzzyAddressMatch(a, b))
                toReturn++;
            return toReturn;
        }
        public static bool IsSSNValid(int a)
        {
            return a > 0 && !_badSSNs.Contains(a);
        }

        public static bool FuzzySSNNoConflict(int a, int b)
        {
            return !IsSSNValid(a) ||
                    !IsSSNValid(b) ||
                    FuzzySSNMatch(a, b);

        }

        public static bool FuzzySSNMatch(int a, int b)
        {
            return IsSSNValid(a) && IsSSNValid(b) && challenge.Program.OneDifference(a.ToString(), b.ToString());
        }

        public static bool FuzzyPhoneMatch(int a, int b)
        {
            return IsPhoneValid(a) && IsPhoneValid(b) && challenge.Program.KDifferences(a.ToString(), b.ToString(), 1);
        }

        public static bool IsPhoneValid(int a)
        {
            return a > 0;
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
            return challenge.Program.OneDifference(a.ToString(), b.ToString());
        }

        public static bool FuzzyAddressMatchEditDistance(row a, row b)
        {
            if (a.ADDRESS1 == "" || b.ADDRESS1 == "")
                return false;
            if (a.ADDRESS1 == b.ADDRESS1)
                return true;

            int editDistance = challenge.Ben.EditDistance.Compute(a.ADDRESS1, b.ADDRESS1);

            return editDistance <= 1;
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
            return challenge.Program.OneDifference(a, b);
        }

        public static int HarderAgreementCount(row ri, row rj)
        {
            int fieldAgreement = 0;

            if (FuzzyStringMatch(ri.LAST, rj.LAST))
            {
                fieldAgreement++;

                if (FuzzySSNMatch(ri.SSN, rj.SSN))
                    fieldAgreement++;

                if (FuzzyAddressMatchEditDistance(ri, rj))
                    fieldAgreement++;

                if (FuzzyDateEquals(ri.DOB, rj.DOB))
                    fieldAgreement++;
            }
            return fieldAgreement;
        }

        public static int EasierAgreementCount(row ri, row rj)
        {
            int fieldAgreement = 0;

            if (challenge.Program.KDifferences(ri.LAST, rj.LAST, 2))
                fieldAgreement++;

            if (NonemptyEquality(ri.FIRST, rj.FIRST))
                fieldAgreement++;

            if (FuzzySSNMatch(ri.SSN, rj.SSN))
                fieldAgreement++;

            if (challenge.Program.FuzzyAddressMatch(ri, rj))
                fieldAgreement++;

            if (FuzzyDateEquals(ri.DOB, rj.DOB))
                fieldAgreement++;

            return fieldAgreement;
        }

        public static int EasiestAgreementCount(row ri, row rj)
        {
            int fieldAgreement = 0;

            if (challenge.Program.KDifferences(ri.LAST, rj.LAST, 2))
                fieldAgreement++;

            if (challenge.Program.KDifferences(ri.FIRST, rj.FIRST, 2))
                fieldAgreement++;

            if (FuzzySSNMatch(ri.SSN, rj.SSN))
                fieldAgreement++;

            if (challenge.Program.FuzzyAddressMatch(ri, rj))
                fieldAgreement++;

            if (FuzzyDateEquals(ri.DOB, rj.DOB))
                fieldAgreement++;

            if (FuzzyPhoneMatch(ri.PHONE, rj.PHONE))
                fieldAgreement++;

            return fieldAgreement;
        }
        #endregion

        #region Matching
        public static void AddMRNMatches(IEnumerable<row> data, ref Dictionary<int, List<int>> matches)
        {
            Console.WriteLine();
            Console.WriteLine("MRN");

            List<List<row>> groupsAdded = new List<List<row>>();
            var fourMillion = data.Where(r => r.MRN >= 4000000).ToArray();
            //Pair off and make a soft check on field to verify sameness
            for (int i = 0; i < fourMillion.Count(); i += 2)
            {
                var r = fourMillion[i];
                var s = fourMillion[i + 1];
                challenge.Program.Add(r, s, ref matches);
                groupsAdded.Add(new List<row>(new row[] { r, s }));
            }

            PrintRemainingRowCount(data, matches);
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

        static List<int> AddMatches<T>(string label, IEnumerable<row> data, ref Dictionary<int, List<int>> matches, Func<row, T> groupingValue, int softEqualsCount, Func<row, row, int> softEquals)
        {
            List<int> toReturn = new List<int>();

            Console.WriteLine();
            Console.WriteLine(label);


            var grouped = data.GroupBy(groupingValue);

            int thrownOutCounter = 0;
            int addedCounter = 0;
            int modifiedCounter = 0;
            foreach (var group in grouped)
            {
                if (group.Count() >= 5) //These are all garbage
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


                if (group.Any(r => softEquals(r, group.First()) < softEqualsCount))
                {
                    if (_printErrors)
                    {
                        PrintRows(group);
                    }
                    thrownOutCounter++;
                }
                else
                {
                    var matchesCopy = matches;
                    bool addedGroup = group.Any(row => !matchesCopy.ContainsKey(row.EnterpriseID));
                    bool isGroupModified = false;

                    int beforeCount = 0;
                    foreach (var r in group)
                    {
                        if (matches.ContainsKey(r.EnterpriseID) && (beforeCount == 0 || beforeCount > matches[r.EnterpriseID].Count))
                        {
                            beforeCount = matches[r.EnterpriseID].Count;
                        }
                    }

                    foreach (var r in group)
                    {
                        foreach (var s in group)
                        {
                            if (r != s)
                                challenge.Program.Add(r, s, ref matches);
                        }
                    }
                    if (beforeCount != 0 && beforeCount != matches[group.First().EnterpriseID].Count)
                    {
                        isGroupModified = true;
                    }



                    if (addedGroup)
                    {
                        toReturn.AddRange(group.Select(row => row.EnterpriseID));
                        addedCounter++;
                        if (_printActuals)
                        {
                            PrintRows(group);
                        }
                    }
                    if (isGroupModified)
                    {
                        toReturn.AddRange(group.Select(row => row.EnterpriseID));
                        modifiedCounter++;
                        if (_printModifieds)
                        {
                            var printme = matches[group.First().EnterpriseID].Select(id => data.First(row => row.EnterpriseID == id)).ToList();
                            printme.Add(group.First());
                            PrintRows(printme);
                        }
                    }
                }

            }
            Console.WriteLine($"Group thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");

            PrintRemainingRowCount(data, matches);

            return toReturn;
        }

        public static List<List<row>> AddSoftMatches(string label, row[] compareFrom, row[] compareTo, ref Dictionary<int, List<int>> matches, int fieldsRequired, Func<row, row, int> agreementCount)
        {

            Console.WriteLine();
            Console.WriteLine(label);

            List<List<row>> addedThisTime = new List<List<row>>();

            for (int i = 0; i < compareFrom.Count(); i++)
            {
                row ri = compareFrom[i];
                for (int j = 0; j < compareTo.Count(); j++)
                {
                    row rj = compareTo[j];

                    if (ri.EnterpriseID == rj.EnterpriseID)
                    {
                        continue;
                    }

                    int fieldAgreement = agreementCount(ri, rj);

                    if (fieldAgreement >= fieldsRequired)
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

        private static List<List<row>> AddHandMatches(row[] data, ref Dictionary<int, List<int>> matches)
        {
            Console.WriteLine();
            Console.WriteLine("HAND MATCHED");

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
                new int[] {15713945, 15974259},
            };

            List<List<row>> addedThisTime = new List<List<row>>();

            foreach (int[] pair in pairs)
            {
                row a = data.Where(row => row.EnterpriseID == pair[0]).FirstOrDefault();
                row b = data.Where(row => row.EnterpriseID == pair[1]).FirstOrDefault();

                challenge.Program.Add(a, b, ref matches);
                addedThisTime.Add(new List<row> { a, b });
                if (_printActuals)
                {
                    PrintPair(a, b);
                }
            }

            PrintRemainingRowCount(data, matches);

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
            Console.WriteLine();
            Console.WriteLine("HAND REMOVED");

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

            PrintRemainingRowCount(data, matches);

            return removedThisTime;
        }
        #endregion
    }
}
