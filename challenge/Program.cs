using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace challenge
{
    class Program
    {
        private static bool _printErrors = false;
        private static bool _printActuals = true;
        private static bool _printLargeGroupValues = false;
        private static string _dataDirectoryPath = @"C:\Users\jbrownkramer\Desktop\PatientMatchingData";

        static void Main(string[] args)
        {

            // Load Data
            var lines = FileLibrary.GetLines();
            Row[] allData = lines.Skip(1).Where(l => l != ",,,,,,,,,,,,,,,,,,").Select(l => FileLibrary.ParseRow(l)).ToArray();
            var realData = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();

            // Clean Data
            Console.WriteLine("Cleaning Rows");
            DataCleaningManager.CleanData(ref allData, realData);
            Console.WriteLine("Done Cleaning Rows");

            // Load Data
            ClosedSets originalMatches = FileLibrary.LoadOriginalMatches(allData);
            ClosedSets newMatches = FileLibrary.LoadOriginalMatches(allData); // create a copy to edit

            // Match Data
            MatchingManager matchingManager = new MatchingManager(_printErrors, _printActuals, _printLargeGroupValues);
            matchingManager.FindAllMatches(allData, ref newMatches);

            FileLibrary.SaveFinalSubmission(newMatches.ClosedRowSets(), @"submission.csv");

            Console.ReadLine();
        }

        public static bool NonTrivial(Matches matches)
        {
            return matches.MatchArray.Any(l => l != null && l.Count() > 1);
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
