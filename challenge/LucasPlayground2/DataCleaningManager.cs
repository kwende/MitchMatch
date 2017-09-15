using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucasPlayground2
{
    public class DataCleaningManager
    {
        private static int[] BadSSNs =
        {
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

        private static string AddSpacesBetweenNumbersAndLetters(string str)
        {
            return str;
        }

        private static string TakeCareOfHomelessAddresses(string str)
        {
            if (str.StartsWith("UNKNO") || str.StartsWith("HOM") || str.StartsWith("UND") || str == "H O M E L E S S" || str == "SHELTER")
            {
                return "HOMELESS";
            }
            return str;
        }

        private static string TakeCareOfUnknownAddresses(string str)
        {
            if (str.StartsWith("UNK") || str.StartsWith("UKN") || str == "UNABLE TO OBTAIN")
            {
                return "";
            }
            return str;
        }

        public static void CleanData(ref Row[] data, Row[] realData)
        {

            //////////////// SSNs ///////////////
            //var badSSNs = realData.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            //var badSSNs2 = data.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
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
            //var badPhones2 = data.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();
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
            //var badAddresses = realData.GroupBy(r => r.ADDRESS1).Where(g => !g.Key.Contains(' ') && g.Count() > 2).Select(g => g.Key).ToArray();
            foreach (Row row in data)
            {
                row.ADDRESS1 = AddSpacesBetweenNumbersAndLetters(row.ADDRESS1);

                row.ADDRESS1 = TakeCareOfHomelessAddresses(row.ADDRESS1);

                row.ADDRESS1 = TakeCareOfUnknownAddresses(row.ADDRESS1);
                row.ADDRESS2 = TakeCareOfUnknownAddresses(row.ADDRESS2);
            }

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
