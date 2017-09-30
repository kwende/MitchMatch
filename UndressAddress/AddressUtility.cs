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
        public static string CleanNYDatabaseAddress(string address, AddressSuffixes suffixes)
        {
            address = Regex.Replace(address, " +", " ");

            for (int c = 0; c < suffixes.LongSuffixes.Length; c++)
            {
                string targetSuffix = " " + suffixes.LongSuffixes[c];
                if (address.EndsWith(targetSuffix))
                {
                    int lastIndexOf = address.LastIndexOf(targetSuffix);
                    address = address.Substring(0, lastIndexOf) + " " + suffixes.ShortSuffixes[c];
                    break;
                }
            }

            address = Regex.Replace(address, @" (N)$", " NORTH");
            address = Regex.Replace(address, @" (S)$", " SOUTH");
            address = Regex.Replace(address, @" (E)$", " EAST");
            address = Regex.Replace(address, @" (W)$", " WEST");

            address = Regex.Replace(address, @"^(N) ", "NORTH ");
            address = Regex.Replace(address, @"^(S) ", "SOUTH ");
            address = Regex.Replace(address, @"^(E) ", "EAST ");
            address = Regex.Replace(address, @"^(W) ", "WEST ");

            address = Regex.Replace(address, @" (N) ", " NORTH ");
            address = Regex.Replace(address, @" (S) ", " SOUTH ");
            address = Regex.Replace(address, @" (E) ", " EAST ");
            address = Regex.Replace(address, @" (W) ", " WEST ");

            return address;
        }

        public static Address InitializeAddress(string input, Data data)
        {
            try
            {
                Address ret = new Address();
                ret.MatchQuality = MatchQuality.Unknown;

                const int Address1Column = 8;
                const int ZipColumn = 10;
                const int CityColumn = 13;

                string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(input);
                string inputAddress1 = bits[Address1Column];

                ret.RawAddress1 = inputAddress1;

                if (inputAddress1.Length > 0)
                {
                    bool identifiedAsHomelessOrUnknown = false;

                    #region DetermineHomelessOrUnknown
                    foreach (string homelessAddress in data.HomelessAddresses)
                    {
                        if (inputAddress1.StartsWith(homelessAddress))
                        {
                            ret.MatchQuality = MatchQuality.Homeless;
                            identifiedAsHomelessOrUnknown = true;
                            break;
                        }
                    }

                    if (!identifiedAsHomelessOrUnknown)
                    {
                        foreach (string unknownAddress in data.UnknownAddresses)
                        {
                            if (inputAddress1.StartsWith(unknownAddress))
                            {
                                ret.MatchQuality = MatchQuality.Unknown;
                                identifiedAsHomelessOrUnknown = true;
                                break;
                            }
                        }
                    }
                    #endregion

                    if (!identifiedAsHomelessOrUnknown)
                    {
                        #region Zip/City
                        int possibleZip = 0;
                        if (int.TryParse(bits[ZipColumn], out possibleZip))
                        {
                            ret.Zip = possibleZip;
                        }
                        ret.City = bits[CityColumn];
                        #endregion

                        #region GenericStringCleaning
                        // replace multiple spaces with one. 
                        inputAddress1 = Regex.Replace(inputAddress1, " +", " ");

                        // remove periods
                        inputAddress1 = inputAddress1.Replace(".", "");

                        // remove the suffix at the end of 1st or 2nd or 3rd, etc. 
                        inputAddress1 = Regex.Replace(inputAddress1, @"(\d+)(ST|ND|RD|TH)", "$1");
                        inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (ST|ND|RD|TH) ", " $1 ");

                        // separate the East/West portion from the street number. 
                        // Ex: "460 E46 STREET"
                        inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (N|S|W|E)(\d+)", "$1 $2 $3");

                        // split apart the N/S/E/W if catenated to number. 
                        // Ex: 219E 121
                        inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+)(N|S|E|W) ", "$1 $2");

                        // split up addresses that are stuck together. 
                        // Ex: 543 W180THST
                        inputAddress1 = Regex.Replace(inputAddress1, @"(E|W|S|N)(\d+)(ST|ND|RD|TH)(.+)", "$1 $2 $4");

                        // 1668 W.6 ST
                        inputAddress1 = Regex.Replace(inputAddress1, @" (E|W|N|S)\.(\d+) ", " $1 $2 ");

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

                        // put street at the end if it's just a number at the end. 
                        // Ex: 360 E 193
                        inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (.+) (\d+)$", "$1 $2 $3 STREET");

                        // Replace WEST132 with WEST 132
                        inputAddress1 = Regex.Replace(inputAddress1, @" (WEST|NORTH|SOUTH|EAST)(\d+) ", " $1 $2 ");

                        // Replace "FTJOHN" with "FORT JOHN"
                        inputAddress1 = Regex.Replace(inputAddress1, @" (FT)([A-Z]+) ", " FORT $2 ");

                        // Replace "1387 STJOHNS PL" with "1387 ST JOHNS PL"
                        inputAddress1 = Regex.Replace(inputAddress1, @" (ST)([BCDFGHJKLMNPQSTVWXZ])([A-Z]+) ", " $1 $2$3 ");

                        // break up number/street suffix. 
                        inputAddress1 = Regex.Replace(inputAddress1, @"(\d+)(AVE)", "$1 $2");

                        // Remove RST from ending of numbers. 
                        inputAddress1 = Regex.Replace(inputAddress1, @" (\d+)(RST) ", " $1 ");

                        // generic abbreviations. 
                        foreach (KeyValuePair<string, string> pair in data.Abbreviations)
                        {
                            inputAddress1 = inputAddress1.Replace(" " + pair.Key + " ", " " + pair.Value + " ");
                        }

                        #endregion

                        string[] inputAddress1Bits = inputAddress1.Split(' ');

                        #region ApartmentNumber
                        // this matching is kind of ... well, not great. I'd like to see examples. I'm not sure if want
                        // to blindly take the text proceeding 'apartment' to just be the apartment number. maybe we do? 
                        Match apartmentNumberMatch = Regex.Match(inputAddress1, @" (APT|APARTMENT) ([0-9]+[A-Z]) ");
                        string partToDelete = "";
                        if (apartmentNumberMatch.Success) // this is odd, always +1 the Number of matches. Groups[0] is the original string. 
                        {
                            partToDelete = apartmentNumberMatch.Value;
                            ret.ApartmentNumber = apartmentNumberMatch.Groups[2].Value;
                        }
                        // motivation: 202 E91ST ST B16
                        apartmentNumberMatch = Regex.Match(inputAddress1, @"^(\d+) ([A-Z 0-9]+) ([A-Z]{1,2}[0-9]{1,2}|[0-9]{1,2}[A-Z]{1,2})$");
                        if (apartmentNumberMatch.Success)
                        {
                            partToDelete = apartmentNumberMatch.Value;
                            ret.ApartmentNumber = apartmentNumberMatch.Groups[3].Value;
                        }

                        if (!string.IsNullOrEmpty(partToDelete))
                        {
                            inputAddress1 = inputAddress1.Replace(partToDelete, " ");
                            inputAddress1 = inputAddress1.Trim();
                        }

                        #endregion

                        #region SuffixNormalization
                        string possibleSuffix = inputAddress1Bits[inputAddress1Bits.Length - 1];
                        string confirmedSuffix = null;
                        for (int c = 0; c < data.Suffixes.LongSuffixes.Length; c++)
                        {
                            string longSuffix = data.Suffixes.LongSuffixes[c];
                            if (possibleSuffix == longSuffix)
                            {
                                confirmedSuffix = longSuffix;
                                break;
                            }
                            else if (longSuffix.Length >= 4 && EditDistance.Compute(possibleSuffix, longSuffix) == 1)
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
                        Match streetNumberMatch = Regex.Match(inputAddress1, @"^(\d+)[A-Z]? ");
                        if (streetNumberMatch.Value.Length > 0)
                        {
                            ret.StreetNumber = streetNumberMatch.Value;
                        }
                        #endregion

                        #region StreetName
                        // examine standard format
                        //if (Regex.IsMatch(inputAddress1, @"^(\d+) ([A-Z 0-9]+)"))
                        //{
                        //    Match standardAddressPortion = Regex.Match(inputAddress1, @"^(\d+) ([A-Z 0-9]+)");
                        //    ret.StreetName = standardAddressPortion.Groups[2].Value;
                        //}
                        //else
                        //{
                        Match match = Regex.Match(inputAddress1, @"^(\d+ )?([A-Z 0-9]+)$");
                        if (match.Success)
                        {
                            ret.StreetName = match.Groups[2].Value;
                        }
                        //}

                        if (ret.StreetName != null)
                        {
                            ret.StreetNameIsNumber = Regex.IsMatch(ret.StreetName, @" ?(\d+)$");
                        }

                        #endregion

                        if (!string.IsNullOrEmpty(ret.StreetName))
                        {
                            ret.MatchQuality = MatchQuality.NotMatched;
                        }
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine(input);
                Console.WriteLine();
                throw ex;
            }
        }
    }
}
