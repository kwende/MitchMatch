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

        private static string[] LoadNYStateStreets(Data data)
        {
            //string[] streets = File.ReadAllLines("allStreets.csv");
            string[] streets = File.ReadAllLines("state_of_new_york_revised.csv").ToArray();
            return streets.Distinct().ToArray();

            string[] streetsRevised = new string[streets.Length + 1];

            List<string> streetsCleaned = new List<string>();
            for (int i = 0; i < streets.Length; i++)
            {
                string street = streets[i];
                street = AddressUtility.CleanSpacesAndPunctuation(street);
                street = AddressUtility.CleanAddressFormat(street, data.AbbreviationsShortened);
                street = AddressUtility.NormalizeSuffix(street, data).FullStreetName;
                streetsCleaned.Add(street);
                streetsRevised[i] = street;
                //Console.Write($"{streets[i]} => {street},     ");
            }

            File.WriteAllLines("state_of_new_york_revised.csv", streetsRevised);

            return streetsCleaned.Distinct().ToArray();
        }

        private static List<StateOfNewYorkAddressRange> LoadNYCityAddresses(Data data)
        {
            string[] addresses = File.ReadAllLines("city_of_new_york_revised.csv").Skip(1).ToArray();
            //string[] addressesRevised = new string[addresses.Length + 1];
            //addressesRevised[0] = addresses[0];
            //int counter = 1;

            List<StateOfNewYorkAddressRange> ret = new List<StateOfNewYorkAddressRange>();

            foreach (string address in addresses)
            {
                string[] bits = address.Split(',').Select(n => n.Trim()).ToArray();

                string addressBit = bits[3];
                string numberBit = bits[2];
                int zip = -1;
                int.TryParse(bits[8], out zip);
                string city = bits[5];
                string nonNumberNumberAddress = "";



                //// Clean up addressBit
                //addressBit = AddressUtility.CleanSpacesAndPunctuation(addressBit);
                //addressBit = AddressUtility.CleanAddressFormat(addressBit, data.AbbreviationsShortened);
                Address cleanAddress = AddressUtility.NormalizeSuffix(addressBit, data);
                ////Console.Write($"{bits[3]} => {cleanAddress.FullStreetName},     ");
                //addressesRevised[counter++] = string.Join(",", bits.Take(3)) + $",{cleanAddress.FullStreetName}," + string.Join(",",bits.Skip(4));

                //if(cleanAddress.FullStreetName != addressBit)
                //{
                //    Console.WriteLine($"{addressBit} => {cleanAddress.FullStreetName}");
                //}

                // Parse numberBit
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

                lock (ret)
                {
                    if (!string.IsNullOrEmpty(nonNumberNumberAddress))
                    {
                        ret.Add(new StateOfNewYorkAddressRange
                        {
                            StreetNumber = new StreetNumberRange(nonNumberNumberAddress),
                            City = city,
                            StreetName = cleanAddress.StreetName,
                            ZipCode = zip,
                            Suffix = cleanAddress.Suffix,
                            FullStreetName = cleanAddress.FullStreetName,
                        });
                    }
                    else
                    {
                        ret.Add(new StateOfNewYorkAddressRange
                        {
                            StreetNumber = new StreetNumberRange(startNumber, endNumber),
                            City = city,
                            StreetName = cleanAddress.StreetName,
                            ZipCode = zip,
                            Suffix = cleanAddress.Suffix,
                            FullStreetName = cleanAddress.FullStreetName,
                        });
                    }
                }
            }

            //File.WriteAllLines("city_of_new_york_revised.csv", addressesRevised);


            return ret;
        }

        public static Data LoadDataBen(bool regenerateBKTree)
        {
            Data data = new Data();

            // RawData
            data.FinalDataSet = FileLibrary.GetLines().Skip(1).Where(l => l != ",,,,,,,,,,,,,,,,,,").ToArray();
            // Suffixes
            data.Suffixes = new AddressSuffixes();
            string[] streetSuffixLines = File.ReadAllLines(StreetSuffixesPath);
            data.Suffixes.ShortSuffixes = streetSuffixLines.Select(n => n.Split(',')[1]).ToArray();
            data.Suffixes.LongSuffixes = streetSuffixLines.Select(n => n.Split(',')[0]).ToArray();

            // Unknown and Homeless
            data.UnknownAddresses = File.ReadAllLines("UnknownAddresses.csv");
            data.HomelessAddresses = File.ReadAllLines("HomelessAddresses.csv");

            // Abbreviations
            data.Abbreviations = new Dictionary<string, string>();
            string[] nameValuePairs = File.ReadAllLines("Abbreviations.txt");
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(',').Select(n => n.Trim()).ToArray();
                data.Abbreviations.Add(bits[0], bits[1]);
            }

            data.AbbreviationsShortened = new Dictionary<string, string>();
            nameValuePairs = File.ReadAllLines("AbbreviationsShortened.txt");
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(',').Select(n => n.Trim()).ToArray();
                data.AbbreviationsShortened.Add(bits[0], bits[1]);
            }

            // SuffixReplacementKey
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

            // KnownCenters
            nameValuePairs = File.ReadAllLines("KnownCenters.txt");
            data.KnownCenters = new Dictionary<string, Address>();
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(';').Select(n => n.Trim()).ToArray();
                string[] rhsAddressParts = bits[1].Split(',').Select(n => n.Trim()).ToArray();
                Address address = new Address
                {
                    CenterName = rhsAddressParts[0],
                    StreetNumber = rhsAddressParts[1],
                    StreetName = rhsAddressParts[2],
                    City = rhsAddressParts[3],
                    State = rhsAddressParts[4],
                };
                if (rhsAddressParts[5].Length > 0)
                {
                    address.Zip = int.Parse(rhsAddressParts[5]);
                }

                address.FullStreetName = (address.StreetNumber != "" ? $"{address.StreetNumber} {address.StreetName}" : address.StreetName);
                data.KnownCenters.Add(bits[0], address);
            }


            // AlternateSuffixList
            string[] lines = File.ReadAllLines("streetToSuffixTable.txt");
            data.AlternateSuffixList = new Dictionary<string, List<string>>();
            foreach (string line in lines)
            {
                string[] halves = line.Split(':');
                List<string> alternates = halves[1].Split(',').ToList();
                data.AlternateSuffixList.Add(halves[0], alternates);
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fin = File.OpenRead("streetNames.dat"))
            {
                data.StreetData = (List<StreetName>)bf.Deserialize(fin);
            }

            List<string> justStreetNames = new List<string>();
            foreach (StreetName streetName in data.StreetData)
            {
                justStreetNames.Add(streetName.Name);
            }

            justStreetNames = justStreetNames.Distinct().ToList();

            //data.NYCityStreets = LoadNYCityAddresses(data);
            //data.NYStateStreets = LoadNYStateStreets(data);

            // BKTree
            if (regenerateBKTree)
            {
                data.StreetNameBKTree = BKTreeEngine.CreateBKTree(justStreetNames);
                //BKTreeSerializer.SerializeTo(data.BKTree, "bkTree.dat");
            }
            else
            {
                data.StreetNameBKTree = BKTreeSerializer.DeserializeFrom("bkTree.dat");
            }

            data.CityNameBKTree = BKTreeSerializer.DeserializeFrom("citiesBKTree.dat");

            data.KnownCities = new List<string>(File.ReadAllLines("knownCities.csv"));

            bf = new BinaryFormatter();
            using (FileStream fin = File.OpenRead("streetNameCity2Zips.dat"))
            {
                data.StreetNameCity2Zips = (Dictionary<StreetNameAndCity, List<int>>)bf.Deserialize(fin);
            }

            bf = new BinaryFormatter();
            using (FileStream fin = File.OpenRead("streetNameZip2Cities.dat"))
            {
                data.StreetNameZip2Cities = (Dictionary<StreetNameAndZip, List<string>>)bf.Deserialize(fin);
            }

            return data;
        }

        public static Data LoadJustSuffixes()
        {
            Data data = new Data();

            // RawData
            data.FinalDataSet = FileLibrary.GetLines().Skip(1).Where(l => l != ",,,,,,,,,,,,,,,,,,").ToArray();
            // Suffixes
            data.Suffixes = new AddressSuffixes();
            string[] streetSuffixLines = File.ReadAllLines(StreetSuffixesPath);
            data.Suffixes.ShortSuffixes = streetSuffixLines.Select(n => n.Split(',')[1]).ToArray();
            data.Suffixes.LongSuffixes = streetSuffixLines.Select(n => n.Split(',')[0]).ToArray();

            return data;
        }

        public static Data LoadData(bool regenerateBKTree)
        {
            Data data = new Data();

            // RawData
            data.FinalDataSet = FileLibrary.GetLines().Skip(1).Where(l => l != ",,,,,,,,,,,,,,,,,,").ToArray();
            // Suffixes
            data.Suffixes = new AddressSuffixes();
            string[] streetSuffixLines = File.ReadAllLines(StreetSuffixesPath);
            data.Suffixes.ShortSuffixes = streetSuffixLines.Select(n => n.Split(',')[1]).ToArray();
            data.Suffixes.LongSuffixes = streetSuffixLines.Select(n => n.Split(',')[0]).ToArray();

            // Unknown and Homeless
            data.UnknownAddresses = File.ReadAllLines("UnknownAddresses.csv");
            data.HomelessAddresses = File.ReadAllLines("HomelessAddresses.csv");

            // Abbreviations
            data.Abbreviations = new Dictionary<string, string>();
            string[] nameValuePairs = File.ReadAllLines("Abbreviations.txt");
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(',').Select(n => n.Trim()).ToArray();
                data.Abbreviations.Add(bits[0], bits[1]);
            }

            data.AbbreviationsShortened = new Dictionary<string, string>();
            nameValuePairs = File.ReadAllLines("AbbreviationsShortened.txt");
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(',').Select(n => n.Trim()).ToArray();
                data.AbbreviationsShortened.Add(bits[0], bits[1]);
            }

            // SuffixReplacementKey
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

            // KnownCenters
            nameValuePairs = File.ReadAllLines("KnownCenters.txt");
            data.KnownCenters = new Dictionary<string, Address>();
            foreach (string nameValuePair in nameValuePairs)
            {
                string[] bits = nameValuePair.Split(';').Select(n => n.Trim()).ToArray();
                string[] rhsAddressParts = bits[1].Split(',').Select(n => n.Trim()).ToArray();
                Address address = new Address
                {
                    CenterName = rhsAddressParts[0],
                    StreetNumber = rhsAddressParts[1],
                    StreetName = rhsAddressParts[2],
                    City = rhsAddressParts[3],
                    State = rhsAddressParts[4],
                };
                if (rhsAddressParts[5].Length > 0)
                {
                    address.Zip = int.Parse(rhsAddressParts[5]);
                }

                address.FullStreetName = (address.StreetNumber != "" ? $"{address.StreetNumber} {address.StreetName}" : address.StreetName);
                data.KnownCenters.Add(bits[0], address);
            }


            // AlternateSuffixList
            string[] lines = File.ReadAllLines("streetToSuffixTable.txt");
            data.AlternateSuffixList = new Dictionary<string, List<string>>();
            foreach (string line in lines)
            {
                string[] halves = line.Split(':');
                List<string> alternates = halves[1].Split(',').ToList();
                data.AlternateSuffixList.Add(halves[0], alternates);
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fin = File.OpenRead("streetNames.dat"))
            {
                data.StreetData = (List<StreetName>)bf.Deserialize(fin);
            }

            data.NYCityStreets = LoadNYCityAddresses(data);
            data.NYStateStreets = LoadNYStateStreets(data);

            // BKTree
            if (regenerateBKTree)
            {
                data.StreetNameBKTree = BKTreeEngine.CreateBKTree(data.NYStateStreets.ToList());
                //BKTreeSerializer.SerializeTo(data.BKTree, "bkTree.dat");
            }
            else
            {
                data.StreetNameBKTree = BKTreeSerializer.DeserializeFrom("bkTree.dat");
            }

            data.CityNameBKTree = BKTreeSerializer.DeserializeFrom("citiesBKTree.dat");

            data.KnownCities = new List<string>(File.ReadAllLines("knownCities.csv"));

            return data;
        }
    }
}
