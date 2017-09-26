﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    class Program
    {
        private static bool _printErrors = false;
        private static bool _printActuals = false;
        private static bool _printLargeGroupValues = false;

        static void Main(string[] args)
        {
            // Load Data
            var lines = FileManager.GetLines();
            var allData = lines.Skip(1).Where(l => l != ",,,,,,,,,,,,,,,,,,").Select(l => FileManager.ParseRow(l)).ToArray();
            var realData = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();


            // Clean Data
            DataCleaningManager.CleanData(ref allData, realData);

            var strings = BKTreeEngine.DistinctNonEmptyStrings(allData, d => d.ADDRESS1).ToList();
            var streetTypes = strings.GroupBy(s => s.Split(' ').Last()).ToArray();
            var streetTypePrefixes = streetTypes.Select(g => g.Select(s => s.Substring(0, s.Length - g.Key.Length)).ToArray()).ToArray();
            var streetTypeStrings = streetTypes.Select(g => g.Key).ToArray();
            var streetTypeNeighbors = FastEditDistanceGrouper.EditDistanceAtMostN(streetTypeStrings, 2);
            Console.WriteLine(streetTypeNeighbors.Pairs());
            for (int i = 0; i < streetTypeStrings.Length; i++)
            {
                foreach(var neighbor in streetTypeNeighbors.Neighbors(i))
                {
                    if (neighbor < i) //This will be handled elsewhere
                        continue;

                    int d = EditDistance.Compute(streetTypeStrings[i], streetTypeStrings[neighbor]);

                    FastEditDistanceGrouper.EditDistanceAtMostN(streetTypePrefixes[i], streetTypePrefixes[neighbor], 2 - d);
                }
            }
            
            

            //Matches fuzzyMatches = EasiestAgreementMatch(allData);
            //fuzzyMatches.Serialize(@"C:\Users\jbrownkramer\Desktop\fuzzyMatches.csv");


            // Load Data
            ClosedSets originalMatches = FileManager.LoadOriginalMatches(allData);
            ClosedSets newMatches = FileManager.LoadOriginalMatches(allData); // create a copy to edit

            // Match Data
            MatchingManager matchingManager = new MatchingManager(_printErrors, _printActuals, _printLargeGroupValues);
            matchingManager.FindAllMatches(allData, ref newMatches);

            //FileManager.SaveFinalSubmission(newMatches.ClosedRowSets(), @"C:\Users\jbrownkramer\Desktop\submission.csv");

            Console.ReadLine();
        }

        private static Matches EasiestAgreementMatch(Row[] allData)
        {
            int maxEid = allData.Max(d => d.EnterpriseID);
            Matches toReturn = new Matches(maxEid + 1);

            //Do fuzzy match on two fields
            Console.WriteLine("Matching Last Names");
            var lastNameMatches =  FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.LAST, 2);

            Console.WriteLine("Matching First Names");
            var firstNameMatches = FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.FIRST, 2);

            Console.WriteLine("Matching SSN");
            var ssnMatches = FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.SSN <= 0 ? "" : d.SSN.ToString(), 1);

            Console.WriteLine("Matching Address");
            var addressMatches = FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.ADDRESS1, 2); //Note : should work much better when address normalization comes in

            Console.WriteLine("Matching Phone");
            var phoneMatches = FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.PHONE <= 0 ? "" : d.PHONE.ToString(), 1);

            List<RowMatchObject> matchObjects = new List<RowMatchObject> { lastNameMatches, firstNameMatches, ssnMatches, addressMatches};

            int c = 0;
            int[] eidToMatchCount = new int[maxEid + 1];
            List<int> usedEids = new List<int>();
            foreach(var row in allData)
            {
                int rowEid = row.EnterpriseID;
                usedEids.Clear();
                Console.Write($"\r{c++}/{allData.Count()} Final Row Matches");
                foreach(var matchObject in matchObjects)
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

                foreach(var eid in usedEids)
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

        private static List<List<Row>> ComputeDifference(ClosedSets originalMatches, ClosedSets newMatches)
        {
            List<Row>[] originals = originalMatches.RowToClosedRowSet;
            List<Row>[] news = newMatches.RowToClosedRowSet;
            bool[] tested = new bool[originals.Length];
            List<List<Row>> difference = new List<List<Row>>();

            for (int i = 0; i < originals.Length; i++)
            {
                if (!tested[i] && originals[i].Count != news[i].Count)
                {
                    difference.Add(news[i]);
                }
                foreach (Row row in news[i])
                {
                    tested[newMatches.GetRowIndexFromEID(row.EnterpriseID)] = true;
                }
            }
            return difference;
        }
    }
}
