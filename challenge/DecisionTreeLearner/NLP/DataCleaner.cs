using DecisionTreeLearner.Data;
using DecisionTreeLearner.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DecisionTreeLearner.NLP
{
    public static class DataCleaner
    {
        private static string[] BadSSNs =
        {
            "000-00-0000",
            "999-99-9999",
            "123-45-6789",
            "111-11-1111",
            "666-66-6666",
            "888-88-8888",
            "555-55-5555",
            "333-33-3333",
            "777-77-7777",
            "444-44-4444",
            "222-22-2222",
            "432-10-9876",
            "109-87-6543",
            "654-32-1098",
            "000-00-0001",
            "098-76-5432",
            "765-43-2109",
            "210-98-7654",
            "678-90-1234",
            "789-01-2345",
            "876-54-3210",
            "901-23-4567",
            "456-78-9012",
            "234-56-7890",
            "890-12-3456",
            "321-09-8765",
            "543-21-0987",
            "567-89-0123",
            "345-67-8901",
        };

        public static List<Record> CleanRecordPairs(List<Record> records)
        {
            Console.Write("Cleaning data...");
            //for(int c=0;c<pairs.Count;c++)
            Parallel.For(0, records.Count, c =>
            {
                records[c] = CleanRecord(records[c]);
            });
            Console.WriteLine("...done");

            return records;
        }

        public static List<RecordPair> CleanRecordPairs(List<RecordPair> pairs,
            string streetSuffixesFile)
        {
            Console.Write("Cleaning data...");
            //for(int c=0;c<pairs.Count;c++)
            Parallel.For(0, pairs.Count, c =>
            {
                pairs[c] = CleanRecordPair(pairs[c], streetSuffixesFile);
            });
            Console.WriteLine("...done");

            return pairs;
        }

        public static RecordPair CleanRecordPair(RecordPair pair, string streetSuffixesFile)
        {
            pair.Record1 = CleanRecord(pair.Record1);
            pair.Record2 = CleanRecord(pair.Record2);

            return pair;
        }

        public static string CleanAddress(string address)
        {
            if (address == "UNKNOWN" ||
                address == "UNKOWN" ||
                address == "UNK" ||
                address == "XXX" ||
                address.StartsWith("UNABL")
                )
            {
                return "";
            }

            if (address == "HOMELESS" ||
                address == "UNDOMICILED" ||
                address == "UNDOMICILE" ||
                address == "H O M E L E S S")
            {
                return "HOMELESS";
            }

            List<Tuple<string, string>> suffixes = AddressSuffixLoader.GetStreetSuffixAbbreviationTuples();

            string cleaned =
    address.ToUpper().Replace(" WEST ", " W ").Replace(" EAST ", " E ").Replace(" NORTH ", " N ").Replace(" SOUTH ", " S ").Replace(" SO ", " S ");

            cleaned = Regex.Replace(cleaned, @"(\d)(ST|ND|RD|TH)\b", "$1");

            for (int c = 0; c < suffixes.Count; c++)
            {
                Tuple<string, string> suffix = suffixes[c];
                if (cleaned.EndsWith(suffix.Item1))
                {
                    cleaned = cleaned.Replace($" {suffix.Item1}", $" {suffix.Item2}");
                }
            }

            return cleaned;
        }

        public static Record CleanRecord(Record input)
        {
            ////////////////// CITY //////////////////
            if (input.City == "BKLYN")
            {
                input.City = "BROOKLYN";
            }
            else if (input.City == "NY")
            {
                input.City = "NEW YORK";
            }
            else if (input.City == "BX")
            {
                input.City = "BRONX";
            }
            ///////////////////////////////////////


            ///////////////////// SSN//////////////////////
            input.SSN = input.SSN.Replace("-", "");
            if (BadSSNs.Contains(input.SSN))
            {
                input.SSN = "";
            }
            if (input.SSN == "0" || input.SSN == "-1")
            {
                input.SSN = "";
            }
            ////////////////////////////////////////////

            ////////////////// GENDER //////////////////
            if (input.Gender == "U")
            {
                input.Gender = "";
            }
            else if (input.Gender == "FEMALE")
            {
                input.Gender = "F";
            }
            else if (input.Gender == "MALE")
            {
                input.Gender = "M";
            }
            /////////////////////////////////////////////


            /////////////////// ADDRESS2 /////////////////////
            if (input.Address2 == "UNKNOWN" ||
                input.Address2 == "UNKOWN" ||
                input.Address2 == "UNK" ||
                input.Address2 == "UNABLE TO OBTAIN")
            {
                input.Address2 = "";
            }
            /////////////////////////////////////////////////

            ///////////////// PHONE1 ////////////////////
            // all the same digit? 
            input.Phone1 = input.Phone1.Replace("-", "");
            input.Phone2 = input.Phone2.Replace("-", "");
            if (Regex.IsMatch(input.Phone1, @"^([0-9])\1*$"))
            {
                input.Phone1 = "";
            }
            if (input.Phone1 == "0" || input.Phone1 == "-1" || input.Phone1 == "1234567890")
            {
                input.Phone1 = "";
            }
            //////////////////////////////////////////////

            /////////////// PHONE2 //////////////////
            // all the same digit? 
            if (Regex.IsMatch(input.Phone2.Replace("-", ""), @"^([0-9])\1*$"))
            {
                input.Phone2 = "";
            }
            /////////////////////////////////////////

            ////////////// ADDRESS1 /////////////////////
            input.Address1 = CleanAddress(input.Address1);
            var largeResidenceList = LargeResidenceFileLoader.GetLargeResidenceList();
            if (largeResidenceList.Contains(input.Address1))
            {
                input.LivesInLargeResidence = true;
            }

            ///////////////////////////////////////////////

            /////////////// ALIAS /////////////////////
            input.Alias = Regex.Replace(input.Alias, " +", " ", RegexOptions.None); 


            //////////////////////////////////////////


            return input;
        }
    }
}
