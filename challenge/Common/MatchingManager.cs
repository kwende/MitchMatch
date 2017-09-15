using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class MatchingManager
    {

        private static bool _printErrors = false;
        private static bool _printActuals = false;
        private static bool _printLargeGroupValues = false;

        public MatchingManager(bool printErrors, bool printActuals, bool printLargeGroupValues)
        {
            _printErrors = printErrors;
            _printActuals = printActuals;
            _printLargeGroupValues = printLargeGroupValues;
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

            int editDistance = EditDistance.Compute(a.ADDRESS1, b.ADDRESS1);

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


        public List<int> AddMatches(string label, Row[] data, Func<Row, string> groupingValue, int softEqualsCount, Func<Row, Row, int> softEquals, ref ClosedSets matches)
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
                                }

                                if (_printActuals)
                                {
                                    PrintingManager.PrintPair(row1, row2);
                                }
                            }
                            else
                            {
                                thrownOutCounter++;

                                if (_printErrors)
                                {
                                    PrintingManager.PrintPair(row1, row2);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Groups thrown out: {thrownOutCounter}");
            Console.WriteLine($"Match added: {addedCounter}");
            Console.WriteLine($"Match modified: {modifiedCounter}");

            PrintingManager.PrintRemainingRowCount(data, matches);

            return toReturn;
        }


        public void FindAllMatches(Row[] allData, ref ClosedSets newMatches)
        {
            AddMatches("SSN + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
            {
                SSN = true,

            }), 1, (r1, r2) => MatchingManager.SoftMatchCount(r1, r2, new FieldInclusions
            {
                //SSN = true,
                First = true,
                Last = true,
                Phone = true,
                DOB = true,
                Address = true,
            }), ref newMatches);


            AddMatches("Name + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
            {
                Name = true,
            }), 1, (r1, r2) => MatchingManager.SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                //First = true,
                //Last = true,
                Phone = true,
                DOB = true,
                Address = true,
            }), ref newMatches);

            AddMatches("Phone + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
            {
                Phone = true,
            }), 1, (r1, r2) => MatchingManager.SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                Last = true,
                //Phone = true,
                DOB = true,
                Address = true,
            }), ref newMatches);



            ////******************  SOLID MATCHES   ******************//
            //AddMatches("SSN + LAST", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //    Last = true,
            //}), 0, (r1, r2) =>
            //    1);
            //AddMatches("SSN + DOB", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //}), 0, (r1, r2) =>
            //    1);
            //AddMatches("SSN + PHONE", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //    Phone = true,
            //}), 0, (r1, r2) =>
            //        1);
            //AddMatches("SSN + ADDRESS", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true,
            //    Address = true,
            //}), 0, (r1, r2) =>
            //    1);

            //AddMatches("NAME + DOB strong", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    Phone = true,
            //    Address = true,
            //}));
            //AddMatches("NAME + PHONE (no sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Phone = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //}));

            //AddMatches("NAME + ADDRESS (no sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //}));

            //AddMatches("DOB + PHONE (no twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //}));  // Josh Code Review : This only matches on First, DOB, and Phone : If it gets past the hard match, it will pass the softmatch
            //// Lucas Code Review : You're right, this should be soft match on Last or Address. I manually checked all of the ones that wouldn't meet that bar and they're ok.

            //AddMatches("DOB + ADDRESS (no twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //    Address = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //}));

            //AddMatches("PHONE + ADDRESS (no twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //    Address = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //}));


            //AddMatches("SSN + soft match", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    SSN = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Name = true, // Josh Code Review : Note that softmatching on Name returns at least 1 if ONE of First, Last soft matches.
            //    Phone = true,
            //    DOB = true,
            //    Address = true,
            //})); // Josh Code Review : Makes many of the SSN matches above redundant.
            //// Lucas Code Review : It was intended that that soft matching on name requires soft matching on both first and last. Manual review validates that that isn't necessary here. The other place it's used is in the weak matches regime, so has been hand validated.

            ////******************  PROBABLY SOLID MATCHES   ******************//

            //AddMatches("NAME + PHONE (sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Address = true,
            //}));
            //AddMatches("NAME + ADDRESS (sr/jr)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Phone = true,
            //}));
            //AddMatches("DOB + PHONE (twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Last = true,
            //    Address = true,
            //})); //Josh Code Review : This is actually a stronger match than one of the matches in the SOLID match block.  Also, it doesn't allow twins.
            //// Lucas Code Review : Agreed. This should not require hard match on First.
            //AddMatches("DOB + ADDRESS (twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Address = true,
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Last = true,
            //    Phone = true,
            //}));
            //AddMatches("PHONE + ADDRESS (twin)", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Last = true,
            //}));


            //AddMatches("Name + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    DOB = true,
            //    Phone = true,
            //    Address = true,
            //}));
            //AddMatches("DOB + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    DOB = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    Phone = true,
            //    Address = true,
            //}));
            //AddMatches("Phone + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    DOB = true,
            //    Address = true,
            //}));
            //AddMatches("Address + 2 soft", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Address = true,
            //}), 2, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //    First = true,
            //    DOB = true,
            //    Phone = true,
            //}));  //Josh code review : This could match on address, first name, and DOB.  Maybe it should go in the weaker matches category?
            //// Lucas Code review : I hand-checked these ~60 matches. They're all ok.

            ////******************  WEAKER MATCHES   ******************//

            //List<int> weakerMatchedIDs = new List<int>();
            //var weak =
            //AddMatches("PHONE + soft SSN", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSN = true,
            //}));
            //weakerMatchedIDs.AddRange(weak);

            //weak =
            //AddMatches("NAME + DOB weaker", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    DOB = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSNSoft = true,
            //}));
            //weakerMatchedIDs.AddRange(weak);

            //weak =
            //AddMatches("NAME + PHONE weaker", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Phone = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSNSoft = true,
            //}));
            //weakerMatchedIDs.AddRange(weak);

            //weak =
            //AddMatches("NAME + ADDRESS weaker", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Name = true,
            //    Address = true,
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    SSNSoft = true,
            //}));
            //weakerMatchedIDs.AddRange(weak);

            //weak =
            //AddMatches("PHONE + soft NAME", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    Name = true,
            //}));
            //weakerMatchedIDs.AddRange(weak);

            //weak =
            //AddMatches("PHONE + soft FIRST/DOB", data, ref matches, r => HardSelector(r, new FieldInclusions
            //{
            //    Phone = true
            //}), 1, (r1, r2) => SoftMatchCount(r1, r2, new FieldInclusions
            //{
            //    First = true,
            //    DOB = true,
            //}));
            //weakerMatchedIDs.AddRange(weak);
        }


    }
}

