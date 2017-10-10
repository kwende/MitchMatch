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
        /// <summary>
        /// Straightup perfect match. Nothing to do to this address. 
        /// </summary>
        /// <returns></returns>
        public static bool IsPerfectMatchIncludingZipAndCity(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                address.Zip != null &&
                !string.IsNullOrEmpty(address.City))
            {
                string streetNameToSearch = (address.StreetName + " " + address.Suffix).Trim();

                return data.StreetData.Any(n => n.FullStreetName == streetNameToSearch &&
                    n.Cities.Contains(address.City) && n.ZipCodes.Contains(address.Zip.Value));
            }

            return false;
        }

        /// <summary>
        /// Maybe portions of the address have been rearranged. The west is at the end, or the avenue is placed
        /// before an apartment number or something wonky. But otherwise most of the parts are there. 
        /// </summary>
        /// <returns></returns>
        public static bool IsRearrangedAddressAndCityZipMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 address.Zip != null &&
                 !string.IsNullOrEmpty(address.City))
            {

                StreetName bestMatch = null;
                int highestMatchNumber = 0;
                foreach (StreetName name in data.StreetData)
                {
                    // demand the stem and city/zip are matches
                    if (!string.IsNullOrEmpty(name.Name) && name.Cities.Contains(address.City) &&
                        name.ZipCodes.Contains(address.Zip.Value) &&
                        StringUtility.Contains(address.FullStreetName, name.Name))
                    {
                        // demand the suffix, somewhere (if it exists)
                        List<string> partsToCheck = new List<string>();

                        // pretype if exists. 
                        if (!string.IsNullOrEmpty(name.PreType))
                        {
                            partsToCheck.Add(name.PreType);
                        }

                        if (!string.IsNullOrEmpty(name.Suffix))
                        {
                            partsToCheck.Add(name.Suffix);
                        }

                        if (!string.IsNullOrEmpty(name.PreDirection))
                        {
                            if (name.PreDirection == "E")
                            {
                                partsToCheck.Add("EAST");
                            }
                            else if (name.PreDirection == "W")
                            {
                                partsToCheck.Add("WEST");
                            }
                            else if (name.PreDirection == "S")
                            {
                                partsToCheck.Add("SOUTH");
                            }
                            else if (name.PreDirection == "N")
                            {
                                partsToCheck.Add("NORTH");
                            }
                        }

                        string fullName = string.Join(" ", address.StreetNumber, address.StreetName, address.Suffix);

                        int matchNumber = 0;
                        foreach (string partToCheck in partsToCheck)
                        {
                            if (StringUtility.Contains(fullName, partToCheck))
                            {
                                matchNumber++;
                            }
                        }

                        if (matchNumber > highestMatchNumber)
                        {
                            highestMatchNumber = matchNumber;
                            bestMatch = name;
                        }
                    }
                }

                return highestMatchNumber > 0;
            }

            return false;
        }

        /// <summary>
        /// Maybe portions of the address have been rearranged. The west is at the end, or the avenue is placed
        /// before an apartment number or something wonky. But otherwise most of the parts are there. 
        /// </summary>
        /// <returns></returns>
        public static bool IsRearrangedAddressAndCityOrZipMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 (address.Zip != null ||
                 !string.IsNullOrEmpty(address.City)))
            {
                StreetName bestMatch = null;
                int highestMatchNumber = 0;

                StreetName[] possibl = data.StreetData.Where(n => n.Name == "MORRIS").ToArray();

                foreach (StreetName name in data.StreetData)
                {
                    // demand the stem and city/zip are matches
                    if (!string.IsNullOrEmpty(name.Name) && (name.Cities.Contains(address.City) ||
                        (address.Zip.HasValue && name.ZipCodes.Contains(address.Zip.Value)) &&
                        StringUtility.Contains(address.FullStreetName, name.Name)))
                    {
                        // demand the suffix, somewhere (if it exists)
                        List<string> partsToCheck = new List<string>();

                        // pretype if exists. 
                        if (!string.IsNullOrEmpty(name.PreType))
                        {
                            partsToCheck.Add(name.PreType);
                        }

                        if (!string.IsNullOrEmpty(name.Suffix))
                        {
                            partsToCheck.Add(name.Suffix);
                        }

                        if (!string.IsNullOrEmpty(name.PreDirection))
                        {
                            if (name.PreDirection == "E")
                            {
                                partsToCheck.Add("EAST");
                            }
                            else if (name.PreDirection == "W")
                            {
                                partsToCheck.Add("WEST");
                            }
                            else if (name.PreDirection == "S")
                            {
                                partsToCheck.Add("SOUTH");
                            }
                            else if (name.PreDirection == "N")
                            {
                                partsToCheck.Add("NORTH");
                            }
                        }

                        string fullName = string.Join(" ", address.StreetNumber, address.StreetName, address.Suffix);

                        int matchNumber = 0;
                        foreach (string partToCheck in partsToCheck)
                        {
                            if (StringUtility.Contains(fullName, partToCheck))
                            {
                                matchNumber++;
                            }
                        }

                        if (matchNumber > highestMatchNumber)
                        {
                            highestMatchNumber = matchNumber;
                            bestMatch = name;
                        }
                    }
                }

                return highestMatchNumber > 0;
            }

            return false;
        }

        public static bool IsRearrangedAddressAndSoftCityMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 !string.IsNullOrEmpty(address.City))
            {
                List<string> softMatchedCities = BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();

                StreetName bestMatch = null;
                int highestMatchNumber = 0;
                foreach (StreetName name in data.StreetData)
                {
                    foreach (string softMatchedCity in softMatchedCities)
                    {
                        // demand the stem and city/zip are matches
                        if (!string.IsNullOrEmpty(name.Name) && name.Cities.Contains(softMatchedCity) &&
                            StringUtility.Contains(address.FullStreetName, name.Name))
                        {
                            // demand the suffix, somewhere (if it exists)
                            List<string> partsToCheck = new List<string>();

                            // pretype if exists. 
                            if (!string.IsNullOrEmpty(name.PreType))
                            {
                                partsToCheck.Add(name.PreType);
                            }

                            if (!string.IsNullOrEmpty(name.Suffix))
                            {
                                partsToCheck.Add(name.Suffix);
                            }

                            if (!string.IsNullOrEmpty(name.PreDirection))
                            {
                                if (name.PreDirection == "E")
                                {
                                    partsToCheck.Add("EAST");
                                }
                                else if (name.PreDirection == "W")
                                {
                                    partsToCheck.Add("WEST");
                                }
                                else if (name.PreDirection == "S")
                                {
                                    partsToCheck.Add("SOUTH");
                                }
                                else if (name.PreDirection == "N")
                                {
                                    partsToCheck.Add("NORTH");
                                }
                            }

                            string fullName = string.Join(" ", address.StreetNumber, address.StreetName, address.Suffix);

                            int matchNumber = 0;
                            foreach (string partToCheck in partsToCheck)
                            {
                                if (StringUtility.Contains(fullName, partToCheck))
                                {
                                    matchNumber++;
                                }
                            }

                            if (matchNumber > highestMatchNumber)
                            {
                                highestMatchNumber = matchNumber;
                                bestMatch = name;
                            }
                        }
                    }
                }

                return highestMatchNumber > 0;
            }

            return false;
        }

        public static bool IsSoftAddressAndSoftCityHardZipMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 address.Zip != null &&
                 !string.IsNullOrEmpty(address.City))
            {
                List<string> softMatchedCities = BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();
                List<string> softMatchedStreets = BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

                StreetName bestMatch = null;
                int highestMatchNumber = 0;
                foreach (StreetName name in data.StreetData)
                {
                    if (!string.IsNullOrEmpty(name.Name) && name.ZipCodes.Contains(address.Zip.Value))
                    {
                        foreach (string softMatchedCity in softMatchedCities)
                        {
                            if (name.Cities.Contains(softMatchedCity))
                            {
                                foreach (string softMatchedStreet in softMatchedStreets)
                                {
                                    if (softMatchedStreet == name.Name)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsSolidAddressMatchWithNoZipCityAvailable(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                string.IsNullOrEmpty(address.City) &&
                !address.Zip.HasValue)
            {
                string streetToMatch = string.Join(" ", address.StreetName, address.Suffix);
                return data.StreetData.Any(n => n.FullStreetName == streetToMatch);
            }
            else
            {
                return false;
            }
        }

        internal static bool IsSolidAddressMatchOnly(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName))
            {
                string streetToMatch = string.Join(" ", address.StreetName, address.Suffix);
                return data.StreetData.Any(n => n.FullStreetName == streetToMatch);
            }
            else
            {
                return false;
            }
        }

        public static bool IsSoftAddressAndHardZipMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 address.Zip.HasValue)
            {
                StreetName[] streetsWithThisZip = data.StreetData.Where(n => n.ZipCodes.Contains(address.Zip.Value)).ToArray();

                double lowestEditDistance = double.MaxValue;
                StreetName bestMatch = null;
                foreach (StreetName streetWithThisZip in streetsWithThisZip)
                {
                    double editDistance = EditDistanceEngine.ComputeNormalized(streetWithThisZip.Name, address.StreetName);
                    if (editDistance < lowestEditDistance)
                    {
                        lowestEditDistance = editDistance;
                        bestMatch = streetWithThisZip;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsSoftAddressAndSoftCityMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 !string.IsNullOrEmpty(address.City))
            {
                List<string> softMatchedCities = BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();
                List<string> softMatchedStreets = BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

                foreach (StreetName name in data.StreetData)
                {
                    if (!string.IsNullOrEmpty(name.Name))
                    {
                        foreach (string softMatchedCity in softMatchedCities)
                        {
                            if (name.Cities.Contains(softMatchedCity))
                            {
                                foreach (string softMatchedStreet in softMatchedStreets)
                                {
                                    if (softMatchedStreet == name.Name)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsSoftAddressMatch(Address address, Data data)
        {
            if (!string.IsNullOrEmpty(address.StreetName))
            {
                List<string> softMatchedStreets = BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

                StreetName[] possibleStreets = data.StreetData.Where(n => softMatchedStreets.Contains(n.Name)).ToArray();

                if (softMatchedStreets.Count > 0 &&
                    EditDistanceEngine.ComputeNormalized(softMatchedStreets[0], address.StreetName) < .5f)
                {
                    foreach (StreetName name in possibleStreets)
                    {
                        if (!string.IsNullOrEmpty(name.Name))
                        {
                            foreach (string softMatchedStreet in softMatchedStreets)
                            {
                                if (softMatchedStreet == name.Name)
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
        //public static bool IsLevel3Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName) &&
        //        (!string.IsNullOrEmpty(address.City) || address.Zip.HasValue))
        //    {
        //        string nameForDictionary = PrepNameForDictionaryLookup(address.StreetName);

        //        if (!string.IsNullOrEmpty(address.City))
        //        {
        //            if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
        //            {
        //                if (data.StreetNameDictionary[nameForDictionary].Any(n => n.Name == address.StreetName &&
        //                     n.Cities.Contains(address.City)))
        //                {
        //                    return true;
        //                }
        //            }
        //        }

        //        if (address.Zip.HasValue)
        //        {
        //            if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
        //            {
        //                if (data.StreetNameDictionary[nameForDictionary].Any(n => n.ZipCodes.Contains(address.Zip.Value) &&
        //                     n.Name == address.StreetName))
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //public static bool IsLevel4Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName) &&
        //        !string.IsNullOrEmpty(address.City))
        //    {
        //        List<string> alternateCities =
        //            BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();

        //        if (alternateCities.Count > 1 || (alternateCities.Count == 1 && address.City != alternateCities[0]))
        //        {
        //            string nameForDictionary = PrepNameForDictionaryLookup(address.StreetName);

        //            foreach (string alternateCity in alternateCities)
        //            {
        //                if (data.StreetNameDictionary.ContainsKey(nameForDictionary))
        //                {
        //                    if (data.StreetNameDictionary[nameForDictionary].Any(n => n.Name == address.StreetName &&
        //                         n.Cities.Contains(alternateCity)))
        //                    {
        //                        return true;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //public static bool IsLevel5Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName))
        //    {
        //        foreach (StreetName streetName in data.StreetData)
        //        {
        //            if (streetName.FullStreetName == address.StreetName + " " + address.Suffix)
        //            {
        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}

        //public static bool IsLevel6Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName))
        //    {
        //        foreach (StreetName streetName in data.StreetData)
        //        {
        //            if (streetName.Name == address.StreetName)
        //            {
        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}

        //public static bool IsLevel7Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName) &&
        //        address.Zip.HasValue)
        //    {
        //        List<string> alternateStreets =
        //            BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

        //        if (alternateStreets.Count > 0)
        //        {
        //            foreach (string alternateStreet in alternateStreets)
        //            {
        //                List<StreetName> streetNames = data.StreetNameDictionary[alternateStreet];
        //                foreach (StreetName streetName in streetNames)
        //                {
        //                    if (streetName.Cities.Contains(address.City) ||
        //                        streetName.ZipCodes.Contains(address.Zip.Value))
        //                    {
        //                        return true;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //public static bool IsLevel8Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName) &&
        //        !string.IsNullOrEmpty(address.City))
        //    {
        //        List<string> alternateCities =
        //            BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();

        //        List<string> alternateStreets =
        //            BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

        //        if (alternateStreets.Count > 0 && alternateCities.Count > 0)
        //        {
        //            foreach (string alternateStreet in alternateStreets)
        //            {
        //                List<StreetName> streetNames = data.StreetNameDictionary[alternateStreet];
        //                foreach (StreetName streetName in streetNames)
        //                {
        //                    foreach (string alternateCity in alternateCities)
        //                    {
        //                        if (streetName.Cities.Contains(alternateCity))
        //                        {
        //                            return true;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //public static bool IsLevel9Match(Address address, Data data)
        //{
        //    if (!string.IsNullOrEmpty(address.StreetName))
        //    {
        //        double[] editDistances =
        //            BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().Select(
        //                n => EditDistanceEngine.ComputeNormalized(n, address.StreetName)).ToArray();

        //        return editDistances.Length > 0 && editDistances.Any(n => n < .4f);
        //    }

        //    return false;
        //}
    }
}
