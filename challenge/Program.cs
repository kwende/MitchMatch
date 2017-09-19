using System;
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
            EasiestAgreementMatch(allData);

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
            Matches toReturn = new Matches(allData.Max(d => d.EnterpriseID));

            //Do fuzzy match on two fields
            Console.WriteLine("Matching Last Names");
            var lastNameMatches =  FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.LAST, 2);

            Console.WriteLine("Matching First Names");
            var firstNameMatches = FastEditDistanceGrouper.EditDistanceAtMostN(allData, d => d.FIRST, 2);

            List<RowMatchObject> matchObjects = new List<RowMatchObject> { lastNameMatches, firstNameMatches };

            int c = 0;
            foreach(var row in allData)
            {
                Console.Write($"{c++}/{allData.Count()} Final Row Matches");
                Dictionary<int, int> eidToMatchCount = new Dictionary<int, int>();
                foreach(var matchObject in matchObjects)
                {
                    int index = matchObject.StringToArrayIndex[matchObject.FieldSelector(row)];
                    var stringNeigborIndices = matchObject.StringMatches.Neighbors(index);
                    foreach (var neighborIndex in stringNeigborIndices)
                    {
                        var rows = matchObject.RowsWithThisField[neighborIndex];
                        foreach (var row2 in rows)
                        {
                            if (!eidToMatchCount.ContainsKey(row2.EnterpriseID))
                            {
                                eidToMatchCount[row2.EnterpriseID] = 1;
                            }
                            else
                            {
                                eidToMatchCount[row2.EnterpriseID]++;
                            }
                        }
                    }
                }

                foreach(var pair in eidToMatchCount)
                {
                    if (pair.Value >= 2 && pair.Key != row.EnterpriseID)
                        toReturn.AddMatch(row.EnterpriseID, pair.Key);
                }
            }

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
