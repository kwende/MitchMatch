using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground2
{
    public class FileManager
    {
        public static IEnumerable<string> GetLines()
        {
            IEnumerable<string> lines = null;
            if (Environment.UserName.ToLower().Contains("sabalka"))
            {
                lines = File.ReadLines(@"C:/github/PMAC/FInalDataset.csv");
            }
            else if (Environment.UserName.ToLower().Contains("brush"))
            {
                lines = File.ReadLines(@"C:/users/brush/desktop/FInalDataset.csv");
            }
            else if (Environment.UserName.ToLower().Contains("ben"))
            {
                lines = File.ReadLines(@"C:/users/ben/desktop/FInalDataset.csv");
            }
            else if (Environment.UserName.ToLower().Contains("jbrownkramer") ||
                Environment.UserName.ToLower().Contains("josh"))
            {
                //lines = File.ReadLines(@"C:\Users\jbrownkramer\Desktop\Data\data.csv");
                lines = File.ReadLines(@"C:\Users\jbrownkramer\Desktop\FInalDataset.csv");
            }


            return lines;
        }

        static int ParseInt(string s)
        {
            string digits = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                    digits += s[i];
            }

            if (digits == "")
            {
                return -1;
            }


            return int.Parse(digits);
        }

        static long ParseLong(string s)
        {
            string digits = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                    digits += s[i];
            }

            if (digits == "")
            {
                return -1;
            }

            long toReturn;
            if (long.TryParse(digits, out toReturn))
            {
                return toReturn;
            }
            else
            {
                Console.WriteLine(s);
                return -1;
            }
        }

        static DateTime ParseDate(string s)
        {
            DateTime toReturn;
            if (s.Trim() == "")
                return default(DateTime);

            if (!DateTime.TryParse(s, out toReturn))
            {
                Console.WriteLine("Bad date : " + s);
                return default(DateTime);
            }
            else
            {
                return toReturn;
            }
        }
        public static Row ParseRow(string line)
        {
            var tokens = line.Split(',');
            Row toReturn = new Row
            {
                EnterpriseID = ParseInt(tokens[0]),
                LAST = tokens[1].Trim(),
                FIRST = tokens[2].Trim(),
                MIDDLE = tokens[3].Trim(),
                SUFFIX = tokens[4].Trim(),
                DOB = ParseDate(tokens[5]),
                GENDER = tokens[6].Trim(),
                SSN = ParseInt(tokens[7]),
                ADDRESS1 = tokens[8].Trim(),
                ADDRESS2 = tokens[9].Trim(),
                ZIP = ParseInt(tokens[10]),
                MOTHERS_MAIDEN_NAME = tokens[11].Trim(),
                MRN = ParseInt(tokens[12]),
                CITY = tokens[13].Trim(),
                STATE = tokens[14].Trim(),
                PHONE = ParseLong(tokens[15]),
                PHONE2 = tokens[16].Trim(),
                EMAIL = tokens[17].Trim(),
                ALIAS = tokens[18].Trim()
            };

            return toReturn;
        }

        public static ClosedSets LoadOriginalMatches(Row[] allData)
        {
            IEnumerable<string> lines = File.ReadLines(@"..\..\..\..\submission1.csv");

            ClosedSets closedSets = new ClosedSets(allData);

            foreach (string line in lines)
            {
                var eidsAsStrings = line.Split(',');
                List<int> eids = new List<int>();
                for(int i = 0; i < eidsAsStrings.Length - 1; i++)
                {
                    int eid;
                    int.TryParse(eidsAsStrings[i], out eid);
                    eids.Add(eid);
                }
                closedSets.AddMatch(eids);
            }

            return closedSets;
        }
    }
}
