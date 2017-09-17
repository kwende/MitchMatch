using DecisionTreeLearner.Data;
using DecisionTreeLearner.NLP;
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
            allSuffixes.Add("ST.");

            return allSuffixes;
        }

        static List<string> GetCleanedNYStreetList(List<string> validSuffixes)
        {
            IEnumerable<string> lines = File.ReadLines(@"C:\Users\ben\Desktop\city_of_new_york.csv");

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
            else if (line.Contains("PO ") || line.Contains("POBOX ") || line.Contains("POB ") || line.Contains("P O "))
            {
                matchesAddressFormat = true;
            }
            else if (line.Contains("DEPT ") || line.Contains("DEPARTMENT"))
            {
                matchesAddressFormat = true;
            }
            else if (line.Contains("ROOM "))
            {
                matchesAddressFormat = true;
            }
            else if (line.Contains("ROUTE"))
            {
                matchesAddressFormat = true;
            }
            else if (addressBits.Length >= 2 && IsNumber(addressBits[0]) && line.Contains(" W ") || line.Contains(" E ") ||
                line.Contains(" N ") || line.Contains(" S "))
            {
                matchesAddressFormat = true;
            }
            else if (addressBits[addressBits.Length - 1].StartsWith("STR") && addressBits[addressBits.Length - 1].EndsWith("T") ||
                addressBits[addressBits.Length - 1].StartsWith("AV") && addressBits[addressBits.Length - 1].EndsWith("E"))
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

        static void BadAddressFinder()
        {
            List<string> allSuffixes = GetValidStreetSuffixes();
            List<string> shortenedStreets = GetCleanedNYStreetList(allSuffixes);

            IEnumerable<string> allLines = File.ReadLines("c:/users/ben/desktop/finaldataset.csv");

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

                if (address1.EndsWith("."))
                {
                    address1 = address1.Substring(0, address1.Length - 1);
                }

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

            using (StreamWriter sw = File.CreateText("C:/users/ben/desktop/rejects.txt"))
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

        static float CharacterMatchQuality(string string1, string string2)
        {
            int[] counts = new int[26];

            for (int c = 0; c < string1.Length; c++)
            {
                counts[string1[c] - 65]++;
            }

            for (int c = 0; c < string2.Length; c++)
            {
                counts[string2[c] - 65]++;
            }

            float total = 0, matches = 0;
            for (int c = 0; c < counts.Length; c++)
            {
                int count = counts[c];
                if (count != 0)
                    total++;
                if (count >= 2)
                    matches++;
            }

            return matches / total;
        }

        static void StreetSuffixVariationFinder()
        {
            IEnumerable<string> allLines = File.ReadLines("c:/users/ben/desktop/finaldataset.csv");
            string[] fullSuffixes = File.ReadAllLines("StreetSuffixes.csv").Select(n => n.Split(',')[0]).ToArray();
            string[] shortSuffixes = File.ReadAllLines("StreetSuffixes.csv").Select(n => n.Split(',')[1]).ToArray();

            Dictionary<string, List<string>> allMatches = new Dictionary<string, List<string>>();

            foreach (string suffix in fullSuffixes)
            {
                allMatches.Add(suffix, new List<string>());
            }

            //foreach (string line in allLines)
            int count = 0;
            //Parallel.ForEach(allLines, line =>
            foreach (string line in allLines)
            {
                Interlocked.Increment(ref count);

                if (count % 10000 == 0)
                {
                    Console.WriteLine($"{count}");
                }
                string address1 = DataLoader.SmartSplit(line)[8];
                string[] address1Bits = address1.Split(' ');
                bool exactMatchFound = false, aMatchFound = false;
                string bestSuffixMatch = null, bitMatchedAgainst = null;

                for (int c = address1Bits.Length - 1; c >= 0 && !aMatchFound; c--)
                {
                    string bit = address1Bits[c];

                    if (bit == "NORTH" || bit == "SOUTH" ||
                        bit == "WEST" || bit == "NORTH" ||
                        bit == "N" || bit == "S" ||
                        bit == "W" || bit == "E")
                    {
                        continue;
                    }

                    string bitToUse = bit;
                    Match match = Regex.Match(bit, "([0-9]+)?([A-Z]+)([0-9]+[A-Z]+)?");
                    if (match != null && match.Groups.Count >= 3)
                    {
                        string newValue = match.Groups[2].Value;
                        bitToUse = newValue;
                    }

                    // look for and break on a short suffix. 
                    foreach (string suffix in shortSuffixes)
                    {
                        if (bitToUse == suffix)
                        {
                            exactMatchFound = true;
                            aMatchFound = true;
                            break;
                        }
                    }

                    // look for and break on short suffix or near match. 
                    int lowest = int.MaxValue;
                    foreach (string suffix in fullSuffixes)
                    {
                        int editDistanceLimit = 2;

                        if (suffix.Length <= 5)
                        {
                            editDistanceLimit = 1;
                        }

                        if (bitToUse == suffix)
                        {
                            exactMatchFound = true;
                            aMatchFound = true;
                            break;
                        }
                        else
                        {
                            int editD = EditDistance.Compute(bitToUse, suffix);
                            if (editD <= editDistanceLimit)
                            {
                                if (editD < lowest)
                                {
                                    lowest = editD;
                                    bestSuffixMatch = suffix;
                                    aMatchFound = true;
                                    bitMatchedAgainst = bitToUse;
                                }
                                else if (editD == lowest)
                                {
                                    float bestSoFar = CharacterMatchQuality(bestSuffixMatch, bitMatchedAgainst);
                                    float competitor = CharacterMatchQuality(suffix, bitToUse);

                                    if (competitor > bestSoFar)
                                    {
                                        bestSuffixMatch = suffix;
                                        bitMatchedAgainst = bitToUse;
                                        aMatchFound = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!exactMatchFound && aMatchFound)
                {
                    List<string> lines = allMatches[bestSuffixMatch];
                    lock (lines)
                    {
                        lines.Add(address1 + "," + bitMatchedAgainst);
                    }
                    //if (!allMatches.Contains(bestSuffixMatch))
                    //{
                    //    allMatches.Add(bestSuffixMatch);
                    //}
                }
            }//);

            foreach (string key in allMatches.Keys)
            {
                using (StreamWriter sw = File.CreateText($"C:/users/ben/desktop/files/{key}.txt"))
                {
                    foreach (string line in allMatches[key])
                    {
                        sw.WriteLine(line);
                    }
                }
            }

            Console.Beep();

            //using (StreamWriter fout = File.CreateText("c:/users/ben/desktop/suffixes.txt"))
            //{
            //    foreach (string ending in allMatches)
            //    {
            //        fout.WriteLine(ending);
            //    }
            //}
        }

        static string[] Summarize()
        {
            List<string> ret = new List<string>();

            foreach (string file in Directory.GetFiles(@"C:\Users\Ben\Desktop\files"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file); 
                string[] all = File.ReadAllLines(file);

                List<string> unique = new List<string>();
                foreach (string a in all)
                {
                    string secondPart = a.Split(',')[1];
                    if(!unique.Contains(secondPart))
                    {
                        unique.Add(secondPart); 
                    }
                }
                using (StreamWriter sw2 = File.CreateText($"C:/users/ben/desktop/processed/{fileName}.txt"))
                {
                    foreach(string u in unique)
                    {
                        sw2.WriteLine(u +"," + fileName); 
                    }
                }
            }

            return ret.ToArray();
        }

        static void Main(string[] args)
        {
            //StreetSuffixVariationFinder();
            Summarize();
        }
    }
}
