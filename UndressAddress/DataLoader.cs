using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
                int zip = -1;
                int.TryParse(bits[8], out zip);
                string city = bits[5];
                string nonNumberNumberAddress = "";

                addressBit = Regex.Replace(addressBit, " +", " ");

                string[] cardinalDirections = { "N", "S", "E", "W" };

                string identifiedDirection = "";
                foreach (string cardinalDirection in cardinalDirections)
                {
                    int startIndex = -1, length = -1;
                    string endsWithString = " " + cardinalDirection;
                    string startsWithString = cardinalDirection + " ";
                    string containsString = " " + cardinalDirection + " ";

                    if (addressBit.EndsWith(endsWithString))
                    {
                        startIndex = addressBit.LastIndexOf(endsWithString);
                        length = endsWithString.Length;
                    }
                    else if (addressBit.StartsWith(cardinalDirection + " "))
                    {
                        startIndex = addressBit.IndexOf(startsWithString);
                        length = startsWithString.Length;
                    }
                    else if (address.Contains(containsString))
                    {
                        startIndex = addressBit.IndexOf(containsString);
                        length = containsString.Length;
                    }

                    if (startIndex != -1)
                    {
                        identifiedDirection = cardinalDirection;
                        addressBit = addressBit.Remove(startIndex, length).Trim();
                        break;
                    }
                }

                string identifiedSuffix = "";
                for (int c = 0; c < suffixes.LongSuffixes.Length; c++)
                {
                    string targetSuffix = " " + suffixes.LongSuffixes[c];
                    if (addressBit.EndsWith(targetSuffix))
                    {
                        identifiedSuffix = suffixes.ShortSuffixes[c];
                        int lastIndexOf = addressBit.LastIndexOf(targetSuffix);
                        addressBit = addressBit.Remove(lastIndexOf, targetSuffix.Length);
                        break;
                    }

                    targetSuffix = " " + suffixes.ShortSuffixes[c];
                    if (addressBit.EndsWith(targetSuffix))
                    {
                        identifiedSuffix = suffixes.ShortSuffixes[c];
                        int lastIndexOf = addressBit.LastIndexOf(targetSuffix);
                        addressBit = addressBit.Remove(lastIndexOf, targetSuffix.Length);
                        break;
                    }
                }

                string cleanedAddress = addressBit;

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
                    endNumber = int.Parse(numberBit);
                }
                else if (numberBit.Length > 0)
                {
                    nonNumberNumberAddress = numberBit;
                }

                string fullStreetName = "";
                if(identifiedDirection != "")
                {
                    switch(identifiedDirection)
                    {
                        case "E":
                            fullStreetName = "EAST ";
                            break;
                        case "W":
                            fullStreetName = "WEST ";
                            break;
                        case "N":
                            fullStreetName = "NORTH ";
                            break;
                        case "S":
                            fullStreetName = "SOUTH ";
                            break;
                    }
                }
                fullStreetName += cleanedAddress;
                if(identifiedSuffix != "")
                {
                    fullStreetName += " " + identifiedSuffix;
                }


                lock (ret)
                {
                    if (!string.IsNullOrEmpty(nonNumberNumberAddress))
                    {
                        ret.Add(new StateOfNewYorkAddressRange
                        {
                            StreetNumber = new StreetNumberRange(nonNumberNumberAddress),
                            City = city,
                            StreetName = cleanedAddress,
                            ZipCode = zip,
                            CardinalDirection = identifiedDirection,
                            Suffix = identifiedSuffix,
                            FullStreetName = fullStreetName,
                        });
                    }
                    else
                    {
                        ret.Add(new StateOfNewYorkAddressRange
                        {
                            StreetNumber = new StreetNumberRange(startNumber, endNumber),
                            City = city,
                            StreetName = cleanedAddress,
                            ZipCode = zip,
                            CardinalDirection = identifiedDirection,
                            Suffix = identifiedSuffix,
                            FullStreetName = fullStreetName,
                        });
                    }
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

            data.FinalDataSet = FileLibrary.GetLines().ToArray();
            string[] newYorkCityAddresses = File.ReadAllLines("city_of_new_york.csv").Skip(1).ToArray();
            data.AllAddresses = LoadAddresses(newYorkCityAddresses, data.Suffixes);

            List<string> uniques = File.ReadAllLines("allStreets.csv").Distinct().ToList();

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

            // at the moment I'm focusing on the "main" ones, and so this
            // format in the file may change to accomodate the various 
            // alternative addresses for centers, etc. don't get too stuck 
            // on how I'm doing this, it can change. 
            nameValuePairs = File.ReadAllLines("KnownCenters.txt");
            data.KnownCenters = new Dictionary<string, Address>();
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(';').Select(n => n.Trim()).ToArray();
                string[] rhsAddressParts = bits[1].Split(',').Select(n => n.Trim()).ToArray();
                data.KnownCenters.Add(bits[0], new Address
                {
                    City = rhsAddressParts[1],
                    StreetName = rhsAddressParts[0],
                    Zip = int.Parse(rhsAddressParts[3]),
                    RawAddress1 = rhsAddressParts[0]
                });
            }

            data.BKTree = BKTreeSerializer.DeserializeFrom("bkTree.dat");

            string[] lines = File.ReadAllLines("streetToSuffixTable.txt");
            data.AlternateSuffixList = new Dictionary<string, List<string>>();
            foreach (string line in lines)
            {
                string[] halves = line.Split(':');
                List<string> alternates = halves[1].Split(',').ToList();
                data.AlternateSuffixList.Add(halves[0], alternates);
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fin = File.OpenRead("addressDictionary.dat"))
            {
                data.StreetNamesToStreetNumbers = (Dictionary<string, Dictionary<int, List<string>>>)bf.Deserialize(fin);
            }

            return data;
        }
    }
}
