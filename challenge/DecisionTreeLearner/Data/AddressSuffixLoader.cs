using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Data
{
    public static class AddressSuffixLoader
    {
        private static object _syncObject = new object();
        private const string StreetSuffixesFilePath = "StreetSuffixes.csv";
        private static List<Tuple<string, string>> _suffixes = null;
        private static string[] _allSuffixesFlattened = null;
        public static List<Tuple<string, string>> GetStreetSuffixAbbreviationTuples()
        {
            lock (_syncObject)
            {
                if (_suffixes == null)
                {
                    _suffixes = new List<Tuple<string, string>>();

                    string[] lines = File.ReadAllLines(StreetSuffixesFilePath);
                    foreach (string line in lines)
                    {
                        string[] bits = line.Split(',').Select(n => n.Trim().ToUpper()).ToArray();
                        _suffixes.Add(new Tuple<string, string>(bits[0], bits[1]));
                    }
                }
                return _suffixes;
            }
        }

        public static string[] GetAllStreetSuffixes()
        {
            lock (_syncObject)
            {
                if (_allSuffixesFlattened == null)
                {
                    List<Tuple<string, string>> suffixes = GetStreetSuffixAbbreviationTuples();
                    _allSuffixesFlattened = new string[suffixes.Count * 2];

                    for (int c = 0; c < suffixes.Count; c++)
                    {
                        _allSuffixesFlattened[c * 2] = suffixes[c].Item1;
                        _allSuffixesFlattened[c * 2 + 1] = suffixes[c].Item2; 
                    }
                }
                return _allSuffixesFlattened; 
            }
        }
    }
}
