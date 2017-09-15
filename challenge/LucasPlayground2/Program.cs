using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground2
{
    class Program
    {
        private static bool _printErrors = false;
        private static bool _printActuals = false;
        private static bool _printLargeGroupValues = false;

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

        static void Main(string[] args)
        {
            // Load Data
            var lines = FileManager.GetLines();
            var allData = lines.Skip(1).Where(l => l != ",,,,,,,,,,,,,,,,,,").Select(l => FileManager.ParseRow(l)).ToArray();
            var realData = allData.Where(r => r.EnterpriseID >= 15374761).OrderBy(n => n.MRN).ToArray();


            // Clean Data
            DataCleaningManager.CleanData(ref allData, realData);


            // Load Data
            ClosedSets originalMatches = FileManager.LoadOriginalMatches(allData);
            ClosedSets newMatches = FileManager.LoadOriginalMatches(allData); // create a copy to edit


            MatchingManager matchingManager = new MatchingManager(_printErrors, _printActuals, _printLargeGroupValues);




            matchingManager.AddMatches("SSN + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
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


            matchingManager.AddMatches("Name + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
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

            matchingManager.AddMatches("Phone + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
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

            matchingManager.AddMatches("DOB + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
            {
                DOB = true,
            }), 1, (r1, r2) => MatchingManager.SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                Last = true,
                Phone = true,
                //DOB = true,
                Address = true,
            }), ref newMatches);

            matchingManager.AddMatches("Address + soft match", allData, r => MatchingManager.HardSelector(r, new FieldInclusions
            {
                Address = true,
            }), 1, (r1, r2) => MatchingManager.SoftMatchCount(r1, r2, new FieldInclusions
            {
                SSN = true,
                First = true,
                Last = true,
                Phone = true,
                DOB = true,
                //Address = true,
            }), ref newMatches);

            Console.ReadLine();
        }
    }
}
