using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndressAddress.DataTypes;

namespace UndressAddress
{
    public static class DataLoader
    {
        private const string StreetSuffixesPath = "StreetSuffixes.csv";

        private static List<StateOfNewYorkAddressRange> LoadAddresses(string[] addresses, AddressSuffixes suffixes)
        {
            List<StateOfNewYorkAddressRange> ret = new List<StateOfNewYorkAddressRange>();

            //foreach (string address in addresses)
            Parallel.ForEach(addresses, address =>
            {
                string[] bits = address.Split(',').Select(n => n.Trim()).ToArray();

                string addressBit = bits[3];
                string numberBit = bits[2];
                string zip = bits[8];
                string city = bits[5];
                string nonNumberNumberAddress = "";

                string cleanedAddress = AddressUtility.CleanNYDatabaseAddress(addressBit, suffixes);

                int startNumber = -1, endNumber = -1;

                if (Regex.IsMatch(numberBit, @"^\d+-\d+$"))
                {
                    int[] numberBitBits = numberBit.Split('-').Select(n => int.Parse(n)).ToArray();

                    int start = 0, end = 0;

                    if (numberBitBits[0] > numberBitBits[1])
                    {
                        start = numberBitBits[1];
                        end = numberBitBits[0];
                    }
                    else
                    {
                        start = numberBitBits[0];
                        end = numberBitBits[1];
                    }

                    startNumber = start;
                    endNumber = end;
                }
                else if (Regex.IsMatch(numberBit, @"^\d+$"))
                {
                    startNumber = int.Parse(numberBit);
                }
                else if (numberBit.Length > 0)
                {
                    nonNumberNumberAddress = numberBit;
                }

                lock (ret)
                {
                    ret.Add(new StateOfNewYorkAddressRange
                    {
                        BuildingNumberEnd = endNumber,
                        BuildingNumberStart = startNumber,
                        City = city,
                        StreetName = cleanedAddress,
                        ZipCode = zip,
                        NonNumberBuildingNumber = nonNumberNumberAddress,
                    });
                }
            });

            return ret;
        }

        public static Data LoadData()
        {
            Data data = new Data();

            data.Suffixes = new AddressSuffixes();
            string[] streetSuffixLines = File.ReadAllLines(StreetSuffixesPath);
            data.Suffixes.ShortSuffixes = streetSuffixLines.Select(n => n.Split(',')[1]).ToArray();
            data.Suffixes.LongSuffixes = streetSuffixLines.Select(n => n.Split(',')[0]).ToArray();

            if (Environment.UserName.Contains("brush"))
            {
                data.FinalDataSet = File.ReadAllLines("c:/users/brush/desktop/finaldataset.csv");
            }
            else
            {
                data.FinalDataSet = File.ReadAllLines("c:/users/ben/desktop/finaldataset.csv");
            }

            string[] newYorkCityAddresses = File.ReadAllLines("city_of_new_york.csv").Skip(1).ToArray();
            data.AllAddresses = LoadAddresses(newYorkCityAddresses, data.Suffixes);

            List<string> uniques = File.ReadAllLines("allStreets.csv").ToList();

            // go through and identify each street with a long suffix. 
            // add to it the corresponding short suffix. 
            Parallel.ForEach(uniques, newYorkCityStreetLine =>
            {
                for (int c = 0; c < data.Suffixes.LongSuffixes.Length; c++)
                {
                    string longSuffix = data.Suffixes.LongSuffixes[c];
                    if (newYorkCityStreetLine.EndsWith(" " + longSuffix))
                    {
                        int index = newYorkCityStreetLine.LastIndexOf(" " + longSuffix);
                        string shortened = newYorkCityStreetLine.Substring(0, index)
                            + " " + data.Suffixes.ShortSuffixes[c];

                        if (shortened != newYorkCityStreetLine)
                        {
                            lock (uniques)
                            {
                                uniques.Add(shortened);
                            }
                        }
                    }
                }
            });

            data.NewYorkStateStreetNames = uniques.ToArray();

            data.UnknownAddresses = File.ReadAllLines("UnknownAddresses.csv");
            data.HomelessAddresses = File.ReadAllLines("HomelessAddresses.csv");

            data.Abbreviations = new Dictionary<string, string>();
            string[] nameValuePairs = File.ReadAllLines("Abbreviations.txt");
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(',').Select(n => n.Trim()).ToArray();
                data.Abbreviations.Add(bits[0], bits[1]);
            }

            nameValuePairs = File.ReadAllLines("SuffixReplacementKey.txt");
            data.SuffixReplacementKey = new Dictionary<string, string>();
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(',').Select(n => n.Trim()).ToArray();
                if (!data.SuffixReplacementKey.ContainsKey(bits[0]))
                {
                    data.SuffixReplacementKey.Add(bits[0], bits[1]);
                }
            }

            return data;
        }
    }
}
