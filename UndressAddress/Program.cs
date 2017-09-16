using DecisionTreeLearner.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace UndressAddress
{
    class Program
    {
        static bool IsNumber(string input)
        {
            double tmp;
            return double.TryParse(input, out tmp);
        }

        static bool IsAlphaNumeric(string input)
        {
            bool isAlphaNumeric = false;

            bool foundLetter = false, foundNumber = false;
            for (int c = 0; c < input.Length; c++)
            {
                if (char.IsDigit(input[c]))
                {
                    foundNumber = true;
                }
                else if (char.IsLetter(input[c]))
                {
                    foundLetter = true;
                }

                if (foundNumber && foundLetter)
                {
                    isAlphaNumeric = true;
                    break;
                }
            }

            return isAlphaNumeric;
        }

        static List<string> GetValidStreetSuffixes()
        {
            List<string> allSuffixes = new List<string>();

            allSuffixes.AddRange(File.ReadAllLines("StreetSuffixes.csv").Select(n => n.Split(',')[0]));
            allSuffixes.AddRange(File.ReadAllLines("StreetSuffixes.csv").Select(n => n.Split(',')[1]));
            allSuffixes.Add("BUILDING");
            allSuffixes.Add("BUIDLING");
            allSuffixes.Add("BUIDING");
            allSuffixes.Add("BLDING");
            allSuffixes.Add("BULIDING");
            allSuffixes.Add("BULDING");
            allSuffixes.Add("BLDDG");
            allSuffixes.Add("BLDG");
            allSuffixes.Add("BLUILDING");
            allSuffixes.Add("BLDNG");
            allSuffixes.Add("BLG");
            allSuffixes.Add("BULD");
            allSuffixes.Add("PALZA");
            allSuffixes.Add("HOUSE");
            allSuffixes.Add("CRT");
            allSuffixes.Add("PLA");
            allSuffixes.Add("CTR");
            allSuffixes.Add("PLC");
            allSuffixes.Add("LA");
            allSuffixes.Add("BLD");
            allSuffixes.Add("LANS");
            allSuffixes.Add("AVEE");
            allSuffixes.Add("COUR");
            allSuffixes.Add("LANEE");
            allSuffixes.Add("COUR");
            allSuffixes.Add("HOSPITAL");
            allSuffixes.Add("GATE");
            allSuffixes.Add("STEET");
            allSuffixes.Add("OHS");
            allSuffixes.Add("FACIL");
            allSuffixes.Add("STREE");
            allSuffixes.Add("HOSP");
            allSuffixes.Add("LAN");
            allSuffixes.Add("FACILITY");
            allSuffixes.Add("SLIP");
            allSuffixes.Add("PLAZ");
            allSuffixes.Add("ROA");
            allSuffixes.Add("STREEET");
            allSuffixes.Add("TERR");
            allSuffixes.Add("DRIVET");
            allSuffixes.Add("LANES");
            allSuffixes.Add("PZ");
            allSuffixes.Add("STR");
            allSuffixes.Add("AV");
            allSuffixes.Add("AVEUE");
            allSuffixes.Add("PKWY");
            allSuffixes.Add("APT");

            return allSuffixes;
        }

        static List<string> GetCleanedNYStreetList(List<string> validSuffixes)
        {
            IEnumerable<string> lines = File.ReadLines(@"C:\Users\brush\Desktop\city_of_new_york.csv");

            List<string> streets = new List<string>(), shortenedStreets = new List<string>();
            bool header = false;
            //foreach (string line in lines)
            Parallel.ForEach(lines, line =>
            {
                if (!header)
                {
                    header = true;
                }
                else if (header)
                {
                    string[] bits = line.Split(',').Select(n => n.Trim()).ToArray();
                    string streetName = bits[3];
                    string originalStreetName = streetName;
                    if (!streets.Contains(originalStreetName))
                    {
                        foreach (string suffix in validSuffixes)
                        {
                            if (streetName.EndsWith(" " + suffix))
                            {
                                int removeAt = streetName.IndexOf(" " + suffix);
                                if (removeAt != -1)
                                {
                                    streetName = streetName.Substring(0, removeAt);
                                }
                            }
                        }
                        if (!IsNumber(streetName))
                        {
                            lock (streets)
                            {
                                streets.Add(originalStreetName);
                                shortenedStreets.Add(streetName);
                            }
                        }
                    }
                }
            });

            return shortenedStreets;
        }

        static bool MatchesAddressFormat(string line, List<string> streetSuffixes)
        {
            bool matchesAddressFormat = false;
            string[] addressBits = line.Split(' ');
            if ((addressBits.Length == 3 || addressBits.Length == 2) && IsNumber(addressBits[0])
                && !IsNumber(addressBits[1]))
            {
                matchesAddressFormat = true;
            }
            else if (addressBits.Length == 2 && IsNumber(addressBits[0]) &&
                IsAlphaNumeric(addressBits[1]))
            {
                matchesAddressFormat = true;
            }
            else if (Regex.IsMatch(line, @"\d+(ST|ND|RD|TH)"))
            {
                matchesAddressFormat = true;
            }
            else
            {
                foreach (string suffix in streetSuffixes)
                {
                    if (line.EndsWith(suffix) || line.Contains(" " + suffix + " "))
                    {
                        matchesAddressFormat = true;
                        break;
                    }
                }
            }
            return matchesAddressFormat;
        }

        static void Main(string[] args)
        {
            List<string> allSuffixes = GetValidStreetSuffixes();
            List<string> shortenedStreets = GetCleanedNYStreetList(allSuffixes);

            IEnumerable<string> allLines = File.ReadLines("c:/users/brush/desktop/finaldataset.csv");

            List<string> rejects = new List<string>();
            List<string> unmatchedButPossible = new List<string>();
            int iteration = 0;

            //foreach (string line in allLines)
            Parallel.ForEach(allLines, line =>
            {
                Interlocked.Increment(ref iteration);

                if (iteration % 1000 == 0)
                {
                    Console.WriteLine($"{iteration}");
                }

                string[] bits = DataLoader.SmartSplit(line);

                string address1 = bits[8];


                bool isRecognized = false;
                foreach (string shortenedStreet in shortenedStreets)
                {
                    if (shortenedStreet.Length > 1 && address1.Contains(shortenedStreet))
                    {
                        isRecognized = true;
                        break;
                    }
                }

                if (!isRecognized)
                {
                    if (!MatchesAddressFormat(address1, allSuffixes))
                    {
                        if (!rejects.Contains(address1))
                        {
                            lock (rejects)
                            {
                                rejects.Add(address1);

                            }
                        }
                    }
                }
            });

            using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/rejects.txt"))
            {
                foreach (string reject in rejects)
                {
                    //Console.WriteLine(reject);
                    //ConsoleKeyInfo response = Console.ReadKey();
                    //if (response.Key == ConsoleKey.N)
                    //{
                    sw.WriteLine(reject);
                    //}
                    //else
                    //{
                    //    unmatchedButPossible.Add(reject);
                    //}
                }
            }

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/unmatchedButPossible.txt"))
            //{
            //    foreach (string unmatched in unmatchedButPossible)
            //    {
            //        sw.WriteLine(unmatched);
            //    }
            //}

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/justStreets.csv"))
            //{
            //    foreach (string street in shortenedStreets)
            //    {
            //        sw.WriteLine(street);
            //    }
            //}

            //Console.WriteLine($"Picked up {streets.Count} streets.");
        }
    }
}
