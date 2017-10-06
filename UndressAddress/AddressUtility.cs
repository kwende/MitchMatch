﻿using DecisionTreeLearner.NLP;
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
                    if (inputAddress1.StartsWith(unknownAddress))
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
                    Regex.IsMatch(inputAddress1, @"^([A-Z]){2}$") ||// only 2 characters
                    Regex.IsMatch(inputAddress1, @"^U ?T ?O$"))
                {
                    return MatchQuality.Unknown;
                }

                foreach (string homelessAddress in data.HomelessAddresses)
                {
                    if (inputAddress1.StartsWith(homelessAddress))
                    {
                        return MatchQuality.Homeless;
                    }
                }
                if (inputAddress1.Contains("UNDOM"))
                {
                    return MatchQuality.Homeless;
                }
            }

            return MatchQuality.NotMatched;
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

        public static Address CheckForBuildingsAndCenters(Address input, Data data)
        {
            Address ret = input;

            // this could be done in a simple .Contains() function, but I think the diversity of the 
            // way these buildings are spelled, etc. might require some regex for now. 
            // we can revisit later. 

            Match buildingMatch = Regex.Match(input.StreetName, @"^MABON ");
            if (buildingMatch.Success)
            {
                ret.StreetName = "MABON BUILDING 13 WARDS ISLAND";
                ret.City = "NEW YORK";
                ret.Zip = 10035;
                ret.MatchQuality = MatchQuality.FullAddressMatched;
            }

            buildingMatch = Regex.Match(input.StreetName, @"METRO ?TECH");
            if (buildingMatch.Success)
            {
                ret.StreetNumber = "9";
                ret.StreetName = "METROTECH CENTER";
                ret.City = "BROOKLYN";
                ret.Zip = 11201;
                ret.MatchQuality = MatchQuality.FullAddressMatched;
            }

            buildingMatch = Regex.Match(input.StreetName, @"KEENER");
            if (buildingMatch.Success)
            {
                ret.StreetNumber = "64";
                ret.StreetName = "SUNKEN GARDEN LOOP";
                ret.City = "NEW YORK";
                ret.Zip = 10035;
                ret.MatchQuality = MatchQuality.FullAddressMatched;
            }

            return ret;
        }

        public static Address InitializeAddress(string input, Data data)
        {
            //try
            //{
            bool print = false;

            Address ret = new Address();
            ret.MatchQuality = MatchQuality.NotMatched;

            const int Address1Column = 8;
            const int Address2Column = 9;
            const int ZipColumn = 10;
            const int CityColumn = 13;

            string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(input);


            // RawAddress1
            string inputAddress1 = bits[Address1Column];
            ret.RawAddress1 = inputAddress1;

            // Zip/City
            int possibleZip = 0;
            if (int.TryParse(bits[ZipColumn], out possibleZip))
            {
                ret.Zip = possibleZip;
            }
            ret.City = bits[CityColumn];


            // replace multiple spaces with one.
            inputAddress1 = Regex.Replace(inputAddress1, " + ", " ");

            // remove periods and other punctuation
            inputAddress1 = inputAddress1.Replace(".", "");
            inputAddress1 = inputAddress1.Replace("]", " ");
            inputAddress1 = inputAddress1.Replace("`", " ");
            inputAddress1 = inputAddress1.Replace("'", " ");
            inputAddress1 = inputAddress1.Replace(":", " ");
            inputAddress1 = inputAddress1.Replace("~", " ");
            inputAddress1 = inputAddress1.Trim();

            if (inputAddress1 == "")
            {
                ret.MatchQuality = MatchQuality.Unknown;
            }
            else
            {
                ret.MatchQuality = IsHomelessOrUnknown(inputAddress1, data);
                bool identifiedAsHomelessOrUnknown = ret.MatchQuality == MatchQuality.Homeless || ret.MatchQuality == MatchQuality.Unknown;


                if (!identifiedAsHomelessOrUnknown)
                {

                    #region GenericStringCleaning
                    // Put spaces between numbers and letters
                    inputAddress1 = Regex.Replace(inputAddress1, @" (\d+)(TH)([A-Z])", " $1 $2 $3");
                    inputAddress1 = Regex.Replace(inputAddress1, @"(\d+)([A-Z]+)", "$1 $2");
                    inputAddress1 = Regex.Replace(inputAddress1, @"([A-Z]+)(\d+)", "$1 $2");

                    // remove the suffix at the end of 1st or 2nd or 3rd, etc. 
                    inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (RST|ND|RD|TH|ERD) ", " $1 ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (RST|ND|TH)$", " $1");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (ST) (ST|AVE)", " $1 $3");
                    inputAddress1 = Regex.Replace(inputAddress1, @"THAV", "AVE");
                    inputAddress1 = Regex.Replace(inputAddress1, @"THST|RDST", "STREET");
                    inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) STAVE", "$1 AVENUE");


                    // Replace "FTJOHN" with "FORT JOHN"
                    inputAddress1 = Regex.Replace(inputAddress1, @" (FT)([A-Z]+) ", " FORT $2 ");
                    // Replace STNICHOLAS with STNICHOLAS

                    // Replace "1387 STJOHNS PL" with "1387 ST JOHNS PL"
                    inputAddress1 = Regex.Replace(inputAddress1, @" (ST)([BCDFGHJKLMNPQSTVWXZ])([A-Z]+) ", " $1 $2$3 ");

                    if (inputAddress1 == "UNKN3146 86TH STREE")
                    {
                        inputAddress1 = "3146 86 ST";
                    }

                    // replace N/S/E/W with the appropriate cardinal if put at end. 
                    // Ex: "31 W MOSHOLU PRKWY N"
                    inputAddress1 = Regex.Replace(inputAddress1, @" (N)$", " NORTH");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (S)$", " SOUTH");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (E)$", " EAST");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (W)$", " WEST");

                    inputAddress1 = Regex.Replace(inputAddress1, @"^(N) ", "NORTH ");
                    inputAddress1 = Regex.Replace(inputAddress1, @"^(S) ", "SOUTH ");
                    inputAddress1 = Regex.Replace(inputAddress1, @"^(E) ", "EAST ");
                    inputAddress1 = Regex.Replace(inputAddress1, @"^(W) ", "WEST ");

                    inputAddress1 = Regex.Replace(inputAddress1, @" (N) ", " NORTH ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (S) ", " SOUTH ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (E) ", " EAST ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (W) ", " WEST ");

                    inputAddress1 = Regex.Replace(inputAddress1, @"^(SO) ", "SOUTH ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (SO) ", " SOUTH ");

                    inputAddress1 = Regex.Replace(inputAddress1, @"^(NO) ", "NORTH ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (NO) ", " NORTH ");

                    inputAddress1 = Regex.Replace(inputAddress1, @"^(SW) ", "SOUTHWEST ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (SW) ", " SOUTHWEST ");

                    inputAddress1 = Regex.Replace(inputAddress1, @"^(EST) ", "EAST ");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (EST) ", " EAST ");


                    // put street at the end if it's just a number at the end. 
                    // Ex: 360 E 193
                    inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (WEST|NORTH|EAST|SOUTH) (\d+)$", "$1 $2 $3 STREET");
                    inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (\d+)$", "$1 $2 STREET");
                    inputAddress1 = Regex.Replace(inputAddress1, " ([A-Z]+)(DR)$", " $1 DRIVE");
                    inputAddress1 = Regex.Replace(inputAddress1, @" (DRI)$", " DRIVE");

                    // generic abbreviations. 
                    foreach (KeyValuePair<string, string> pair in data.Abbreviations)
                    {
                        inputAddress1 = inputAddress1.Replace(" " + pair.Key + " ", " " + pair.Value + " ");
                    }
                    #endregion

                    #region ApartmentNumber

                    // motivation: 202 E91ST ST B16
                    int startIndex = -1, length = -1;

                    Match apartmentNumberMatch = Regex.Match(inputAddress1, @" (UNIT|STE|SUITE|APT|APARTMENT) ([0-9]? ?[A-Z]? ?[0-9]?)+");
                    if (apartmentNumberMatch.Success)
                    {
                        startIndex = apartmentNumberMatch.Index;
                        length = apartmentNumberMatch.Length;

                        ret.ApartmentNumber = apartmentNumberMatch.Groups[2].Value;  // this is odd, always +1 the Number of matches. Groups[0] is the original string.
                        ret.ApartmentNumber = Regex.Replace(ret.ApartmentNumber, " ", "");
                    }

                    apartmentNumberMatch = Regex.Match(inputAddress1, @" (APT|APARTMENT) (\d+)$");
                    if (apartmentNumberMatch.Success)
                    {
                        startIndex = apartmentNumberMatch.Index;
                        length = apartmentNumberMatch.Length;

                        ret.ApartmentNumber = apartmentNumberMatch.Groups[2].Value;  // this is odd, always +1 the Number of matches. Groups[0] is the original string.
                        ret.ApartmentNumber = Regex.Replace(ret.ApartmentNumber, " ", "");
                    }
                    else
                    {
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
                                    apartmentNumberMatch = Regex.Match(inputAddress1, @" (\d+) FL$");
                                    if (apartmentNumberMatch.Success)
                                    {
                                        Group matchedGroup = apartmentNumberMatch.Groups[1];
                                        startIndex = apartmentNumberMatch.Index;
                                        length = apartmentNumberMatch.Length;
                                        ret.ApartmentNumber = matchedGroup.Value + " FLOOR";
                                    }
                                    else
                                    {
                                        apartmentNumberMatch = Regex.Match(inputAddress1, @" (\d+)L$");
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


                    string inputAddress2 = bits[Address2Column];
                    if (ret.ApartmentNumber == default(string) && inputAddress2.Length <= 5)
                    {
                        // Call these apartment numbers 
                        ret.ApartmentNumber = inputAddress2;
                    }

                    #endregion

                    #region SuffixNormalization
                    string[] inputAddress1Bits = inputAddress1.Split(' ');
                    string possibleSuffix = inputAddress1Bits[inputAddress1Bits.Length - 1];
                    string confirmedSuffix = null;

                    // is this a known bad suffix? 
                    if (data.SuffixReplacementKey.ContainsKey(possibleSuffix))
                    {
                        // yes, replace
                        confirmedSuffix = data.SuffixReplacementKey[possibleSuffix];
                    }

                    // did we replace above? 
                    if (confirmedSuffix == null)
                    {
                        // no, so look for this suffix. 
                        for (int c = 0; c < data.Suffixes.LongSuffixes.Length; c++)
                        {
                            string longSuffix = data.Suffixes.LongSuffixes[c];
                            if (possibleSuffix == longSuffix)
                            {
                                confirmedSuffix = longSuffix;
                                break;
                            }
                        }

                        if (confirmedSuffix == null)
                        {
                            for (int c = 0; c < data.Suffixes.ShortSuffixes.Length; c++)
                            {
                                string shortSuffix = data.Suffixes.ShortSuffixes[c];

                                if (possibleSuffix == shortSuffix)
                                {
                                    confirmedSuffix = data.Suffixes.LongSuffixes[c];
                                    break;
                                }
                            }
                        }
                    }

                    ret.Suffix = confirmedSuffix;
                    if (confirmedSuffix != null && inputAddress1.Length > 0)
                    {
                        // identify index of suffix to remove. 
                        int toRemove = inputAddress1.LastIndexOf(possibleSuffix);
                        if (toRemove == 0)
                        {
                            // if the entire name IS the suffix, then remove it, we have crap data. 
                            inputAddress1 = "";
                        }
                        else
                        {
                            // remove the suffix. 
                            inputAddress1 = inputAddress1.Substring(0, toRemove - 1);
                        }
                    }

                    #endregion

                    #region StreetNumber
                    // is there a street number? 
                    if (Regex.IsMatch(inputAddress1, @"^(\d+) .+"))
                    {
                        Match streetNumberMatch = Regex.Match(inputAddress1, @"^(\d+)");
                        ret.StreetNumber = streetNumberMatch.Value;
                    }
                    #endregion

                    #region StreetName

                    Match match = Regex.Match(inputAddress1, @"^(\d+ )?([A-Z 0-9]+)$");
                    if (match.Success)
                    {
                        ret.StreetName = match.Groups[2].Value;
                        ret.StreetName.Trim();
                    }

                    // handle addresses
                    ret = HandleParticularlyProblematicStreets(ret);

                    // PO BOX
                    match = Regex.Match(inputAddress1, @"(P ?O ?BOX|POB|P O B) ?(\d+)");
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
                        ret.StreetNameIsNumber = Regex.IsMatch(ret.StreetName, @" ?(\d+)$");
                    }

                    #endregion


                    if (ret.MatchQuality == MatchQuality.NotMatched && string.IsNullOrEmpty(ret.StreetName))
                    {
                        ret.MatchQuality = MatchQuality.CouldNotParseFormat;
                    }
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
    }
}
