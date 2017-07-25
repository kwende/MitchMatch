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

            var lines = File.ReadLines(@"C:/users/ben/desktop/FInalDataset.csv");
            var allData = lines.Skip(1).Select(l => RowLibrary.ParseRow(l)).ToArray();
            var data = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();
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
            var addedSSN = AddMatches(data, r => r.SSN, 4, (r1, r2) => true, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("PHONE");
            var addedPhone = AddMatches(data, r => r.PHONE, 5, (r1, r2) => (r1.FIRST != "" && r1.FIRST == r2.FIRST) || (r1.LAST != "" && r1.LAST == r2.LAST) || challenge.Program.DateSoftMatch(r1.DOB, r2.DOB), ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + DOB");
            var addedNameDOB = AddMatches(data, r =>
            {
                return (r.LAST != "" ? (r.DOB != default(DateTime) ? r.LAST + r.FIRST + r.DOB.ToString("d") : "NODOB") : "NONAME");
            }, 4, (r1, r2) =>
                challenge.Program.OneDifference(r1.PHONE.ToString(), r2.PHONE.ToString()) || challenge.Program.OneDifference(r1.SSN.ToString(), r2.SSN.ToString()) || challenge.Program.FuzzyAddressMatch(r1, r2) || (r1.SSN <= 0 || r2.SSN <= 0),
            ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("ADDRESS + DOB");
            var addedAddressDOB = AddMatches(data, r =>
            {
                return (r.ADDRESS1 != "" ? (r.DOB != default(DateTime) ? r.DOB.ToString("d") + r.ADDRESS1 : "NODOB") : "NOADDRESS");
            }, 4, (r1, r2) =>
                challenge.Program.OneDifference(r1.PHONE.ToString(), r2.PHONE.ToString()) || challenge.Program.OneDifference(r1.SSN.ToString(), r2.SSN.ToString()) || (r1.FIRST != "" && r1.FIRST == r2.FIRST) || (r1.LAST != "" && r1.LAST == r2.LAST) || (r1.SSN <= 0 || r2.SSN <= 0),
            ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + ADDRESS");
            var addedNameAddress = AddMatches(data, r =>
            {
                return (r.LAST != "" ? (r.ADDRESS1 != "" ? r.LAST + r.FIRST + r.ADDRESS1 : "NOADDRESS") : "NONAME");
            }, 4, (r1, r2) =>
                challenge.Program.OneDifference(r1.PHONE.ToString(), r2.PHONE.ToString()) || challenge.Program.OneDifference(r1.SSN.ToString(), r2.SSN.ToString()) || challenge.Program.FuzzyDateEquals(r1.DOB, r2.DOB) || (r1.SSN <= 0 || r2.SSN <= 0),
            ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("NAME + PHONE");
            var addedNamePhone = AddMatches(data, r =>
            {
                return (r.LAST != "" ? (r.PHONE > 0 ? r.LAST + r.FIRST + r.PHONE : "NOPHONE") : "NONAME");
            }, 4, (r1, r2) => true, ref matches);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);

            Console.WriteLine();
            Console.WriteLine("SOFT MATCH");
            int[] badSSNs = data.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            var addedSoftMatches = AddSoftMatches(remainingRows, ref matches, badSSNs);
            remainingRows = data.Where(r => !matches.ContainsKey(r.EnterpriseID)).ToArray();
            Console.WriteLine("Remaining: " + remainingRows.Length);







            Console.WriteLine(remainingRows.Count());
            Console.ReadLine();
        }

        public static void AddMRNMatches(IEnumerable<row> data, ref Dictionary<int, List<int>> matches)
        {
            var fourMillion = data.Where(r => r.MRN >= 4000000).OrderBy(r => r.MRN).ToArray();
            //Pair off and make a soft check on field to verify sameness
            for (int i = 0; i < fourMillion.Count(); i += 2)
            {
                var r = fourMillion[i];
                var s = fourMillion[i + 1];
                challenge.Program.Add(r, s, ref matches);
                if (_printActuals)
                {
                    PrintPair(r, s);
                }
            }
        }

        public static void AddMRNMatchesBenStyle(IEnumerable<row> data, ref Dictionary<int, List<int>> matches)
        {
            var fourMillion = data.Where(r => r.MRN >= 4000000).ToArray();
            //Pair off and make a soft check on field to verify sameness

            Random rand = new Random();

            using (StreamWriter fout = File.CreateText("C:/users/ben/desktop/distances.csv"))
            {
                for (int i = 0; i < fourMillion.Count(); i += 2)
                {
                    var r = fourMillion[i];
                    var s = fourMillion[i + 1];

                    //if (rand.Next() % 100 == 0)
                    {
                        double distance = Ben.EditDistance.ComputeDistanceForRecordPair(r, s);
                        //fout.WriteLine($"{r.MRN}, {s.MRN}, {distance}"); 

                        if(distance > .6)
                        {
                            fout.WriteLine(r.ToString());
                            fout.WriteLine(s.ToString());
                            fout.WriteLine();
                        }
                    }

                    challenge.Program.Add(r, s, ref matches);
                }
            }

            Console.WriteLine("done"); 
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
                    if (_printErrors)
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
                        if (_printActuals)
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

            Console.WriteLine(counter);
            return addedThisTime;
        }

        public static List<List<row>> AddSoftMatches(row[] remainingRows, ref Dictionary<int, List<int>> matches, int[] badSSNs)
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

                    if (!badSSNs.Contains(ri.SSN) && !badSSNs.Contains(rj.SSN) && challenge.Program.OneDifference(ri.SSN.ToString(), rj.SSN.ToString()))
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
