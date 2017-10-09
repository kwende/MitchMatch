using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndressAddress.DataTypes;

namespace UndressAddress
{
    public static class MatchEngine
    {
        private static string PrepNameForDictionaryLookup(string name)
        {
            name = Regex.Replace(name, @"(EAST|WEST|SOUTH|NORTH) (\d+)", "$2");
            name = Regex.Replace(name, @" (EAST|WEST|SOUTH|NORTH)$", "");

            return name;
        }

        public static bool IsLevel1Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                address.Zip != null &&
                !string.IsNullOrEmpty(address.City))
            {
                string nameForDictionary = PrepNameForDictionaryLookup(address.StreetName);

                if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
                {
                    string fullStreetName = (nameForDictionary + " " + address.Suffix).Trim();
                    List<StreetName> names = data.StreetNameDictionary[nameForDictionary].Where(n =>
                        n.ZipCodes.Contains(address.Zip.Value) && n.Cities.Contains(address.City) &&
                        n.FullStreetName == fullStreetName).ToList();

                    if (names.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsLevel2Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                (!string.IsNullOrEmpty(address.City) || address.Zip.HasValue))
            {
                string nameForDictionary = PrepNameForDictionaryLookup(address.StreetName);

                if (!string.IsNullOrEmpty(address.City))
                {
                    if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
                    {
                        string fullStreetName = (address.StreetName + " " + address.Suffix).Trim();
                        if (data.StreetNameDictionary[nameForDictionary].Any(n => n.FullStreetName == fullStreetName &&
                             n.Cities.Contains(address.City)))
                        {
                            return true;
                        }
                    }
                }

                if (address.Zip.HasValue)
                {
                    if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
                    {
                        string fullStreetName = (address.StreetName + " " + address.Suffix).Trim();
                        if (data.StreetNameDictionary[nameForDictionary].Any(n => n.ZipCodes.Contains(address.Zip.Value) &&
                             n.FullStreetName == fullStreetName))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsLevel3Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                (!string.IsNullOrEmpty(address.City) || address.Zip.HasValue))
            {
                string nameForDictionary = PrepNameForDictionaryLookup(address.StreetName);

                if (!string.IsNullOrEmpty(address.City))
                {
                    if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
                    {
                        if (data.StreetNameDictionary[nameForDictionary].Any(n => n.Name == address.StreetName &&
                             n.Cities.Contains(address.City)))
                        {
                            return true;
                        }
                    }
                }

                if (address.Zip.HasValue)
                {
                    if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
                    {
                        if (data.StreetNameDictionary[nameForDictionary].Any(n => n.ZipCodes.Contains(address.Zip.Value) &&
                             n.Name == address.StreetName))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsLevel4Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                !string.IsNullOrEmpty(address.City))
            {
                List<string> alternateCities =
                    BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();

                if (alternateCities.Count > 1 || (alternateCities.Count == 1 && address.City != alternateCities[0]))
                {
                    string nameForDictionary = PrepNameForDictionaryLookup(address.StreetName);

                    foreach (string alternateCity in alternateCities)
                    {
                        if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
                        {
                            if (data.StreetNameDictionary[nameForDictionary].Any(n => n.Name == address.StreetName &&
                                 n.Cities.Contains(alternateCity)))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsLevel5Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName))
            {
                foreach (StreetName streetName in data.StreetData)
                {
                    if (streetName.FullStreetName == address.StreetName + " " + address.Suffix)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsLevel6Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName))
            {
                foreach (StreetName streetName in data.StreetData)
                {
                    if (streetName.Name == address.StreetName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsLevel7Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                address.Zip.HasValue)
            {
                List<string> alternateStreets =
                    BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

                if (alternateStreets.Count > 0)
                {
                    foreach (string alternateStreet in alternateStreets)
                    {
                        List<StreetName> streetNames = data.StreetNameDictionary[alternateStreet];
                        foreach (StreetName streetName in streetNames)
                        {
                            if (streetName.Cities.Contains(address.City) ||
                                streetName.ZipCodes.Contains(address.Zip.Value))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsLevel8Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                !string.IsNullOrEmpty(address.City))
            {
                List<string> alternateCities =
                    BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();

                List<string> alternateStreets =
                    BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

                if (alternateStreets.Count > 0 && alternateCities.Count > 0)
                {
                    foreach (string alternateStreet in alternateStreets)
                    {
                        List<StreetName> streetNames = data.StreetNameDictionary[alternateStreet];
                        foreach (StreetName streetName in streetNames)
                        {
                            foreach (string alternateCity in alternateCities)
                            {
                                if (streetName.Cities.Contains(alternateCity))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsLevel9Match(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName))
            {
                double[] editDistances =
                    BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().Select(
                        n => EditDistanceEngine.ComputeNormalized(n, address.StreetName)).ToArray();

                return editDistances.Length > 0 && editDistances.Any(n => n < .4f);
            }

            return false;
        }
    }
}
