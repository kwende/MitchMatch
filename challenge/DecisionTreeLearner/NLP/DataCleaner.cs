using DecisionTreeLearner.Data;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DecisionTreeLearner.NLP
{
    public static class DataCleaner
    {
        private static string[] BadSSNs =
        {
            "000-00-0000",
            "999-99-9999",
            "123-45-6789",
            "111-11-1111",
            "666-66-6666",
            "888-88-8888",
            "555-55-5555",
            "333-33-3333",
            "777-77-7777",
            "444-44-4444",
            "222-22-2222",
            "432-10-9876",
            "109-87-6543",
            "654-32-1098",
            "000-00-0001",
            "098-76-5432",
            "765-43-2109",
            "210-98-7654",
            "678-90-1234",
            "789-01-2345",
            "876-54-3210",
            "901-23-4567",
            "456-78-9012",
            "234-56-7890",
            "890-12-3456",
            "321-09-8765",
            "543-21-0987",
            "567-89-0123",
            "345-67-8901",
        };

        public static List<Record> CleanRecordPairs(List<Record> records)
        {
            Console.Write("Cleaning data...");
            //for(int c=0;c<pairs.Count;c++)
            Parallel.For(0, records.Count, c =>
            {
                records[c] = CleanRecord(records[c]);
            });
            Console.WriteLine("...done");

            return records;
        }

        public static List<RecordPair> CleanRecordPairs(List<RecordPair> pairs,
            string streetSuffixesFile)
        {
            Console.Write("Cleaning data...");
            //for(int c=0;c<pairs.Count;c++)
            Parallel.For(0, pairs.Count, c =>
            {
                pairs[c] = CleanRecordPair(pairs[c], streetSuffixesFile);
            });
            Console.WriteLine("...done");

            return pairs;
        }

        public static RecordPair CleanRecordPair(RecordPair pair, string streetSuffixesFile)
        {
            pair.Record1 = CleanRecord(pair.Record1);
            pair.Record2 = CleanRecord(pair.Record2);

            return pair;
        }

        public static Record CleanRecord(Record input)
        {
            List<Tuple<string, string>> suffixes = AddressSuffixLoader.GetStreetSuffixAbbreviationTuples();

            string cleaned =
                input.Address1.ToUpper().Replace(" WEST ", " W ").Replace(" EAST ", " E ").Replace(" NORTH ", " N ").Replace(" SOUTH ", " S ").Replace(" SO ", " S ");

            if (input.City == "BKLYN")
            {
                input.City = "BROOKLYN";
            }

            input.Address1 = Regex.Replace(input.Address1, @"(\d)(ST|ND|RD|TH)\b", "$1");

            if (BadSSNs.Contains(input.SSN))
            {
                input.SSN = "";
            }

            //foreach (Tuple<string, string> suffix in suffixes)
            for (int c = 0; c < suffixes.Count; c++)
            {
                Tuple<string, string> suffix = suffixes[c];
                if (cleaned.EndsWith(suffix.Item1))
                {
                    cleaned = cleaned.Replace($" {suffix.Item1}", $" {suffix.Item2}");
                }
            }

            input.Address1 = cleaned;

            return input;
        }


    }
}
