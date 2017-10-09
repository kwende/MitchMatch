using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace challenge
{
    public class DataCleaningManager
    {
        private static int[] BadSSNs =
        {
            -1,
            000000000,
            999999999,
            123456789,
            111111111,
            666666666,
            888888888,
            555555555,
            333333333,
            777777777,
            444444444,
            222222222,
            432109876,
            109876543,
            654321098,
            000000001,
            098765432,
            765432109,
            210987654,
            678901234,
            789012345,
            876543210,
            901234567,
            456789012,
            234567890,
            890123456,
            321098765,
            543210987,
            567890123,
            345678901,
        };

        private static long[] BadPhones =
        {
            -1,
            0000000000,
            1111111111,
            2222222222,
            3333333333,
            4444444444,
            5555555555,
            6666666666,
            7777777777,
            8888888888,
            9999999999,
            1234567890,
        };


        private static string RemoveSpaces(string str)
        {
            return str.Replace(" ", String.Empty);
        }

        private static string StripStNdRdTh(string str)
        {
            return Regex.Replace(str, @"( |[a-zA-Z])(\d+)(ST|ND|RD|TH)( |[a-zA-Z])", "$1$2$4", RegexOptions.None);
        }

        private static string AddSpacesBetweenNumbersAndLetters(string str)
        {
            string numberSpaceLetter = Regex.Replace(str, @"(\d)([a-zA-Z])", "$1 $2", RegexOptions.None);
            string letterSpaceNumber = Regex.Replace(numberSpaceLetter, @"([a-zA-Z])(\d)", "$1 $2", RegexOptions.None);
            return letterSpaceNumber;
        }

        private static string TakeCareOfHomelessAddresses(string str)
        {
            if (str.StartsWith("HOM") ||
                str.StartsWith("UND") ||
                str.StartsWith("INDOM") ||
                str.StartsWith("NONDOM") ||
                str.StartsWith("H O M") ||
                str.StartsWith("H-O-M") ||
                str.StartsWith("H.O.M") ||
                str == "UDOMICILED" ||
                str == "UNSOMICILED" ||
                str == "SHELTER" ||
                str == "NONE" ||
                str == "NON")
            {
                return "HOMELESS";
            }
            return str;
        }

        private static object _lockObject = new object();
        private static Dictionary<string, string> _suffixReplacementKey = null;
        public static string TakeCareOfAddress1Suffix(string address1Line)
        {
            throw new Exception("DONT USE YET.");

            lock (_lockObject)
            {
                if (_suffixReplacementKey == null)
                {
                    _suffixReplacementKey = new Dictionary<string, string>();
                    string[] allLines = File.ReadAllLines("SuffixReplacementKey.txt");

                    foreach (string line in allLines)
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length == 2 && !_suffixReplacementKey.ContainsKey(parts[0]))
                        {
                            _suffixReplacementKey.Add(parts[0], parts[1]);
                        }
                    }
                }
            }
            string[] address1Bits = address1Line.Split(' ');

            for (int c = address1Bits.Length - 1; c >= 0; c--)
            {
                string address1Bit = address1Bits[c];
                if (_suffixReplacementKey.ContainsKey(address1Bit))
                {
                    address1Bits[c] = _suffixReplacementKey[address1Bit];
                    break;
                }
            }

            return string.Join(" ", address1Bits);
        }

        private static string TakeCareOfUnknownAddresses(string str)
        {
            if (str == "UNKN3146 86TH STREE")
            {
                str = "3146 86 ST";
            }
            else if (str.StartsWith("UNK") || str.StartsWith("UKN") || str == "UNABLE TO OBTAIN")
            {
                return "";
            }
            return str;
        }

        public static void CleanData(ref Row[] data, Row[] realData)
        {

            //////////////// SSNs ///////////////
            //var badSSNs = realData.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            foreach (Row row in data)
            {
                if (BadSSNs.Contains(row.SSN))
                {
                    row.SSN = 0;
                }
            }
            ///////////////////////////////////////////////////

            //////////////// NAMES ///////////////
            foreach (Row row in data)
            {
                row.FIRST = RemoveSpaces(row.FIRST);
                row.MIDDLE = RemoveSpaces(row.MIDDLE);
                row.LAST = RemoveSpaces(row.LAST);

                if (row.FIRST == "MALE")
                {
                    row.FIRST = "";
                }
                else if (row.FIRST == "FEMALE")
                {
                    row.FIRST = "";
                }

                if (row.LAST == "UNKNOWN")
                {
                    row.LAST = "";
                }
            }
            ///////////////////////////////////////////////////

            ////////////////// GENDER //////////////////
            foreach (Row row in data)
            {
                if (row.GENDER == "U")
                {
                    row.GENDER = "";
                }
                else if (row.GENDER == "FEMALE")
                {
                    row.GENDER = "F";
                }
                else if (row.GENDER == "MALE")
                {
                    row.GENDER = "M";
                }

                if (row.GENDER != "F" && row.GENDER != "M")  //Probably unnecessary, but doing it anyway
                {
                    row.GENDER = "";
                }
            }
            /////////////////////////////////////////////


            ////////////////// DOB //////////////////
            var badDOBs = data.GroupBy(r => r.DOB).Where(g => g.Count() >= 1000).Select(g => g.Key).ToArray();
            foreach (Row row in data)
            {
                if (badDOBs.Contains(row.DOB))
                {
                    row.DOB = default(DateTime);
                }
            }
            ///////////////////////////////////////

            ///////////////// PHONE1 ////////////////////
            //var badPhones = realData.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();
            foreach (Row row in data)
            {
                if (BadPhones.Contains(row.PHONE))
                {
                    row.PHONE = 0;
                }
            }
            //////////////////////////////////////////////

            /////////////// ALIAS /////////////////////
            foreach (Row row in data)
            {
                row.ALIAS = Regex.Replace(row.ALIAS, " +", " ", RegexOptions.None);
            }
            //////////////////////////////////////////


            ////////////// ADDRESS /////////////////////

            string[] originalLines = FileLibrary.GetLines().ToArray();

            int index = 0;
            string[] lines = File.ReadAllLines("CleanedAddresses.csv");
            for (int i = 1; i < data.Length; i++)
            {
                if (originalLines[i] != ",,,,,,,,,,,,,,,,,,")
                    data[index].ADDRESS1 = lines[i].Trim();
                if (data[index].ADDRESS1 == "UNKNOWN")
                    data[index].ADDRESS1 = "";
                index++;
            }

            //string[] lines = File.ReadAllLines("CleanedAddresses.csv");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    data[i].ADDRESS1 = lines[i];
            //}

            ///////////////////////////////////////////////

            ////////////////// CITY //////////////////
            foreach (Row row in data)
            {
                if (row.CITY.StartsWith("BROOKL") || row.CITY == "BKLYN")
                {
                    row.CITY = "BROOKLYN";
                }
                else if (row.CITY == "NY")
                {
                    row.CITY = "NEW YORK";
                }
                else if (row.CITY == "BX")
                {
                    row.CITY = "BRONX";
                }
                else if (row.CITY == "BKLYN" || row.CITY.StartsWith("BROOKL"))
                {
                    row.CITY = "BROOKLYN";
                }

            }
            ///////////////////////////////////////
        }
    }
}
