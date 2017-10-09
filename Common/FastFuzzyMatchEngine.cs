using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FastFuzzyMatchEngine
    {
        public static RowMatchObject FuzzyLastNameMatches(Row[] allData)
        {
            Console.WriteLine("Matching Last Names");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.LAST, 2);
        }

        public static RowMatchObject FuzzyFirstNameMatches(Row[] allData)
        {
            Console.WriteLine("Matching First Names");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.FIRST, 2);
        }

        public static RowMatchObject FuzzySSNMatches(Row[] allData)
        {
            Console.WriteLine("Matching SSN");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.SSN <= 0 ? "" : d.SSN.ToString(), 2);
        }

        public static RowMatchObject FuzzyAddressMatches(Row[] allData)
        {
            Console.WriteLine("Matching Address");
            FastBKTreeGrouper fastBKTreeGrouper = new FastBKTreeGrouper();
            return fastBKTreeGrouper.DistanceAtMostN(allData, d => d.ADDRESS1, 2); //Note : should work much better when address normalization comes in
        }

        public static RowMatchObject FuzzyDateMatches(Row[] allData)
        {
            Console.WriteLine("Matching DOB");
            FastFuzzyDateGrouper fastFuzzyDateGrouper = new FastFuzzyDateGrouper();
            return fastFuzzyDateGrouper.DistanceAtMostN(allData, d => d.DOB == default(DateTime) ? "" : d.DOB.ToString(), 1);
        }

        public static RowMatchObject FuzzyPhoneMatches(Row[] allData)
        {
            Console.WriteLine("Matching Phone");
            FastEditDistanceGrouper fastGrouper = new FastEditDistanceGrouper();
            return fastGrouper.DistanceAtMostN(allData, d => d.PHONE <= 0 ? "" : d.PHONE.ToString(), 1);
        }

        public static RowMatchObject FuzzyMRNMatches(Row[] allData)
        {
            Console.WriteLine("Matching MRN");
            FastMRNGrouper fastMRNGrouper = new FastMRNGrouper();
            return fastMRNGrouper.DistanceAtMostN(allData, r => r.MRN > 0 ? r.MRN.ToString() : "",1200);
        }

        public static RowMatchObject FuzzyMIDDLEMatches(Row[] allData)
        {
            Console.WriteLine("Matching Middle Names");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.MIDDLE, 2);
        }
        public static RowMatchObject FuzzySUFFIXMatches(Row[] allData)
        {
            Console.WriteLine("Matching SUFFIXES");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.SUFFIX, 2);
        }

        public static RowMatchObject FuzzyGENDERMatches(Row[] allData)
        {
            Console.WriteLine("Matching GENDER");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.GENDER, 0);
        }

        public static RowMatchObject FuzzyADDRESS2Matches(Row[] allData)
        {
            Console.WriteLine("Matching ADDRESS2");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.ADDRESS2, 2);
        }

        public static RowMatchObject FuzzyZIPMatches(Row[] allData)
        {
            Console.WriteLine("Matching ZIP");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.ZIP > 0 ? d.ZIP.ToString() : "", 2);
        }

        public static RowMatchObject FuzzyMOTHERS_MAIDEN_NAMEMatches(Row[] allData)
        {
            Console.WriteLine("Matching MOTHERS_MAIDEN_NAME");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.MOTHERS_MAIDEN_NAME, 2);
        }

        public static RowMatchObject FuzzyCITYMatches(Row[] allData)
        {
            Console.WriteLine("Matching CITY");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.CITY, 0);
        }

        public static RowMatchObject FuzzySTATEMatches(Row[] allData)
        {
            Console.WriteLine("Matching STATE");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.STATE, 0);
        }

        public static RowMatchObject FuzzyEMAILMatches(Row[] allData)
        {
            Console.WriteLine("Matching EMAIL");
            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();
            return fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.EMAIL == "" ? "" : d.EMAIL.Split('@').First(), 2);
        }

        //new string[] { "LAST", "FIRST", "MIDDLE", "SUFFIX", "DOB", "GENDER","SSN","ADDRESS1","ADDRESS2","ZIP","MOTHERS_MAIDEN_NAME","MRN","CITY","STATE","PHONE","EMAIL"};
        public static void SerializeFuzzyMatchData(Row[] allData, string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Serializer.Serialize<RowMatchObject>(FuzzyFirstNameMatches(allData), Path.Combine(directoryPath, "FIRSTMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyLastNameMatches(allData), Path.Combine(directoryPath, "LASTMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzySSNMatches(allData), Path.Combine(directoryPath, "SSNMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyDateMatches(allData), Path.Combine(directoryPath, "DOBMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyPhoneMatches(allData), Path.Combine(directoryPath, "PHONEMatches.dat"));
            //Serializer.Serialize<RowMatchObject>(FuzzyMRNMatches(allData), Path.Combine(directoryPath, "MRNMatches.dat"));
            Console.WriteLine("Skipping The MRN matching since it won't fit in memory when serializing");
            Serializer.Serialize<RowMatchObject>(FuzzyAddressMatches(allData), Path.Combine(directoryPath, "ADDRESS1Matches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyMIDDLEMatches(allData), Path.Combine(directoryPath, "MIDDLEMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzySUFFIXMatches(allData), Path.Combine(directoryPath, "SUFFIXMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyGENDERMatches(allData), Path.Combine(directoryPath, "GENDERMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyADDRESS2Matches(allData), Path.Combine(directoryPath, "ADDRESS2Matches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyZIPMatches(allData), Path.Combine(directoryPath, "ZIPMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyMOTHERS_MAIDEN_NAMEMatches(allData), Path.Combine(directoryPath, "MOTHERS_MAIDEN_NAMEMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyCITYMatches(allData), Path.Combine(directoryPath, "CITYMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzySTATEMatches(allData), Path.Combine(directoryPath, "STATEMatches.dat"));
            Serializer.Serialize<RowMatchObject>(FuzzyEMAILMatches(allData), Path.Combine(directoryPath, "EMAILMatches.dat"));
        }

        public static Matches FuzzyMatchOnNImportantFields(Row[] allData, string matchObjectDirectoryPath, int n)
        {
            int[] eids = allData.Select(d => d.EnterpriseID).ToArray();
            List<RowMatchObject> matchObjectsForFields = new List<RowMatchObject>();

            List<string> fileNames = new List<string> { "fuzzyFirstNameMatches.dat", "fuzzyLastNameMatches.dat", "fuzzySSNMatches.dat", "fuzzyDateMatches.dat", "fuzzyPhoneMatches.dat", "fuzzyMRNMatches.dat","fuzzyAddressMatches.dat" };

            foreach (var fileName in fileNames)
            {
                Console.WriteLine("Deserializing " + fileName);
                var rowMatchData = Serializer.Deserialize<RowMatchObject>(Path.Combine(matchObjectDirectoryPath, fileName));
                matchObjectsForFields.Add(rowMatchData);
            }

            return FuzzyMatchOnNImportantFields(eids, matchObjectsForFields, n, allData);
        }

        public static Matches FuzzyMatchOnNImportantFields(int[] eids, List<RowMatchObject> matchObjectsForFields, int n, Row[] allData)
        {
            int maxEid = eids.Max();
            Matches toReturn =  MatchesEngine.NewMatches(maxEid + 1);

            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();

            int c = 0;
            int[] eidToMatchCount = new int[maxEid + 1];
            List<int> usedEids = new List<int>();

            Random r = new Random();

            foreach (int eid in eids)
            {
                usedEids.Clear();
                Console.Write($"\r{c++}/{eids.Count()} Final Row Matches");
                foreach (var matchObject in matchObjectsForFields)
                {
                    int index = matchObject.EidToIndex[eid];
                    if (index == -1)
                        continue;
                    var neigborIndices = matchObject.Matches.Neighbors(index);
                    foreach (var neighborIndex in neigborIndices)
                    {
                        var neighborEids = matchObject.IndexToEids[neighborIndex.Index];
                        foreach (var neighborEid in neighborEids)
                        {
                            if (eid > neighborEid)  //We will do the exact same computation when we find all the matches for neighborEID.
                                continue;
                            usedEids.Add(neighborEid);
                            eidToMatchCount[neighborEid]++;
                        }
                    }
                }

                int bigNeighborCount = 0;
                foreach (var usedEid in usedEids)
                {
                    if (eidToMatchCount[usedEid] >= n)
                    {
                        toReturn.AddMatch(eid, usedEid, eidToMatchCount[usedEid]);  //One way of recording the NUMBER of fuzzy matches
                        bigNeighborCount++;
                    }

                    eidToMatchCount[usedEid] = 0;
                }

                //if (r.NextDouble() < .0001)
                //{
                //    Console.WriteLine("Delete me");
                //    Console.WriteLine(bigNeighborCount);
                //    int betterCount = 0;
                //    var myGuy = allData.Where(d => d.EnterpriseID == eid).First();
                //    foreach (var datum in allData)
                //    {
                //        if (eid <= datum.EnterpriseID)
                //        {
                //            if (EasiestAgreementCount(myGuy, datum) >= n)
                //            {
                //                betterCount++;
                //            }
                //        }
                //    }

                //    Console.WriteLine(betterCount);
                //}
            }

            Console.WriteLine("\nCleaning Two Field Fuzzy Match Object");
            toReturn.Clean();  //I think I've actually staged things in a way that makes this unnecessary
            Console.WriteLine("Done Cleaning");
            return toReturn;
        }

        public static int EasiestAgreementCount(Row ri, Row rj)
        {
            int fieldAgreement = 0;

            if (ri.LAST != "" && rj.LAST != "" && EditDistanceEngine.Compute(ri.LAST, rj.LAST) <= 2)
                fieldAgreement++;

            if (ri.FIRST != "" && rj.FIRST != "" && EditDistanceEngine.Compute(ri.FIRST, rj.FIRST) <= 2)
                fieldAgreement++;

            if (MatchingManager.FuzzierSSNMatch(ri.SSN, rj.SSN)) //!!!!!!! This changed !!!!!!!!
                fieldAgreement++;

            if (ri.ADDRESS1 != "" && rj.ADDRESS1 != "" && EditDistanceEngine.Compute(ri.ADDRESS1, rj.ADDRESS1) <= 2) 
                fieldAgreement++;

            if (MatchingManager.FuzzyDateEquals(ri.DOB, rj.DOB)) //!!!!!!! This changed !!!!!!!!
                fieldAgreement++;

            if (MatchingManager.FuzzyPhoneMatch(ri.PHONE, rj.PHONE))
                fieldAgreement++;

            if (System.Math.Abs(ri.MRN - rj.MRN) < 500)
                fieldAgreement++;

            return fieldAgreement;
        }
    }
}
