using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    class Program
    {
        static void Main(string[] args)
        {
            Random random = new Random();

            var lines = File.ReadLines(@"C: \Users\jbrownkramer\Desktop\Data\data.csv");
            var allData = lines.Select(l => ParseRow(l)).ToArray();
            var data = allData.Where(r => r.EnterpriseID >= 15374761).ToArray();
            Console.WriteLine(lines.Count() + " total rows" ); // >= 15374761

            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();
            AddMatches(data, r => r.SSN, 4, ref matches);
            AddMatches(data, r => r.PHONE, 5, ref matches);
            AddMatches(data, r => r.LAST + r.FIRST + r.DOB.ToString("d"), 4, ref matches);

            Console.WriteLine(matches.Count() + " matched entries");

            for(int i = 0; i < 10; i++)
            {
                int nextTry = random.Next(data.Count());
                while(matches.ContainsKey(data[nextTry].EnterpriseID))
                {
                    nextTry = random.Next(data.Count());
                }
                Console.WriteLine(data[nextTry].EnterpriseID);
            }

            int nPaired = 0;
            int nUniquelyPaired = 0;




            for (int i = 0; i < data.Count(); i++)
            {
                int maxExtent = 0;
                int nMaxMatches = 0;
                for (int j = 0; j < data.Count(); j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    int matchExtent = MatchExtent(data[i], data[j]);

                    if (matchExtent > maxExtent)
                    {
                        maxExtent = matchExtent;
                        nMaxMatches = 0;
                    }

                    if (matchExtent == maxExtent && matchExtent > 0)
                    {
                        nMaxMatches++;
                    }
                }

                if (nMaxMatches > 0)
                    nPaired++;
                else
                {
                    Console.WriteLine(data[i].EnterpriseID);
                }

                if (nMaxMatches == 1)
                    nUniquelyPaired++;

                if (i % 100 == 0)
                    Console.Write("\r" + (i + 1) + " " + nPaired + " " + nUniquelyPaired);
            }
            Console.ReadLine();
        }

        static void AddMatches<T>(IEnumerable<row> data, Func<row,T> groupingValue, int sizeToThrowAway, ref Dictionary<int, List<int>> matches)
        {
            var grouped = data.GroupBy(groupingValue);
            Console.WriteLine(grouped.Where(g => g.Count() >= sizeToThrowAway).Count());
            int counter = 0;
            foreach (var group in grouped)
            {
                if (group.Count() >= sizeToThrowAway) //These are all garbage
                {
                    //Console.WriteLine(group.Key);
                    continue;
                }
                if (group.Count() < 2)
                {
                    continue;
                }

                var representative = group.First();
                if (group.Any(r => !PartialMatch(r, representative)))
                {
                    Console.WriteLine(group.Key);
                    counter++;
                }
                else
                {
                    foreach (var r in group)
                    {
                        if (!matches.ContainsKey(r.EnterpriseID))
                        {
                            matches[r.EnterpriseID] = new List<int>();
                        }
                        matches[r.EnterpriseID] = matches[r.EnterpriseID].Union(group.Select(r2 => r2.EnterpriseID)).ToList();
                    }
                }
            }

            Console.WriteLine(counter);
        }

        static bool PartialMatch(row a, row b)
        {
            return (a.FIRST != "" && a.FIRST == b.FIRST) || (a.LAST != "" && a.LAST == b.LAST) || DateSoftMatch(a.DOB,b.DOB);
        }

        static bool DateSoftMatch(DateTime a, DateTime b)
        {
            if (a == default(DateTime) || b == default(DateTime))
                return false;

            return OneDifference(a.ToString("d"), b.ToString("d"));
            
        }

        static bool OneDifference(string sm, string sn)
        {
            if (sm.Length != sn.Length)
                return false;

            int nd = 0;
            for(int i = 0; i < sn.Length; i++)
            {
                if (sm[i] != sn[i])
                    nd++;
            }

            return nd <= 1;
        }

        static int MatchExtent(row a, row b)
        {
            int toReturn = 0;
            if (PositiveMatch(a.SSN, b.SSN))
                toReturn+=16;
            if (PositiveMatch(a.PHONE, b.PHONE))
                toReturn+=8;
            if (Name(a) == Name(b))
                toReturn+=4;

            return toReturn;

            if (a.ADDRESS1 == b.ADDRESS1)
                toReturn+=2;
            if (a.DOB == b.DOB)
                toReturn+=1;

            return toReturn;
        }

        static string Name(row a)
        {
            return a.FIRST + " " + a.LAST;
        }

        static bool PositiveMatch(long a, long b)
        {
            return a > 0 && b > 0 && a == b;
        }

        static int ParseInt(string s)
        {
            string digits = "";
            for(int i = 0; i < s.Length; i++)
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

        static string[] Tokens(string line)
        {
            return line.Split(',');
        }

        static row ParseRow(string line)
        {
            var tokens = Tokens(line);
            row toReturn = new row
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
    }



    public class row
    {
        public int EnterpriseID;
        public string LAST;
        public string FIRST;
        public string MIDDLE;
        public string SUFFIX;
        public DateTime DOB;
        public string GENDER;
        public int SSN;
        public string ADDRESS1;
        public string ADDRESS2;
        public int ZIP;
        public string MOTHERS_MAIDEN_NAME;
        public int MRN;
        public string CITY;
        public string STATE;
        public long PHONE;
        public string PHONE2;
        public string EMAIL;
        public string ALIAS;
    }
}
