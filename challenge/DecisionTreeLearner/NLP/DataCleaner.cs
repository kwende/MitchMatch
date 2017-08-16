using DecisionTreeLearner.Data;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.NLP
{
    public static class DataCleaner
    {
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
                input.Address1.ToUpper().Replace(" WEST ", " W ").Replace(" EAST ", " E ").Replace(" NORTH ", " N ").Replace(" SOUTH ", " S ");

            if (input.City == "BKLYN")
            {
                input.City = "BROOKLYN";
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
