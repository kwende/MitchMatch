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
        private static List<Tuple<string, string>> _suffixes = null;

        private static List<Tuple<string, string>> LazyLoadStreetSuffixes(string streetSuffixesFile)
        {
            if (_suffixes == null)
            {
                _suffixes = new List<Tuple<string, string>>();

                string[] lines = File.ReadAllLines(streetSuffixesFile);
                foreach (string line in lines)
                {
                    string[] bits = line.Split(',').Select(n => n.Trim().ToUpper()).ToArray();
                    _suffixes.Add(new Tuple<string, string>(bits[0], bits[1]));
                }
            }

            return _suffixes;
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
            pair.Record1 = CleanRecord(pair.Record1, streetSuffixesFile);
            pair.Record2 = CleanRecord(pair.Record2, streetSuffixesFile);

            return pair; 
        }

        public static Record CleanRecord(Record input, string streetSuffixesFile)
        {
            List<Tuple<string, string>> suffixes = null;

            lock(typeof(DataCleaner))
            {
                suffixes = LazyLoadStreetSuffixes(streetSuffixesFile);
            }

            string cleaned =
                input.Address1.ToUpper().Replace(" WEST ", " W ").Replace(" EAST ", " E ").Replace(" NORTH ", " N ").Replace(" SOUTH ", " S ");

            foreach (Tuple<string, string> suffix in suffixes)
            {
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
