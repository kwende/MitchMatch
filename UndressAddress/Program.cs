using DecisionTreeLearner.Data;
using DecisionTreeLearner.NLP;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
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
        static string CleanAddress(string inputAddress1, List<NewYorkCityAddress> fullAddresses)
        {
            inputAddress1 = inputAddress1.Replace("  ", " ");

            // replace periods at end of the address
            if (inputAddress1.EndsWith("."))
            {
                inputAddress1 = inputAddress1.Substring(0, inputAddress1.Length - 1);
            }

            // remove the suffix at the end of 1st or 2nd or 3rd, etc. 
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+)(ST|ND|RD|TH)", "$1");
            inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (ST|ND|RD|TH) ", " $1 ");

            // separate the East/West portion from the street number. 
            // Ex: "460 E46 STREET"
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (N|S|W|E)(\d+) \b", "$1 $2 $3 ");

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

            // Remove the period from end of numbers. 
            // 2920 W 21. ST
            inputAddress1 = Regex.Replace(inputAddress1, @" (\d+)(\.) ", " $1 ");

            // Remove RST from ending of numbers. 
            inputAddress1 = Regex.Replace(inputAddress1, @" (\d+)(RST) ", " $1 ");

            // BKLYN to BROOKLYN
            inputAddress1.Replace(" BKLYN ", " BROOKLYN ");

            Match streetWithPossibleCardinal = Regex.Match(inputAddress1, @"(\d+) ([A-Z]+) (\d+) ([A-Z]+)");
            if (streetWithPossibleCardinal.Groups.Count == 5)
            {
                string possibleCardinal = streetWithPossibleCardinal.Groups[2].Value;
                if (EditDistance.Compute(possibleCardinal, "WEST") == 1)
                {
                    return inputAddress1;
                }
                if (EditDistance.Compute(possibleCardinal, "EAST") == 1)
                {
                    return inputAddress1;
                }
                if (EditDistance.Compute(possibleCardinal, "NORTH") == 1)
                {
                    return inputAddress1;
                }
                if (EditDistance.Compute(possibleCardinal, "SOUTH") == 1)
                {
                    return inputAddress1;
                }
            }

            return inputAddress1;
        }

        static bool ConfirmAddress(string addressLine)
        {
            return false;
        }

        static List<string> GetCleanedNYStreetList2()
        {
            // read from all the necessary files
            string[] streetSuffixLines = File.ReadAllLines("StreetSuffixes.csv");
            string[] finalDataSetLines = File.ReadAllLines("c:/users/ben/desktop/finaldataset.csv").Where(n => n.Contains(" EST ")).ToArray();
            //string[] finalDataSetLines = File.ReadAllLines("c:/users/brush/desktop/notmatchedButFormatIsGood2.txt");
            string[] newYorkStateStreetLines = File.ReadAllLines("allStreets.csv").Select(n => n.Split(',')[0].Trim()).ToArray();

            List<NewYorkCityAddress> fullAddresses = new List<NewYorkCityAddress>();

            IEnumerable<string> linesInNewYorkAddresses = File.ReadLines("c:/users/ben/desktop/city_of_new_york.csv").Skip(1);

            foreach (string lineInNewYorkAddress in linesInNewYorkAddresses)
            {
                string[] bits = lineInNewYorkAddress.Split(',').Select(n => n.Trim()).ToArray();

                NewYorkCityAddress address = new NewYorkCityAddress
                {
                    BuildingNumber = bits[2],
                    StreetName = bits[3],
                    ZipCode = bits[8]
                };
                fullAddresses.Add(address);
            }

            // process the suffixes into long and short 
            string[] shortSuffixes = streetSuffixLines.Select(n => n.Split(',')[1]).ToArray();
            string[] longSuffixes = streetSuffixLines.Select(n => n.Split(',')[0]).ToArray();

            // add all the streetNames
            List<string> uniques = new List<string>();
            uniques.AddRange(newYorkStateStreetLines);

            // go through and identify each street with a long suffix. 
            // add to it the corresponding short suffix. 
            Parallel.ForEach(newYorkStateStreetLines, newYorkCityStreetLine =>
            {
                for (int c = 0; c < longSuffixes.Length; c++)
                {
                    string longSuffix = longSuffixes[c];
                    if (newYorkCityStreetLine.EndsWith(" " + longSuffix))
                    {
                        int index = newYorkCityStreetLine.LastIndexOf(" " + longSuffix);
                        string shortened = newYorkCityStreetLine.Substring(0, index) + " " + shortSuffixes[c];

                        if (shortened != newYorkCityStreetLine)
                        {
                            lock (uniques)
                            {
                                uniques.Add(shortened);
                            }
                        }
                    }
                }
            });

            // create lists to store exact, no match and near matches. 
            List<string> exactMatchesFound = new List<string>();
            List<string> notMatched = new List<string>();
            List<string> notMatchedButFormatIsGood = new List<string>();

            // counter variables. 
            int exactMatches = 0;
            int editDistanceMatches = 0;
            int nonZeroAddress1 = 0;
            int iterations = 0;

            DateTime lastTime = DateTime.Now;
            List<double> timeSpans = new List<double>();
            // go over each line in the final data set. 
            for (int c = 0; c < finalDataSetLines.Length; c++)
            //Parallel.For(1, finalDataSetLines.Length, c =>
            {
                // debugging purposes. 
                Interlocked.Increment(ref iterations);
                if (iterations % 1000 == 0)
                {
                    DateTime now = DateTime.Now;
                    double millisecondsSinceLast = (now - lastTime).TotalMilliseconds;
                    timeSpans.Add(millisecondsSinceLast);

                    double averageTimeFor1000 = timeSpans.Average();
                    double numberOf1000sLeft = (finalDataSetLines.Length - iterations) / 1000.0f;

                    double hoursLeft = (averageTimeFor1000 * numberOf1000sLeft) / 1000.0f / 60.0f / 60.0f;

                    if (timeSpans.Count > 100)
                    {
                        timeSpans.RemoveAt(0);
                    }

                    double percentage = (exactMatches / (iterations * 1.0)) * 100;

                    Console.WriteLine($"{iterations}/{finalDataSetLines.Length}:{exactMatches}. Projected {percentage.ToString("0.00")}% match. {hoursLeft.ToString("0.00")} hours left.");

                    lastTime = now;
                }

                // precleaning. 
                string[] parts = DataLoader.SmartSplit(finalDataSetLines[c]);
                //parts[8] = "219 E121 ST";
                string address1Raw = parts[8];
                string address1 = CleanAddress(address1Raw, fullAddresses);

                // is there an address left? 
                if (address1.Length != 0)
                {
                    // look for matches. 
                    Interlocked.Increment(ref nonZeroAddress1);
                    bool exactMatchFound = false, noMatchButFormatSeemsGood = false;
                    string matched = "";

                    // go through all of the uniques and find the 
                    // longest match that matches. 
                    foreach (string unique in uniques)
                    {
                        if ((address1 == unique ||
                            address1.Contains(" " + unique + " ") ||
                            address1.EndsWith(" " + unique)) && unique.Length > matched.Length)
                        {
                            exactMatchFound = true;
                            matched = unique;
                        }
                    }

                    // if no match found, is the format of this line at least sane? 
                    if (!exactMatchFound)
                    {
                        string[] address1Parts = address1.Split(' ');
                        if (address1Parts.Length >= 3)
                        {
                            string firstPart = address1Parts[0];
                            string lastPart = address1Parts[address1Parts.Length - 1];

                            if (IsNumber(firstPart) &&
                                (shortSuffixes.Contains(lastPart) || longSuffixes.Contains(lastPart)))
                            {
                                string alternative = FindBestMatchedStreetNameWithinEditDistance(
                                    address1, shortSuffixes, longSuffixes, newYorkStateStreetLines);

                                if (!string.IsNullOrEmpty(alternative))
                                {
                                    exactMatchFound = true;
                                    matched = alternative;
                                }
                                else
                                {
                                    noMatchButFormatSeemsGood = true;
                                }
                            }
                        }
                    }

                    if (exactMatchFound)
                    {
                        exactMatchesFound.Add($"{address1Raw} => {matched}");
                        exactMatches++;
                    }
                    else if (noMatchButFormatSeemsGood)
                    {
                        notMatchedButFormatIsGood.Add(address1);
                    }
                    else
                    {
                        notMatched.Add(address1);
                    }
                }
            }//);

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/matched.txt"))
            {
                for (int c = 0; c < exactMatchesFound.Count; c++)
                {
                    fout.WriteLine(exactMatchesFound[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/notmatchedButFormatIsGood.txt"))
            {
                for (int c = 0; c < notMatchedButFormatIsGood.Count; c++)
                {
                    fout.WriteLine(notMatchedButFormatIsGood[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/notmatched.txt"))
            {
                for (int c = 0; c < notMatched.Count; c++)
                {
                    fout.WriteLine(notMatched[c]);
                }
            }

            Console.WriteLine($"Exact matches: {exactMatches}/{nonZeroAddress1}");

            return null;
        }
        static string FindBestMatchedStreetNameWithinEditDistance(string streetName,
            string[] shortSuffixes, string[] longSuffixes, string[] newYorkStateStreetNames)
        {
            string ret = "";

            // right now it simply returns the first street which is within edit distance
            // and so it breaks out early. later might consider a bet heuristic on top of edit distance 
            // to break ties. 
            const int MaximumLengthOfPortionToExamineForEditDistance = 5;

            Match match = Regex.Match(streetName.Replace("'", ""), @"(\d+) ([A-Z ]+) ([A-Z]+)");
            if (match.Groups[2].Value.Length >= MaximumLengthOfPortionToExamineForEditDistance)
            {
                if (match != null && match.Groups.Count == 4)
                {
                    string portionToExamine = match.Groups[2].Value;
                    string possibleSuffix = match.Groups[3].Value;

                    // look at the possible suffix. if it's a short variant, replace with long variant. 
                    bool correctedSuffix = false;
                    for (int c = 0; c < shortSuffixes.Length; c++)
                    {
                        if (shortSuffixes[c] == possibleSuffix || longSuffixes[c] == possibleSuffix)
                        {
                            portionToExamine += " " + longSuffixes[c];
                            correctedSuffix = true;
                            break;
                        }
                    }

                    // if we didn't auto correct the suffix, then just glom it back on. 
                    // it was either a part of the street name or was already a long suffix
                    // or some other variation. 
                    if (!correctedSuffix)
                    {
                        portionToExamine += " " + possibleSuffix;
                    }

                    // look for the closest possible match
                    foreach (string newYorkStateStreeName in newYorkStateStreetNames)
                    {
                        // limit to the portion without the suffix. 
                        int distance = EditDistance.Compute(portionToExamine, newYorkStateStreeName);
                        if (distance == 1)
                        {
                            // match found. replace "portion to example" above (which is group 2 + 3). 
                            ret = match.Groups[1] + " " + newYorkStateStreeName;
                            break;
                        }
                        else if (distance == 2)
                        {
                            // are letters/numbers just permuted? 
                            string longestString = "", shortestString = "";

                            // find the longest string. 
                            if (portionToExamine.Length > newYorkStateStreeName.Length)
                            {
                                longestString = portionToExamine;
                                shortestString = newYorkStateStreeName;
                            }
                            else
                            {
                                longestString = newYorkStateStreeName;
                                shortestString = portionToExamine;
                            }

                            // examine each character of the longest string. 
                            bool justPermutation = false;
                            for (int c = 0; c < longestString.Length; c++)
                            {
                                char toFind = longestString[c];
                                bool found = false;
                                // does it exist in the shortest string? 
                                for (int d = 0; d < shortestString.Length; d++)
                                {
                                    if (shortestString[d] == longestString[c])
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    // if it doesn't exist, then this isn't acceptable. 
                                    justPermutation = false;
                                    break;
                                }
                            }

                            // if letters/numbers are just shuffled around, then be okay with distance 2. 
                            if (justPermutation)
                            {
                                ret = match.Groups[1] + " " + newYorkStateStreeName;
                                break;
                            }
                        }
                    }
                }
            }


            return ret;
        }

        static void Main(string[] args)
        {
            GetCleanedNYStreetList2();
        }
    }
}
