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
        public static bool IsRearrangedAddressAndCityZipMatch(Address address, Data data, List<string> alternateLines)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 address.Zip != null &&
                 !string.IsNullOrEmpty(address.City))
            {
                StreetName[] possibleStreets = data.StreetData.Where(n =>
                        n.Name == address.StreetName &&
                        n.Cities.Contains(address.City) &&
                        n.ZipCodes.Contains(address.Zip.Value)).ToArray();

                StreetName bestMatch = null;
                int highestMatchNumber = 0;
                foreach (StreetName name in possibleStreets)
                {
                    // demand the stem and city/zip are matches
                    //if (!string.IsNullOrEmpty(name.Name) && name.Cities.Contains(address.City) &&
                    //    name.ZipCodes.Contains(address.Zip.Value) &&
                    //    StringUtility.Contains(address.FullStreetName, " " + name.Name + " "))
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

                        int matchNumber = 0;

                        if (!string.IsNullOrEmpty(name.PreDirection))
                        {
                            if (name.PreDirection == "E" &&
                                 address.CardinalDirection == "EAST")
                            {
                                matchNumber++;
                            }
                            else if (name.PreDirection == "W" &&
                                 address.CardinalDirection == "WEST")
                            {
                                matchNumber++;
                            }
                            else if (name.PreDirection == "S" &&
                                 address.CardinalDirection == "SOUTH")
                            {
                                matchNumber++;
                            }
                            else if (name.PreDirection == "N" &&
                                 address.CardinalDirection == "NORTH")
                            {
                                matchNumber++;
                            }
                        }

                        string fullName = string.Join(" ", address.StreetNumber, address.StreetName, address.Suffix);

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

                if (highestMatchNumber > 0)
                {
                    // street name correction, zip/city okay. 
                    string newLine = AddressUtility.CreateLineFromAddress(address, bestMatch.FullStreetName, address.Zip.Value, address.City);
                    lock (alternateLines)
                    {
                        alternateLines.Add(newLine);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Maybe portions of the address have been rearranged. The west is at the end, or the avenue is placed
        /// before an apartment number or something wonky. But otherwise most of the parts are there. 
        /// </summary>
        /// <returns></returns>
        public static bool IsRearrangedAddressAndCityOrZipMatch(Address address, Data data, List<string> alternateLines)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 (address.Zip != null ||
                 !string.IsNullOrEmpty(address.City)))
            {
                StreetName bestMatch = null;
                int highestMatchNumber = 0;

                StreetName[] possibleStreets = data.StreetData.Where(n =>
                        n.Name == address.StreetName &&
                        (n.Cities.Contains(address.City) ||
                        (address.Zip != null && n.ZipCodes.Contains(address.Zip.Value)))).ToArray();

                foreach (StreetName name in possibleStreets)
                {
                    // demand the stem and city/zip are matches
                    //if (!string.IsNullOrEmpty(name.Name) && (name.Cities.Contains(address.City) ||
                    //    (address.Zip.HasValue && name.ZipCodes.Contains(address.Zip.Value)) &&
                    //    StringUtility.Contains(address.FullStreetName, name.Name)))
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

                        int matchNumber = 0;

                        if (!string.IsNullOrEmpty(name.PreDirection))
                        {
                            if (name.PreDirection == "E" &&
                                 address.CardinalDirection == "EAST")
                            {
                                matchNumber++;
                            }
                            else if (name.PreDirection == "W" &&
                                 address.CardinalDirection == "WEST")
                            {
                                matchNumber++;
                            }
                            else if (name.PreDirection == "S" &&
                                 address.CardinalDirection == "SOUTH")
                            {
                                matchNumber++;
                            }
                            else if (name.PreDirection == "N" &&
                                 address.CardinalDirection == "NORTH")
                            {
                                matchNumber++;
                            }
                        }

                        string fullName = string.Join(" ", address.StreetNumber, address.StreetName, address.Suffix);

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

                if (highestMatchNumber > 0)
                {
                    // either or on zip/city. 
                    // which one did we settle on? 

                    if (!string.IsNullOrEmpty(address.City) &&
                        bestMatch.Cities.Contains(address.City))
                    {
                        // city
                        // given the street + city, what are the available zips? 
                        StreetNameAndCity key1 = new StreetNameAndCity
                        {
                            City = address.City,
                            FullStreetName = bestMatch.FullStreetName
                        };

                        int[] availableZips = data.StreetNameCity2Zips[key1].ToArray();

                        lock (alternateLines)
                        {
                            foreach (int zip in availableZips)
                            {
                                alternateLines.Add(AddressUtility.CreateLineFromAddress(address, bestMatch.FullStreetName, zip, address.City));
                            }
                        }

                        return true;
                    }
                    else if (address.Zip != null &&
                        bestMatch.ZipCodes.Contains(address.Zip.Value))
                    {
                        // zip
                        // given the street + zip, what are the available cities? 
                        StreetNameAndZip key2 = new StreetNameAndZip
                        {
                            Zip = address.Zip.Value,
                            FullStreetName = bestMatch.FullStreetName
                        };
                        string[] availableCities = data.StreetNameZip2Cities[key2].ToArray();

                        lock (alternateLines)
                        {
                            foreach (string city in availableCities)
                            {
                                alternateLines.Add(AddressUtility.CreateLineFromAddress(address, bestMatch.FullStreetName, address.Zip.Value, city));
                            }
                        }


                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsRearrangedAddressAndSoftCityMatch(Address address, Data data, List<string> alternateLines)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 !string.IsNullOrEmpty(address.City))
            {
                List<string> softMatchedCities = BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();

                StreetName bestMatch = null;
                int highestMatchNumber = 0;
                string bestSoftMatchedCity = null;
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

                            int matchNumber = 0;

                            if (!string.IsNullOrEmpty(name.PreDirection))
                            {
                                if (name.PreDirection == "E" &&
                                     address.CardinalDirection == "EAST")
                                {
                                    matchNumber++;
                                }
                                else if (name.PreDirection == "W" &&
                                     address.CardinalDirection == "WEST")
                                {
                                    matchNumber++;
                                }
                                else if (name.PreDirection == "S" &&
                                     address.CardinalDirection == "SOUTH")
                                {
                                    matchNumber++;
                                }
                                else if (name.PreDirection == "N" &&
                                     address.CardinalDirection == "NORTH")
                                {
                                    matchNumber++;
                                }
                            }

                            string fullName = string.Join(" ", address.StreetNumber, address.StreetName, address.Suffix);

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
                                bestSoftMatchedCity = softMatchedCity;
                            }
                        }
                    }
                }

                if (highestMatchNumber > 0)
                {
                    // given the city + street, what are the available zips? 
                    int[] possibleZips = data.StreetNameCity2Zips[new StreetNameAndCity
                    {
                        City = bestSoftMatchedCity,
                        FullStreetName = bestMatch.FullStreetName
                    }].ToArray();

                    lock (alternateLines)
                    {
                        foreach (int possibleZip in possibleZips)
                        {
                            alternateLines.Add(AddressUtility.CreateLineFromAddress(address, bestMatch.FullStreetName, possibleZip, bestSoftMatchedCity));
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool IsSoftAddressAndSoftCityHardZipMatch(Address address, Data data, List<string> alternateLines)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                 address.Zip != null &&
                 !string.IsNullOrEmpty(address.City))
            {
                List<string> innerAlternateLines = new List<string>();

                List<string> softMatchedCities = BKTreeEngine.LeastEditDistance(address.City, data.CityNameBKTree).Distinct().ToList();
                List<string> softMatchedStreets = BKTreeEngine.LeastEditDistance(address.StreetName, data.StreetNameBKTree).Distinct().ToList();

                foreach (StreetName name in data.StreetData)
                {
                    if (!string.IsNullOrEmpty(name.Name) && name.ZipCodes.Contains(address.Zip.Value))
                    {
                        foreach (string softMatchedCity in softMatchedCities)
                        {
                            if (data.StreetNameZip2Cities[new StreetNameAndZip
                            {
                                Zip = address.Zip.Value,
                                FullStreetName = name.FullStreetName,
                            }].Contains(softMatchedCity))
                            {
                                if (name.Cities.Contains(softMatchedCity))
                                {
                                    foreach (string softMatchedStreet in softMatchedStreets)
                                    {
                                        if (softMatchedStreet == name.Name)
                                        {
                                            innerAlternateLines.Add(AddressUtility.CreateLineFromAddress(address, name.FullStreetName, address.Zip, softMatchedCity));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                lock(alternateLines)
                {
                    alternateLines.AddRange(innerAlternateLines);
                }

                return innerAlternateLines.Count > 0;
            }

            return false;
        }

        internal static bool IsSolidAddressMatchWithNoZipCityAvailable(Address address, Data data, List<string> alternateLines)
        {
            if (!string.IsNullOrEmpty(address.StreetName) &&
                string.IsNullOrEmpty(address.City) &&
                !address.Zip.HasValue)
            {
                string streetToMatch = string.Join(" ", address.StreetName, address.Suffix);
                StreetName[] matches = data.StreetData.Where(n => n.FullStreetName == streetToMatch).ToArray();

                foreach (StreetName match in matches)
                {
                    string[] allPossibleCities = match.Cities.ToArray();

                    foreach (string possibleCity in allPossibleCities)
                    {
                        int[] possibleZipsForThisCityAndStreet = data.StreetNameCity2Zips[new StreetNameAndCity
                        {
                            City = possibleCity,
                            FullStreetName = match.FullStreetName,
                        }].ToArray();
                        lock (alternateLines)
                        {
                            foreach (int possibleZip in possibleZipsForThisCityAndStreet)
                            {
                                alternateLines.AddRange(matches.Select(n => AddressUtility.CreateLineFromAddress(address, n.FullStreetName, possibleZip, possibleCity)));
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool IsSolidStreetMatchOnly(Address address, Data data, List<string> alternateLines)
        {
            if (!string.IsNullOrEmpty(address.StreetName))
            {
                List<string> innerAlternateLines = new List<string>();
                string streetToMatch = string.Join(" ", address.StreetName, address.Suffix);
                StreetName[] streetNamesThatMatch = data.StreetData.Where(n => n.FullStreetName == streetToMatch).ToArray();

                foreach (StreetName streetNameThatMatches in streetNamesThatMatch)
                {
                    string[] citiesForThisStreetName = streetNameThatMatches.Cities.ToArray();

                    foreach (string city in citiesForThisStreetName)
                    {
                        int[] zips = data.StreetNameCity2Zips[new StreetNameAndCity
                        {
                            City = city,
                            FullStreetName = streetNameThatMatches.FullStreetName,
                        }].ToArray();

                        foreach (int zip in zips)
                        {
                            innerAlternateLines.Add(AddressUtility.CreateLineFromAddress(address, streetNameThatMatches.FullStreetName, zip, city));
                        }
                    }
                }

                lock(alternateLines)
                {
                    alternateLines.AddRange(innerAlternateLines);
                }

                return innerAlternateLines.Count > 0;
            }
            else
            {
                return false;
            }
        }

        public static bool IsSoftAddressAndHardZipMatch(Address address, Data data, List<string> alternateLines)
        {
            List<string> innerAlternateLines = new List<string>();

            if (!string.IsNullOrEmpty(address.StreetName) &&
                 address.Zip.HasValue)
            {
                StreetName[] streetsWithThisZip = data.StreetData.Where(n => n.ZipCodes.Contains(address.Zip.Value)).ToArray();

                double lowestEditDistance = double.MaxValue;
                StreetName bestMatch = null;
                foreach (StreetName streetWithThisZip in streetsWithThisZip)
                {
                    string[] citiesWithThisStreetAndZip = data.StreetNameZip2Cities[new StreetNameAndZip
                    {
                        FullStreetName = streetWithThisZip.FullStreetName,
                        Zip = address.Zip.Value
                    }].ToArray();

                    double editDistance = EditDistanceEngine.ComputeNormalized(streetWithThisZip.Name, address.StreetName);
                    if (editDistance < lowestEditDistance)
                    {
                        innerAlternateLines.Clear();

                        lowestEditDistance = editDistance;
                        bestMatch = streetWithThisZip;

                        foreach (string city in citiesWithThisStreetAndZip)
                        {
                            innerAlternateLines.Add(AddressUtility.CreateLineFromAddress(address, streetWithThisZip.FullStreetName, address.Zip.Value, city));
                        }
                    }
                    else if (editDistance == lowestEditDistance)
                    {
                        foreach (string city in citiesWithThisStreetAndZip)
                        {
                            innerAlternateLines.Add(AddressUtility.CreateLineFromAddress(address, streetWithThisZip.FullStreetName, address.Zip.Value, city));
                        }
                    }

                }
            }

            lock(alternateLines)
            {
                alternateLines.AddRange(innerAlternateLines);
            }

            return innerAlternateLines.Count > 0;
        }

        public static bool IsSoftAddressAndSoftCityMatch(Address address, Data data, List<string> alternateLines)
        {
            List<string> innerAlternateLines = new List<string>();

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
                                        int[] zipsForCityAndStreet = data.StreetNameCity2Zips[new StreetNameAndCity
                                        {
                                            City = softMatchedCity,
                                            FullStreetName = name.FullStreetName,
                                        }].ToArray();

                                        foreach (int zip in zipsForCityAndStreet)
                                        {
                                            innerAlternateLines.Add(AddressUtility.CreateLineFromAddress(address, name.FullStreetName, zip, softMatchedCity));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            lock(alternateLines)
            {
                alternateLines.AddRange(innerAlternateLines);
            }

            return innerAlternateLines.Count > 0;
        }

        public static bool IsSoftAddressMatch(Address address, Data data, List<string> alternateLines)
        {
            List<string> innerAlternateLines = new List<string>();
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
                                    string[] possibleCities = name.Cities.ToArray();
                                    foreach (string city in possibleCities)
                                    {
                                        int[] possibleZips = data.StreetNameCity2Zips[
                                            new StreetNameAndCity
                                            {
                                                City = city,
                                                FullStreetName = name.FullStreetName,
                                            }].ToArray();

                                        foreach (int possibleZip in possibleZips)
                                        {
                                            innerAlternateLines.Add(AddressUtility.CreateLineFromAddress(address, name.FullStreetName, possibleZip, city));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                lock(alternateLines)
                {
                    alternateLines.AddRange(innerAlternateLines);
                }
                return innerAlternateLines.Count > 0;
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
