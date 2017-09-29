using System;
using System.Collections.Generic;
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

            // Load Data
            ClosedSets originalMatches = FileManager.LoadOriginalMatches(allData);
            ClosedSets newMatches = FileManager.LoadOriginalMatches(allData); // create a copy to edit

            // Match Data
            MatchingManager matchingManager = new MatchingManager(_printErrors, _printActuals, _printLargeGroupValues);
            matchingManager.FindAllMatches(allData, ref newMatches);

            //FileManager.SaveFinalSubmission(newMatches.ClosedRowSets(), @"C:\Users\jbrownkramer\Desktop\submission.csv");

            Console.ReadLine();
        }

        public static T Deserialize<T>(string filePath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fstream = new FileStream(filePath, FileMode.Open))
            {
                return (T)formatter.Deserialize(fstream);
            }
        }

        public static void Serialize<T>(T toSerialize, string filePath)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fstream = new FileStream(filePath, FileMode.Create))
            {
                formatter.Serialize(fstream, toSerialize);
            }
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
