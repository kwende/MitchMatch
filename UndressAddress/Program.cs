using DecisionTreeLearner.Data;
using DecisionTreeLearner.NLP;
using System;
using System.Collections.Generic;
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

        static string CleanAddress(string inputAddress1)
        {
            // replace periods at end of the address
            if (inputAddress1.EndsWith("."))
            {
                inputAddress1 = inputAddress1.Substring(0, inputAddress1.Length - 1);
            }

            // remove the suffix at the end of 1st or 2nd or 3rd, etc. 
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d)(ST|ND|RD|TH)\b", "$1");
            inputAddress1 = Regex.Replace(inputAddress1, @" (\d+) (ST|ND|RD|TH) ", " $1 ");

            // separate the East/West portion from the street number. 
            // Ex: "460 E46 STREET"
            inputAddress1 = Regex.Replace(inputAddress1, @"(\d+) (N|S|W|E)(\d+) \b", "$1 $2 $3 ");

            // split up addresses that are stuck together. 
            // Ex: 543 W180THST
            inputAddress1 = Regex.Replace(inputAddress1, @"(E|W|S|N)(\d+)(ST|ND|RD|TH)(.+)", "$1 $2 $4");

            // replace N/S/E/W with the appropriate cardinal if put at end. 
            // Ex: "31 W MOSHOLU PRKWY N"
            inputAddress1 = Regex.Replace(inputAddress1, @" (N)$", " NORTH");
            inputAddress1 = Regex.Replace(inputAddress1, @" (S)$", " SOUTH");
            inputAddress1 = Regex.Replace(inputAddress1, @" (E)$", " EAST");
            inputAddress1 = Regex.Replace(inputAddress1, @" (W)$", " WEST");

            // split apart the N/S/E/W if catenated to number. 
            // Ex: 219E 121
            inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+)(N|S|E|W) ", "$1 $2");

            // put street at the end if it's just a number at the end. 
            // Ex: 360 E 193
            inputAddress1 = Regex.Replace(inputAddress1, @"^(\d+) (.+) (\d+)$", "$1 $2 $3 STREET");

            // Replace WEST132 with WEST 132
            inputAddress1 = Regex.Replace(inputAddress1, @" (WEST|NORTH|SOUTH|EAST)(\d+) ", " $1 $2 ");

            // Replace "FTJOHN" with "FORT JOHN"
            inputAddress1 = Regex.Replace(inputAddress1, @" (FT)([A-Z]+) ", " FORT $2 ");

            // Replace "1387 STJOHNS PL" with "1387 ST JOHNS PL"
            inputAddress1 = Regex.Replace(inputAddress1, @" (ST)([BCDFGHJKLMNPQSTVWXZ])([A-Z]+) ", " $1 $2$3 ");

            return inputAddress1;
        }

        static List<string> GetCleanedNYStreetList2()
        {
            // read from all the necessary files. 
            string[] streetSuffixLines = File.ReadAllLines("StreetSuffixes.csv");
            string[] finalDataSetLines = File.ReadAllLines("c:/users/brush/desktop/finaldataset.csv");
            string[] newYorkStateStreetLines = File.ReadAllLines("NewYorkStateStreets.csv").Skip(1).ToArray();

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

            // go over each line in the final data set. 
            Parallel.For(1, finalDataSetLines.Length, c =>
            {
                // debugging purposes. 
                Interlocked.Increment(ref iterations);
                if (iterations % 1000 == 0)
                {
                    Console.WriteLine($"{iterations}/{finalDataSetLines.Length}:{exactMatches}:{editDistanceMatches}");
                }

                // precleaning. 
                string[] parts = DataLoader.SmartSplit(finalDataSetLines[c]);
                string address1 = CleanAddress(parts[8]);


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


                                noMatchButFormatSeemsGood = true;
                            }
                        }
                    }

                    if (exactMatchFound)
                    {
                        exactMatchesFound.Add($"{address1} => {matched}");
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
            });

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

        static void FindStreetNameWithEditDistanceLessThanOne()
        {
            string[] streetNames = File.ReadAllLines("c:/users/brush/desktop/untouched.txt");
            string[] newYorkStateStreetNames = File.ReadAllLines("NewYorkStateStreets.csv");

            Dictionary<string, List<string>> replacements = new Dictionary<string, List<string>>();
            //foreach (string streetName in streetNames)

            int counter = 0;
            Parallel.ForEach(streetNames, streetName =>
            {
                Match match = Regex.Match(streetName, @"'(\d+) ([A-Z]+) ([A-Z]+)'");

                if (match != null && match.Groups.Count == 4)
                {
                    streetName = match.Groups[2].Value;
                    //streetName = match.Groups[]

                    Interlocked.Increment(ref counter);

                    if (counter % 100 == 0)
                    {
                        Console.WriteLine($"{counter} / {streetNames.Length}");
                    }

                    List<string> alternates = null;

                    lock (replacements)
                    {
                        if (replacements.ContainsKey(streetName))
                        {
                            alternates = replacements[streetName];
                        }
                        else
                        {
                            alternates = new List<string>();
                            replacements.Add(streetName, alternates);
                        }
                    }


                    foreach (string newYorkStateStreeName in newYorkStateStreetNames)
                    {
                        if (streetName.Length >= 7)
                        {
                            int distance = EditDistance.Compute(streetName, newYorkStateStreeName);
                            if (distance == 1)
                            {
                                alternates.Add(newYorkStateStreeName);
                            }
                        }
                    }
                }
            });

            using (StreamWriter sw = File.CreateText("c:/users/brush/desktop/alternates.txt"))
            {
                foreach (string key in replacements.Keys)
                {
                    List<string> alternates = replacements[key];

                    sw.WriteLine(key + ":" + string.Join(" && ", alternates));
                }
            }
        }

        static void Main(string[] args)
        {
            ////string cleaned = CleanAddress("1387 STJOHNS PL");
            ////return; 

            //string[] allAddresses = File.ReadAllLines("c:/users/brush/desktop/notmatchedButFormatIsGood.txt");

            //using (StreamWriter untouched = File.CreateText("c:/users/brush/desktop/untouched.txt"))
            //{
            //    foreach (string line in allAddresses)
            //    {
            //        string changed = CleanAddress(line);

            //        if (changed == line)
            //        {
            //            untouched.WriteLine($"'{line}'");
            //        }
            //    }
            //}
            //using (StreamWriter fout = File.CreateText("c:/users/brush/desktop/changed.txt"))
            //{
            //    foreach (string line in allAddresses)
            //    {
            //        string changed = CleanAddress(line);

            //        if (changed != line)
            //        {
            //            fout.WriteLine($"'{line}' => '{changed}'");
            //        }
            //    }
            //}

            //StreetSuffixVariationFinder();
            //Summarize();
            //ReplacementCount();
            //HowManyMatchNewYorkDatabase();
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //GetCleanedNYStreetList2();
            //sw.Stop();

            FindStreetNameWithEditDistanceLessThanOne();

            //Console.WriteLine($"The process took {sw.ElapsedMilliseconds / 1000.0f / 60.0f} minutes");

            //GetCleanedCities();
        }
    }
}
