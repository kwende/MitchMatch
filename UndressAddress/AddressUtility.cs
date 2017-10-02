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


        private static MatchQuality IsHomelessOrUnknown(string inputAddress1, Data data)
        {
            bool identifiedAsHomelessOrUnknown = false;

            foreach (string homelessAddress in data.HomelessAddresses)
            {
                if (inputAddress1.StartsWith(homelessAddress))
                {
                    return MatchQuality.Homeless;
                }
            }
            if (inputAddress1 != "UNKN3146 86TH STREE")
            {
                if (!identifiedAsHomelessOrUnknown)
                {
                    foreach (string unknownAddress in data.UnknownAddresses)
                    {
                        if (inputAddress1.StartsWith(unknownAddress))
                        {
                            return MatchQuality.Unknown;
                        }
                    }
                }
            }

            return MatchQuality.NotMatched;
        }

        public static Address InitializeAddress(string input, Data data)
        {
            try
            {
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


                if(inputAddress1 == "")
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
                        // replace multiple spaces with one.
                        inputAddress1 = Regex.Replace(inputAddress1, " +", " ");

                        // remove periods
                        inputAddress1 = inputAddress1.Replace(".", "");

                        // Put spaces between numbers and letters
                        inputAddress1 = Regex.Replace(inputAddress1, @"(\d+)([A-Z]+)", "$1 $2");
                        inputAddress1 = Regex.Replace(inputAddress1, @"([A-Z]+)(\d+)", "$1 $2");


                        // remove the suffix at the end of 1st or 2nd or 3rd, etc. 
                        inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (RST|ND|RD|TH) ", " $1 ");
                        inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (RST|ND|TH)$", " $1");
                        inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (ST) (ST|AVE)", " $1 $3");



                        // Replace "FTJOHN" with "FORT JOHN"
                        inputAddress1 = Regex.Replace(inputAddress1, @" (FT)([A-Z]+) ", " FORT $2 ");

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

                        // put street at the end if it's just a number at the end. 
                        // Ex: 360 E 193
                        inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (WEST|NORTH|EAST|SOUTH) (\d+)$", "$1 $2 $3 STREET");
                        inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (\d+)$", "$1 $2 STREET");



                        // generic abbreviations. 
                        foreach (KeyValuePair<string, string> pair in data.Abbreviations)
                        {
                            inputAddress1 = inputAddress1.Replace(" " + pair.Key + " ", " " + pair.Value + " ");
                        }

                        #endregion

                        #region ApartmentNumber
                        Match apartmentNumberMatch = Regex.Match(inputAddress1, @" (UNIT|STE|SUITE|APT|APARTMENT) ([0-9]? ?[A-Z]? ?[0-9]?)+");
                        string partToDelete = "";
                        if (apartmentNumberMatch.Success)
                        {
                            partToDelete = apartmentNumberMatch.Value;
                            ret.ApartmentNumber = apartmentNumberMatch.Groups[2].Value;  // this is odd, always +1 the Number of matches. Groups[0] is the original string.
                            ret.ApartmentNumber = Regex.Replace(ret.ApartmentNumber, " ", "");
                        }
                        else
                        {
                            // motivation: 202 E91ST ST B16
                            apartmentNumberMatch = Regex.Match(inputAddress1, @"^(\d+) ([A-Z0-9]+) ([A-Z 0-9]+) ([0-9]+|[A-Z] [0-9]{1,2}|[0-9]{1,2} [A-Z])$");
                            if (apartmentNumberMatch.Success)
                            {
                                partToDelete = apartmentNumberMatch.Groups[apartmentNumberMatch.Groups.Count - 1].Value;
                                ret.ApartmentNumber = partToDelete;
                                ret.ApartmentNumber = Regex.Replace(ret.ApartmentNumber, " ", "");
                            }
                        }

                        if (!string.IsNullOrEmpty(partToDelete))
                        {
                            inputAddress1 = inputAddress1.Replace(partToDelete, " ");
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
                            ret.StreetName.Trim();
                        }
                        //}

                        if (ret.StreetName != null)
                        {
                            ret.StreetNameIsNumber = Regex.IsMatch(ret.StreetName, @" ?(\d+)$");
                        }

                        #endregion


                        if (ret.MatchQuality == MatchQuality.NotMatched)
                        {
                            ret.MatchQuality = (string.IsNullOrEmpty(ret.StreetName)) ? MatchQuality.NotMatched : MatchQuality.FullAddressMatched;
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
