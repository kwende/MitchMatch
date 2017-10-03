using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FastFuzzyMatchEngine
    {
        public static Matches EasiestAgreementMatch(Row[] allData)
        {
            int maxEid = allData.Max(d => d.EnterpriseID);
            Matches toReturn = new Matches(maxEid + 1);

            FastEditDistanceGrouper fastEditDistanceGrouper = new FastEditDistanceGrouper();

            //Do fuzzy match on two fields
            Console.WriteLine("Matching Last Names");
            var lastNameMatches = fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.LAST, 2);

            Console.WriteLine("Matching First Names");
            var firstNameMatches = fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.FIRST, 2);

            Console.WriteLine("Matching SSN");
            var ssnMatches = fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.SSN <= 0 ? "" : d.SSN.ToString(), 1);

            Console.WriteLine("Matching Address");
            FastBKTreeGrouper fastBKTreeGrouper = new FastBKTreeGrouper();
            var addressMatches = fastBKTreeGrouper.DistanceAtMostN(allData, d => d.ADDRESS1, 2); //Note : should work much better when address normalization comes in

            Console.WriteLine("Matching DOB");
            FastFuzzyDateGrouper fastFuzzyDateGrouper = new FastFuzzyDateGrouper();
            var dobMatches = fastFuzzyDateGrouper.DistanceAtMostN(allData, d => d.DOB == default(DateTime) ? "" : d.DOB.ToString(), 1);

            Console.WriteLine("Matching Phone");
            var phoneMatches = fastEditDistanceGrouper.DistanceAtMostN(allData, d => d.PHONE <= 0 ? "" : d.PHONE.ToString(), 1);
            

            List<RowMatchObject> matchObjects = new List<RowMatchObject> { lastNameMatches, firstNameMatches, ssnMatches, addressMatches, dobMatches, phoneMatches };

            int c = 0;
            int[] eidToMatchCount = new int[maxEid + 1];
            List<int> usedEids = new List<int>();
            foreach (var row in allData)
            {
                int rowEid = row.EnterpriseID;
                usedEids.Clear();
                Console.Write($"\r{c++}/{allData.Count()} Final Row Matches");
                foreach (var matchObject in matchObjects)
                {
                    int index = matchObject.EidToIndex[row.EnterpriseID];
                    if (index == -1)
                        continue;
                    var stringNeigborIndices = matchObject.Matches.Neighbors(index);
                    foreach (var neighborIndex in stringNeigborIndices)
                    {
                        var eids = matchObject.IndexToEids[neighborIndex];
                        foreach (var eid in eids)
                        {
                            if (eid <= rowEid)  //We will do the exact same computation when we find all the matches for eid.
                                continue;
                            usedEids.Add(eid);
                            eidToMatchCount[eid]++;
                        }
                    }
                }

                foreach (var eid in usedEids)
                {
                    if (eidToMatchCount[eid] >= 2)
                        toReturn.AddMatch(rowEid, eid);

                    eidToMatchCount[eid] = 0;
                }


            }

            Console.WriteLine("Cleaning");
            toReturn.Clean();  //I think I've actually staged things in a way that makes this unnecessary
            return toReturn;
        }
    }
}
