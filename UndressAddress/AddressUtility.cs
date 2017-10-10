using DecisionTreeLearner.NLP;
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
    public static class AddressUtility
    {
        private static MatchQuality IsHomelessOrUnknown(string inputAddress1, Data data)
        {
            if (inputAddress1 != "UNKN3146 86TH STREE")
            {
                foreach (string unknownAddress in data.UnknownAddresses)
                {
                    if (inputAddress1.StartsWith(unknownAddress) || inputAddress1.StartsWith("PT " + unknownAddress))
                    {
                        return MatchQuality.Unknown;
                    }
                }

                if (inputAddress1 == "U" || inputAddress1 == "UN" || Regex.IsMatch(inputAddress1, "^[A-Z]00000") ||
                    inputAddress1 == "0" || inputAddress1 == "UTO" || inputAddress1 == "NCB" || inputAddress1.Contains("ADDRESS") || inputAddress1 == "MD" || inputAddress1.EndsWith("ADRS") ||
                    inputAddress1.Contains("ANSWER") || inputAddress1.Contains("UNABLE") ||
                    inputAddress1.Contains("XX") || inputAddress1 == "NA" || inputAddress1 == "UK" || inputAddress1 == "UINK")
                {
                    return MatchQuality.Unknown;
                }
                else if (Regex.IsMatch(inputAddress1, @"^[0-9]{4,10}$"))
                {
                    return MatchQuality.Unknown;
                }

                // match repeats of the same character 5 or more times. 
                if (Regex.IsMatch(inputAddress1, @"(.)\1{5,}") ||
                    Regex.IsMatch(inputAddress1, @"(^| )NOT ") ||
                    Regex.IsMatch(inputAddress1, @"DFS") || ////DFSFSDF
                    Regex.IsMatch(inputAddress1, @"(\d+){6,20}") ||//13846 ST. Numbers way too high for street address
                    Regex.IsMatch(inputAddress1, @"^([A-Z]){1,2}$") ||// only 1-2 characters
                    Regex.IsMatch(inputAddress1, @"^U ?T ?O$"))
                {
                    return MatchQuality.Unknown;
                }

                foreach (string homelessAddress in data.HomelessAddresses)
                {
                    if (inputAddress1.StartsWith(homelessAddress) || inputAddress1.StartsWith("PT " + homelessAddress))
                    {
                        return MatchQuality.Homeless;
                    }
                }
                if (inputAddress1.Contains("UNDOM"))
                {
                    return MatchQuality.Homeless;
                }
            }

            return MatchQuality.MatchNotYetDetermined;
        }

        public static Address HandleParticularlyProblematicStreets(Address address)
        {
            string streetName = address.StreetName;

            if (!string.IsNullOrEmpty(streetName))
            {
                // grand concourse has been a bitch
                Match weirdStreetMatch = Regex.Match(streetName, "(GRAND|GR) ?CO[A-Z ]+");
                if (weirdStreetMatch.Success)
                {
                    string toReplace = weirdStreetMatch.Value;
                    streetName = streetName.Replace(toReplace, "GRAND CONCOURSE");

                    // some people put AVENUE at the end
                    address.Suffix = "";
                }

                // BROADWAY
                weirdStreetMatch = Regex.Match(streetName, @"([A-Z]+)? ?(B[A-Z]+DWAY|BWAY|BRWAY|BWY)$");
                if (weirdStreetMatch.Success)
                {
                    if (weirdStreetMatch.Groups[1].Value != "")
                    {
                        streetName = $"{weirdStreetMatch.Groups[1].Value} BROADWAY";
                    }
                    else
                    {
                        streetName = "BROADWAY";
                    }
                }
                address.StreetName = streetName;
            }

            return address;
        }

        public static void CheckForBuildingsAndCenters(ref Address ret, Data data)
        {
            foreach (KeyValuePair<string, Address> pair in data.KnownCenters)
            {
                Match buildingMatch = Regex.Match(ret.RawAddress1, @"" + pair.Key);
                if (buildingMatch.Success)
                {
                    ret.CenterName = pair.Value.CenterName;
                    ret.StreetNumber = pair.Value.StreetNumber;
                    ret.StreetName = pair.Value.StreetName;
                    ret.City = pair.Value.City;
                    ret.State = pair.Value.State;
                    ret.FullStreetName = pair.Value.FullStreetName;
                    ret.MatchQuality = MatchQuality.LeaveAlone;
                }
            }
        }

        public static Address NormalizeSuffix(string input, Data data)
        {
            string[] inputAddress1Bits = input.Split(' ');
            string possibleSuffix = inputAddress1Bits[inputAddress1Bits.Length - 1];
            string suffixToCheck = possibleSuffix;
            string confirmedSuffix = null;

            // is this a known bad suffix? 
            if (data.SuffixReplacementKey.ContainsKey(possibleSuffix))
            {
                // yes, replace
                suffixToCheck = data.SuffixReplacementKey[possibleSuffix];
            }

            for (int c = 0; c < data.Suffixes.LongSuffixes.Length; c++)
            {
                string longSuffix = data.Suffixes.LongSuffixes[c];
                if (suffixToCheck == longSuffix)
                {
                    confirmedSuffix = data.Suffixes.ShortSuffixes[c];
                    break;
                }
            }

            if (confirmedSuffix == null)
            {
                for (int c = 0; c < data.Suffixes.ShortSuffixes.Length; c++)
                {
                    string shortSuffix = data.Suffixes.ShortSuffixes[c];

                    if (suffixToCheck == shortSuffix)
                    {
                        confirmedSuffix = data.Suffixes.ShortSuffixes[c];
                        break;
                    }
                }
            }


            string streetName = input;
            if (confirmedSuffix != null && input.Length > 0)
            {
                // identify index of suffix to remove. 
                int toRemove = input.LastIndexOf(possibleSuffix);
                if (toRemove == 0)
                {
                    // if the entire name IS the suffix, then remove it, we have crap data. 
                    streetName = "";
                }
                else
                {
                    // remove the suffix. 
                    streetName = input.Substring(0, toRemove - 1);
                }
            }

            string correctedAddress = streetName;
            if (!string.IsNullOrEmpty(confirmedSuffix))
            {
                correctedAddress += " " + confirmedSuffix;
            }
            return new Address
            {
                RawAddress1 = input,
                Suffix = confirmedSuffix,
                StreetName = streetName,
                FullStreetName = correctedAddress,
            };
        }

        public static string CleanSpacesAndPunctuation(string inputAddress1)
        {
            // strip extra spaces betwen numbers at the beginning
            inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (\d+) (\d+)", "$1$2 $3");

            // remove periods and other punctuation
            inputAddress1 = inputAddress1.Replace(".", " ");
            inputAddress1 = inputAddress1.Replace("]", " ");
            inputAddress1 = inputAddress1.Replace("`", " ");
            inputAddress1 = inputAddress1.Replace("'", " ");
            inputAddress1 = inputAddress1.Replace(":", " ");
            inputAddress1 = inputAddress1.Replace("~", " ");

            inputAddress1 = inputAddress1.Replace("/", " ");
            inputAddress1 = inputAddress1.Replace("\\", " ");
            inputAddress1 = inputAddress1.Replace("-", " ");

            inputAddress1 = inputAddress1.Trim();




            // replace multiple spaces with one.
            inputAddress1 = Regex.Replace(inputAddress1, " + ", " ");
            return inputAddress1;
        }

        public static string CleanAddressFormat(string inputAddress1, Dictionary<string, string> abbreviations)
        {
            // Put spaces between numbers and letters
            inputAddress1 = Regex.Replace(inputAddress1, @" (\d+)(TH)([A-Z])", " $1 $2 $3");
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+)([A-Z]+)", "$1 $2");
            inputAddress1 = Regex.Replace(inputAddress1, @"([A-Z]+)(\d+)", "$1 $2");

            // remove the suffix at the end of 1st or 2nd or 3rd, etc. 
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (RST|ND|RD|TH|ERD) ", "$1 ");
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (RST|ND|TH)$", "$1");
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (ST) (ST|AVE)", "$1 $3");
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (ST|ND|RD|TH)(AVE?)", "$1 AVENUE");
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (ST|ND|RD|TH)(ST)", "$1 STREET");

            if (inputAddress1 == "UNKN3146 86TH STREE")
            {
                inputAddress1 = "3146 86 ST";
            }

            // Ex: 360 E 193
            inputAddress1 = Regex.Replace(inputAddress1, @"^(.+) (WEST|EAST|SOUTH|NORTH)$", "$2 $1");
            inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (WEST|NORTH|EAST|SOUTH) (\d+)$", "$1 $2 $3 STREET");
            inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (WEST|NORTH|EAST|SOUTH) (\d+)$", "$1 $2 $3 STREET");
            inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (\d+)$", "$1 $2 STREET");
            inputAddress1 = Regex.Replace(inputAddress1, " ([A-Z]+)(DR)$", " $1 DRIVE");

            if (Regex.Match(inputAddress1, @"( |^)" + "ST " + "( |$)").Success)
            {
                Console.WriteLine(inputAddress1);
            }

            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) S ", "$1 SOUTH");

            // generic abbreviations. 
            foreach (KeyValuePair<string, string> pair in abbreviations)
            {
                inputAddress1 = Regex.Replace(inputAddress1, @"( |^)" + pair.Key + "( |$)", "$1" + pair.Value + "$2");
            }
            return inputAddress1;
        }

        public static Address InitializeAddressBen(string input, Data data)
        {
            bool print = false;

            Address ret = new Address();

            ret.MatchQuality = MatchQuality.MatchNotYetDetermined;

            const int Address1Column = 8;
            const int Address2Column = 9;
            const int ZipColumn = 10;
            const int CityColumn = 13;

            ret.OriginalLine = input;
            string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(input);
            ret.OriginalLineBits = bits;
            ret.EnterpriseId = 0;

            int enterpriseId = 0;
            if (int.TryParse(bits[0], out enterpriseId))
            {
                ret.EnterpriseId = enterpriseId;
            }

            // RawAddress1
            string inputAddress1 = bits[Address1Column];
            ret.RawAddress1 = inputAddress1;
            string inputAddress2 = bits[Address2Column];
            ret.RawAddress2 = inputAddress2;

            // Zip/City
            int possibleZip = 0;
            if (int.TryParse(bits[ZipColumn], out possibleZip))
            {
                ret.Zip = possibleZip;
            }
            ret.City = bits[CityColumn];


            inputAddress1 = CleanSpacesAndPunctuation(inputAddress1);

            if (inputAddress1 == "")
            {
                ret.MatchQuality = MatchQuality.Unknown;
                ret.FullStreetName = "";
            }
            else
            {
                CheckForBuildingsAndCenters(ref ret, data);

                if (ret.MatchQuality != MatchQuality.LeaveAlone)
                {
                    ret.MatchQuality = IsHomelessOrUnknown(inputAddress1, data);
                    if (ret.MatchQuality == MatchQuality.Homeless)
                    {
                        ret.FullStreetName = "HOMELESS";
                    }
                    else if (ret.MatchQuality == MatchQuality.Unknown)
                    {
                        ret.FullStreetName = "UNKNOWN";
                    }

                }

                if (ret.MatchQuality == MatchQuality.MatchNotYetDetermined)
                {
                    inputAddress1 = CleanAddressFormat(inputAddress1, data.Abbreviations);


                    #region ApartmentNumber
                    int startIndex = -1, length = -1;

                    Match apartmentNumberMatch = Regex.Match(inputAddress1, @" (UNIT|STE|SUITE|APT|APARTMENT) ([0-9]+ ?[A-Z]*|[A-Z]+ ?[0-9]*)");

                    if (apartmentNumberMatch.Success)
                    {
                        startIndex = apartmentNumberMatch.Index;
                        length = apartmentNumberMatch.Length;

                        ret.ApartmentNumber = apartmentNumberMatch.Groups[2].Value;  // this is odd, always +1 the Number of matches. Groups[0] is the original string.
                        ret.ApartmentNumber = Regex.Replace(ret.ApartmentNumber, " ", "");
                        startIndex = apartmentNumberMatch.Groups[1].Index;
                        length = apartmentNumberMatch.Groups[1].Length + 1 + apartmentNumberMatch.Groups[2].Length;
                    }
                    else
                    {
                        // motivation: 202 E91ST ST B16
                        apartmentNumberMatch = Regex.Match(inputAddress1, @"^(\d+) ([A-Z0-9]+) ([A-Z 0-9]+) ([0-9]+|[A-Z] [0-9]{1,2}|[0-9]{1,2} [A-Z])$");
                        if (apartmentNumberMatch.Success)
                        {
                            Group matchedGroup = apartmentNumberMatch.Groups[apartmentNumberMatch.Groups.Count - 1];
                            startIndex = matchedGroup.Index;
                            length = matchedGroup.Length;

                            ret.ApartmentNumber = Regex.Replace(matchedGroup.Value, " ", "");
                        }
                        else
                        {
                            apartmentNumberMatch = Regex.Match(inputAddress1, "BSMT|BASEMENT");
                            if (apartmentNumberMatch.Success)
                            {
                                Group matchedGroup = apartmentNumberMatch.Groups[0];
                                startIndex = matchedGroup.Index;
                                length = matchedGroup.Length;

                                ret.ApartmentNumber = "BASEMENT";
                            }
                            else
                            {
                                apartmentNumberMatch = Regex.Match(inputAddress1, @" (ST)(\d{1,2}[A-Z]{1,2}|\d{1,2})$");
                                if (apartmentNumberMatch.Success)
                                {
                                    Group matchedGroup = apartmentNumberMatch.Groups[2];
                                    startIndex = matchedGroup.Index;
                                    length = matchedGroup.Length;

                                    ret.ApartmentNumber = matchedGroup.Value;
                                }
                                else
                                {
                                    apartmentNumberMatch = Regex.Match(inputAddress1, @" (\d+) ?FL?$");
                                    if (apartmentNumberMatch.Success)
                                    {
                                        startIndex = apartmentNumberMatch.Index;
                                        length = apartmentNumberMatch.Length;
                                        ret.ApartmentNumber = apartmentNumberMatch.Groups[1].Value + " FLOOR";
                                    }
                                    else
                                    {
                                        apartmentNumberMatch = Regex.Match(inputAddress1, @" (\d+)LV?L?$");
                                        if (apartmentNumberMatch.Success)
                                        {
                                            startIndex = apartmentNumberMatch.Index;
                                            length = apartmentNumberMatch.Length;
                                            ret.ApartmentNumber = apartmentNumberMatch.Groups[1].Value + " LEVEL";
                                        }
                                        else
                                        {
                                            apartmentNumberMatch = Regex.Match(inputAddress1, @"^(\d+) (\d+) (\d+) ([A-Z]+)$");
                                            if (apartmentNumberMatch.Success)
                                            {
                                                string wordPart = apartmentNumberMatch.Groups[4].Value;

                                                if (data.Suffixes.IndexOf(wordPart) != -1)
                                                {
                                                    ret.ApartmentNumber = apartmentNumberMatch.Groups[1].Value + "-" +
                                                        apartmentNumberMatch.Groups[2].Value;

                                                    startIndex = 0;
                                                    length = ret.ApartmentNumber.Length;
                                                }
                                            }
                                            else
                                            {
                                                apartmentNumberMatch = Regex.Match(inputAddress1, @"(\d+)-(\d+|[A-Z]+)");
                                                if (apartmentNumberMatch.Success)
                                                {
                                                    ret.ApartmentNumber = apartmentNumberMatch.Value;
                                                    startIndex = apartmentNumberMatch.Index;
                                                    length = apartmentNumberMatch.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    if (startIndex != -1)
                    {
                        inputAddress1 = inputAddress1.Remove(startIndex, length);
                        inputAddress1 = inputAddress1.Trim();
                    }


                    if (ret.ApartmentNumber == default(string) && inputAddress2.Length <= 5)
                    {
                        // Call these apartment numbers 
                        ret.ApartmentNumber = inputAddress2;
                        ret.ApartmentNumberFromAddress2Field = true;
                    }

                    #endregion

                    #region SuffixNormalization
                    Address suffixNormalized = NormalizeSuffix(inputAddress1, data);
                    ret.StreetName = suffixNormalized.StreetName;
                    ret.Suffix = suffixNormalized.Suffix;
                    inputAddress1 = ret.StreetName;
                    #endregion

                    #region StreetNumber
                    // is there a street number? 
                    Match match = Regex.Match(inputAddress1, @"^(\d+ ?[A-Z]?) .+");
                    if (match.Success)
                    {
                        ret.StreetNumber = match.Groups[1].Value;
                        inputAddress1 = inputAddress1.Remove(0, ret.StreetNumber.Length + 1);
                    }
                    #endregion

                    #region StreetName
                    ret.StreetName = inputAddress1;

                    // Problematic addresses
                    ret = HandleParticularlyProblematicStreets(ret);

                    // PO BOXes
                    match = Regex.Match(inputAddress1, @"(P ?O ?BOX|P ?O ?B?) ?(\d+)");
                    if (match.Success)
                    {
                        ret.POBoxNumber = int.Parse(match.Groups[2].Value);
                        ret.StreetName = "PO BOX " + match.Groups[2].Value;
                    }
                    else
                    {
                        match = Regex.Match(inputAddress1, @"BOX (\d+)$");
                        if (match.Success)
                        {
                            ret.POBoxNumber = int.Parse(match.Groups[1].Value);
                            ret.StreetName = "PO BOX " + match.Groups[1].Value;
                        }
                    }

                    if (ret.StreetName != null)
                    {
                        Match streetNameIsNumberMatch = Regex.Match(ret.StreetName, @"[A-Z]* ?(\d+)$");
                        if (streetNameIsNumberMatch.Success)
                        {
                            ret.StreetNameIsNumber = true;
                            ret.StreetNameNumber = int.Parse(streetNameIsNumberMatch.Groups[1].Value);
                        }

                        Match eastMatch = Regex.Match(ret.StreetName, "(^EAST | EAST$| EAST )");
                        if (eastMatch.Success)
                        {
                            ret.StreetName = ret.StreetName.Remove(eastMatch.Index, eastMatch.Length);
                            ret.CardinalDirection = "EAST";
                        }

                        Match westMatch = Regex.Match(ret.StreetName, "(^WEST | WEST$| WEST )");
                        if (westMatch.Success)
                        {
                            ret.StreetName = ret.StreetName.Remove(westMatch.Index, westMatch.Length);
                            ret.CardinalDirection = "WEST";
                        }

                        Match northMatch = Regex.Match(ret.StreetName, "(^NORTH | NORTH$| NORTH )");
                        if (northMatch.Success)
                        {
                            ret.StreetName = ret.StreetName.Remove(northMatch.Index, northMatch.Length);
                            ret.CardinalDirection = "NORTH";
                        }

                        Match southMatch = Regex.Match(ret.StreetName, "(^SOUTH | SOUTH$| SOUTH )");
                        if (southMatch.Success)
                        {
                            ret.StreetName = ret.StreetName.Remove(southMatch.Index, southMatch.Length);
                            ret.CardinalDirection = "SOUTH";
                        }
                    }

                    #endregion

                    ret.FullStreetName = ret.StreetName;
                    if (!string.IsNullOrEmpty(ret.Suffix))
                    {
                        ret.FullStreetName += " " + ret.Suffix;
                    }

                    //if (ret.MatchQuality == MatchQuality.MatchNotYetDetermined && string.IsNullOrEmpty(ret.StreetName))
                    //{
                    //    ret.MatchQuality = MatchQuality.CouldNotParseFormat;
                    //}
                }
                if (print)
                {
                    string addressRaw = $"{ret.RawAddress1}";
                    if (bits[Address2Column].Length <= 5)
                    {
                        addressRaw += $" / {bits[Address2Column]}";
                    }
                    string addressCleaned = $"{ ret.StreetNumber } / { ret.StreetName} / { ret.Suffix}";
                    if (!string.IsNullOrEmpty(ret.ApartmentNumber))
                    {
                        addressCleaned += $" / {ret.ApartmentNumber}";
                    }
                    Console.WriteLine($"{addressRaw} => {addressCleaned}");
                }
            }
            return ret;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(input);
            //    Console.WriteLine();
            //    throw ex;
            //}
        }


        public static Address InitializeAddress(string input, Data data)
        {
            bool print = false;

            Address ret = new Address();

            ret.MatchQuality = MatchQuality.MatchNotYetDetermined;

            const int Address1Column = 8;
            const int Address2Column = 9;
            const int ZipColumn = 10;
            const int CityColumn = 13;

            ret.OriginalLine = input;
            string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(input);
            ret.OriginalLineBits = bits;
            ret.EnterpriseId = 0;

            int enterpriseId = 0;
            if (int.TryParse(bits[0], out enterpriseId))
            {
                ret.EnterpriseId = enterpriseId;
            }

            // RawAddress1
            string inputAddress1 = bits[Address1Column];
            ret.RawAddress1 = inputAddress1;
            string inputAddress2 = bits[Address2Column];
            ret.RawAddress2 = inputAddress2;

            // Zip/City
            int possibleZip = 0;
            if (int.TryParse(bits[ZipColumn], out possibleZip))
            {
                ret.Zip = possibleZip;
            }
            ret.City = bits[CityColumn];


            inputAddress1 = CleanSpacesAndPunctuation(inputAddress1);

            if (inputAddress1 == "")
            {
                ret.MatchQuality = MatchQuality.Unknown;
                ret.FullStreetName = "";
            }
            else
            {
                CheckForBuildingsAndCenters(ref ret, data);

                if (ret.MatchQuality != MatchQuality.LeaveAlone)
                {
                    ret.MatchQuality = IsHomelessOrUnknown(inputAddress1, data);
                    if (ret.MatchQuality == MatchQuality.Homeless)
                    {
                        ret.FullStreetName = "HOMELESS";
                    }
                    else if (ret.MatchQuality == MatchQuality.Unknown)
                    {
                        ret.FullStreetName = "UNKNOWN";
                    }

                }

                if (ret.MatchQuality == MatchQuality.MatchNotYetDetermined)
                {
                    inputAddress1 = CleanAddressFormat(inputAddress1, data.Abbreviations);


                    #region ApartmentNumber
                    int startIndex = -1, length = -1;

                    Match apartmentNumberMatch = Regex.Match(inputAddress1, @" (UNIT|STE|SUITE|APT|APARTMENT) ([0-9]+ ?[A-Z]*|[A-Z]+ ?[0-9]*)");

                    if (apartmentNumberMatch.Success)
                    {
                        startIndex = apartmentNumberMatch.Index;
                        length = apartmentNumberMatch.Length;

                        ret.ApartmentNumber = apartmentNumberMatch.Groups[2].Value;  // this is odd, always +1 the Number of matches. Groups[0] is the original string.
                        ret.ApartmentNumber = Regex.Replace(ret.ApartmentNumber, " ", "");
                        startIndex = apartmentNumberMatch.Groups[1].Index;
                        length = apartmentNumberMatch.Groups[1].Length + 1 + apartmentNumberMatch.Groups[2].Length;
                    }
                    else
                    {
                        // motivation: 202 E91ST ST B16
                        apartmentNumberMatch = Regex.Match(inputAddress1, @"^(\d+) ([A-Z0-9]+) ([A-Z 0-9]+) ([0-9]+|[A-Z] [0-9]{1,2}|[0-9]{1,2} [A-Z])$");
                        if (apartmentNumberMatch.Success)
                        {
                            Group matchedGroup = apartmentNumberMatch.Groups[apartmentNumberMatch.Groups.Count - 1];
                            startIndex = matchedGroup.Index;
                            length = matchedGroup.Length;

                            ret.ApartmentNumber = Regex.Replace(matchedGroup.Value, " ", "");
                        }
                        else
                        {
                            apartmentNumberMatch = Regex.Match(inputAddress1, "BSMT|BASEMENT");
                            if (apartmentNumberMatch.Success)
                            {
                                Group matchedGroup = apartmentNumberMatch.Groups[0];
                                startIndex = matchedGroup.Index;
                                length = matchedGroup.Length;

                                ret.ApartmentNumber = "BASEMENT";
                            }
                            else
                            {
                                apartmentNumberMatch = Regex.Match(inputAddress1, @" (ST)(\d{1,2}[A-Z]{1,2}|\d{1,2})$");
                                if (apartmentNumberMatch.Success)
                                {
                                    Group matchedGroup = apartmentNumberMatch.Groups[2];
                                    startIndex = matchedGroup.Index;
                                    length = matchedGroup.Length;

                                    ret.ApartmentNumber = matchedGroup.Value;
                                }
                                else
                                {
                                    apartmentNumberMatch = Regex.Match(inputAddress1, @" (\d+) ?FL?$");
                                    if (apartmentNumberMatch.Success)
                                    {
                                        startIndex = apartmentNumberMatch.Index;
                                        length = apartmentNumberMatch.Length;
                                        ret.ApartmentNumber = apartmentNumberMatch.Groups[1].Value + " FLOOR";
                                    }
                                    else
                                    {
                                        apartmentNumberMatch = Regex.Match(inputAddress1, @" (\d+)LV?L?$");
                                        if (apartmentNumberMatch.Success)
                                        {
                                            startIndex = apartmentNumberMatch.Index;
                                            length = apartmentNumberMatch.Length;
                                            ret.ApartmentNumber = apartmentNumberMatch.Groups[1].Value + " LEVEL";
                                        }
                                        else
                                        {
                                            apartmentNumberMatch = Regex.Match(inputAddress1, @"^(\d+) (\d+) (\d+) ([A-Z]+)$");
                                            if (apartmentNumberMatch.Success)
                                            {
                                                string wordPart = apartmentNumberMatch.Groups[4].Value;

                                                if (data.Suffixes.IndexOf(wordPart) != -1)
                                                {
                                                    ret.ApartmentNumber = apartmentNumberMatch.Groups[1].Value + "-" +
                                                        apartmentNumberMatch.Groups[2].Value;

                                                    startIndex = 0;
                                                    length = ret.ApartmentNumber.Length;
                                                }
                                            }
                                            else
                                            {
                                                apartmentNumberMatch = Regex.Match(inputAddress1, @"(\d+)-(\d+|[A-Z]+)");
                                                if (apartmentNumberMatch.Success)
                                                {
                                                    ret.ApartmentNumber = apartmentNumberMatch.Value;
                                                    startIndex = apartmentNumberMatch.Index;
                                                    length = apartmentNumberMatch.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    if (startIndex != -1)
                    {
                        inputAddress1 = inputAddress1.Remove(startIndex, length);
                        inputAddress1 = inputAddress1.Trim();
                    }


                    if (ret.ApartmentNumber == default(string) && inputAddress2.Length <= 5)
                    {
                        // Call these apartment numbers 
                        ret.ApartmentNumber = inputAddress2;
                        ret.ApartmentNumberFromAddress2Field = true;
                    }

                    #endregion

                    #region SuffixNormalization
                    Address suffixNormalized = NormalizeSuffix(inputAddress1, data);
                    ret.StreetName = suffixNormalized.StreetName;
                    ret.Suffix = suffixNormalized.Suffix;
                    inputAddress1 = ret.StreetName;
                    #endregion

                    #region StreetNumber
                    // is there a street number? 
                    Match match = Regex.Match(inputAddress1, @"^(\d+ ?[A-Z]?) .+");
                    if (match.Success)
                    {
                        ret.StreetNumber = match.Groups[1].Value;
                        inputAddress1 = inputAddress1.Remove(0, ret.StreetNumber.Length + 1);
                    }
                    #endregion

                    #region StreetName
                    ret.StreetName = inputAddress1;

                    // Problematic addresses
                    ret = HandleParticularlyProblematicStreets(ret);

                    // PO BOXes
                    match = Regex.Match(inputAddress1, @"(P ?O ?BOX|P ?O ?B?) ?(\d+)");
                    if (match.Success)
                    {
                        ret.POBoxNumber = int.Parse(match.Groups[2].Value);
                        ret.StreetName = "PO BOX " + match.Groups[2].Value;
                    }
                    else
                    {
                        match = Regex.Match(inputAddress1, @"BOX (\d+)$");
                        if (match.Success)
                        {
                            ret.POBoxNumber = int.Parse(match.Groups[1].Value);
                            ret.StreetName = "PO BOX " + match.Groups[1].Value;
                        }
                    }

                    if (ret.StreetName != null)
                    {
                        Match streetNameIsNumberMatch = Regex.Match(ret.StreetName, @"[A-Z]* ?(\d+)$");
                        if (streetNameIsNumberMatch.Success)
                        {
                            ret.StreetNameIsNumber = true;
                            ret.StreetNameNumber = int.Parse(streetNameIsNumberMatch.Groups[1].Value);
                        }
                    }

                    #endregion

                    ret.FullStreetName = ret.StreetName;
                    if (!string.IsNullOrEmpty(ret.Suffix))
                    {
                        ret.FullStreetName += " " + ret.Suffix;
                    }

                    //if (ret.MatchQuality == MatchQuality.MatchNotYetDetermined && string.IsNullOrEmpty(ret.StreetName))
                    //{
                    //    ret.MatchQuality = MatchQuality.CouldNotParseFormat;
                    //}
                }
                if (print)
                {
                    string addressRaw = $"{ret.RawAddress1}";
                    if (bits[Address2Column].Length <= 5)
                    {
                        addressRaw += $" / {bits[Address2Column]}";
                    }
                    string addressCleaned = $"{ ret.StreetNumber } / { ret.StreetName} / { ret.Suffix}";
                    if (!string.IsNullOrEmpty(ret.ApartmentNumber))
                    {
                        addressCleaned += $" / {ret.ApartmentNumber}";
                    }
                    Console.WriteLine($"{addressRaw} => {addressCleaned}");
                }
            }
            return ret;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(input);
            //    Console.WriteLine();
            //    throw ex;
            //}
        }

        public static string Address1FromStreetNameAndAddress(Address address, string streetName)
        {
            StringBuilder ret = new StringBuilder();

            if (!string.IsNullOrEmpty(address.StreetNumber))
            {
                ret.Append(address.StreetNumber + " ");
            }

            ret.Append(streetName + " ");

            if (!string.IsNullOrEmpty(address.ApartmentNumber) && !address.ApartmentNumberFromAddress2Field)
            {
                ret.Append(address.ApartmentNumber + " ");
            }

            return ret.ToString().Trim();
        }

        public static bool IsMatchingSuffix(string suffix1, string suffix2, AddressSuffixes suffixes)
        {
            bool theSame = false;

            if ((string.IsNullOrEmpty(suffix1) && string.IsNullOrEmpty(suffix2)) ||
                suffix1 == suffix2)
            {
                theSame = true;
            }
            else
            {
                for (int c = 0; c < suffixes.LongSuffixes.Length; c++)
                {
                    if ((suffixes.LongSuffixes[c] == suffix1 || suffixes.ShortSuffixes[c] == suffix1) &&
                        (suffixes.LongSuffixes[c] == suffix2 || suffixes.ShortSuffixes[c] == suffix2))
                    {
                        theSame = true;
                        break;
                    }
                }
            }

            return theSame;
        }

        public static string CreateLineFromAddress(Address address, string alternateStreet)
        {
            return CreateLineFromAddress(address, alternateStreet, null, null);
        }

        public static string CreateLineFromAddress(Address address, string alternateStreetName, int? alternateZip, string alternateCity)
        {
            string[] copiedParts = new string[address.OriginalLineBits.Length];
            Array.Copy(address.OriginalLineBits, copiedParts, copiedParts.Length);

            const int Address1Column = 8;
            const int ZipColumn = 10;
            const int CityColumn = 13;

            string modifiedAlternateStreetName = alternateStreetName;

            if (!string.IsNullOrEmpty(address.StreetNumber))
            {
                modifiedAlternateStreetName = address.StreetNumber + " " + modifiedAlternateStreetName;
            }

            if (!string.IsNullOrEmpty(address.ApartmentNumber) && !address.ApartmentNumberFromAddress2Field)
            {
                modifiedAlternateStreetName = modifiedAlternateStreetName + " APT " + address.ApartmentNumber;
            }

            copiedParts[Address1Column] = modifiedAlternateStreetName;
            if (alternateZip != null)
            {
                copiedParts[ZipColumn] = alternateZip.ToString();
            }
            if (alternateCity != null)
            {
                copiedParts[CityColumn] = alternateCity;
            }

            return string.Join(",", copiedParts);
        }
    }
}
