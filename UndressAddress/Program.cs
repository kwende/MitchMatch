using Common;
using DecisionTreeLearner.NLP;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UndressAddress.DataSetParsers;
using UndressAddress.DataTypes;

namespace UndressAddress
{
    class Program
    {
        static bool IsNumber(string input)
        {
            double tmp;
            return double.TryParse(input, out tmp);
        }
        static bool AddressExists(List<StateOfNewYorkAddressRange> fullAddresses, int houseNumber, string streetName, string suffix)
        {
            bool exists = false;


            return exists;
        }


        static List<string> GetCleanedNYStreetList2()
        {
            //// read from all the necessary files
            Data data = DataLoader.LoadData();
            data.FinalDataSet = data.FinalDataSet.Where(b => b.Contains("2857 SEDWICK AVE")).Take(1).ToArray();
            Random rand = new Random();
            //data.FinalDataSet = data.FinalDataSet.Where(b => rand.Next() % 100 == 0).ToArray();

            // precompute these strings because otherwise we compute them in a for() loop and 
            // string.concat() becomes a wasteful operation. 
            List<string> streetNameSubStrings = new List<string>();
            streetNameSubStrings.AddRange(data.NewYorkStateStreetNames.Select(n => " " + n + " "));
            List<string> streetNameEndsWith = new List<string>();
            streetNameEndsWith.AddRange(data.NewYorkStateStreetNames.Select(n => " " + n));
            List<string> streetNames = new List<string>();
            streetNames.AddRange(data.NewYorkStateStreetNames);

            List<string> perfectMatch = new List<string>();
            List<string> homeless = new List<string>();
            List<string> unknown = new List<string>();
            List<string> adjustmentNeededToStreetName = new List<string>();
            List<string> streetNamePerfectMatch = new List<string>();
            List<string> failed = new List<string>();

            int numberCouldntProcess = 0;

            // counter variables. 
            int iterations = 0;

            List<string> cleanedAddresses = new List<string>();

            DateTime lastTime = DateTime.Now;
            List<double> timeSpans = new List<double>();
            // go over each line in the final data set. 
            //for (int c = 0; c < data.FinalDataSet.Length; c++)
            Parallel.For(0, data.FinalDataSet.Length, c =>
            {
                #region DebuggingOutput
                // debugging purposes. 
                Interlocked.Increment(ref iterations);
                if (iterations % 1000 == 0)
                {
                    DateTime now = DateTime.Now;
                    double millisecondsSinceLast = (now - lastTime).TotalMilliseconds;
                    timeSpans.Add(millisecondsSinceLast);

                    double averageTime = timeSpans.Average();
                    double numberOfChecksLeft = (data.FinalDataSet.Length - iterations) / 1000.0f;

                    double hoursLeft = (averageTime * numberOfChecksLeft) / 1000.0f / 60.0f / 60.0f;

                    if (timeSpans.Count > 100)
                    {
                        timeSpans.RemoveAt(0);
                    }

                    //double percentage = ((fullAddressMatched.Count + homeless.Count + unknown.Count + streetMatched.Count) / (iterations * 1.0)) * 100;
                    double percentage = ((perfectMatch.Count +
                        homeless.Count + unknown.Count +
                        adjustmentNeededToStreetName.Count +
                        streetNamePerfectMatch.Count) / ((iterations * 1.0))) * 100;
                    //Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}: {fullAddressMatched.Count} Full. {streetMatched.Count} Street. {homeless.Count} Homeless. {unknown.Count} Unknown. {couldNotParse.Count} Couln't parse. {notMatched.Count} Not matched. Projected {percentage.ToString("0.00")}% match. {hoursLeft.ToString("0.00")} hours left.");

                    Console.Clear();
                    Console.WriteLine($"Perfect match: {perfectMatch.Count}");
                    Console.WriteLine($"Zip matched, adjustment needed for street name/suffix: {adjustmentNeededToStreetName.Count}");
                    Console.WriteLine($"Zip mismatch, perfect street name: {streetNamePerfectMatch.Count}");
                    Console.WriteLine($"Homeless or Unknown: {homeless.Count + unknown.Count}");
                    Console.WriteLine("========SUMMARY=======");
                    Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}: {percentage.ToString("0.00")}% matched. {hoursLeft.ToString("0.00")} hours left.");

                    lastTime = now;
                }
                #endregion

                // clean the address and do what we can with pure NLP. 
                Address address = AddressUtility.InitializeAddress(data.FinalDataSet[c], data);

                if (address.MatchQuality == MatchQuality.MatchNotYetDetermined)
                {
                    bool matched = false;

                    if (address.POBoxNumber != 0)
                    {
                        matched = true;
                        perfectMatch.Add(AddressUtility.CreateLineFromAddress(address));
                    }
                    else
                    {
                        // check for known buildings. 
                        address = AddressUtility.CheckForBuildingsAndCenters(address, data);

                        if (address.MatchQuality == MatchQuality.Alternate)
                        {
                            perfectMatch.Add(AddressUtility.CreateLineFromAddress(address));
                            matched = true;
                        }
                        else
                        {
                            // check for exact match by street name and zip. 
                            // do nothing to it but just a blind check. 
                            if (address.Zip.HasValue)
                            {
                                string streetNameToMatch = address.StreetName;

                                // find all streets which this street name is a substring. 
                                // this is like a slightly fuzzier equals. 
                                List<string> toInspects = new List<string>();
                                foreach (string streetNameInDatabase in data.NewYorkStateStreetNames)
                                {
                                    if (StringUtility.Contains(streetNameInDatabase, streetNameToMatch))
                                    {
                                        toInspects.Add(streetNameInDatabase);
                                    }
                                }

                                // go through those streets
                                foreach (string toInspect in toInspects)
                                {
                                    // do we have a ZIP
                                    if (data.StreetNamesToZips.ContainsKey(toInspect))
                                    {
                                        if (data.StreetNamesToZips[toInspect].Contains(address.Zip.Value))
                                        {
                                            lock (perfectMatch)
                                            {
                                                perfectMatch.Add(address.OriginalLine);
                                                matched = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                            }

                            // assume zip is right, but the street name is a bit wonky. 
                            if (!matched && address.Zip.HasValue)
                            {
                                List<string> alternativeStreetNames = BKTreeEngine.LeastEditDistance(address.StreetName, data.BKTree).Distinct().ToList();
                                foreach (string alternativeStreetName in alternativeStreetNames)
                                {
                                    if (data.AlternateSuffixList.ContainsKey(alternativeStreetName))
                                    {
                                        List<string> alternateSuffixes = data.AlternateSuffixList[alternativeStreetName];
                                        foreach (string alternateSuffix in alternateSuffixes)
                                        {
                                            string streetWithAlternateSuffix = alternativeStreetName + " " +
                                                alternateSuffix;

                                            if (data.StreetNamesToZips.ContainsKey(streetWithAlternateSuffix))
                                            {
                                                List<int> zips = data.StreetNamesToZips[streetWithAlternateSuffix];
                                                if (zips.Contains(address.Zip.Value))
                                                {
                                                    matched = true;
                                                    lock (adjustmentNeededToStreetName)
                                                    {
                                                        adjustmentNeededToStreetName.Add(address.OriginalLine);
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                }
                            }

                            // street name perfect match, zip code is wonky. 
                            if (!matched && !string.IsNullOrEmpty(address.StreetName))
                            {
                                string streetNameToSearch = (address.StreetName + " " + address.Suffix).Trim();
                                if (data.NewYorkStateStreetNames.Contains(streetNameToSearch))
                                {
                                    lock (streetNamePerfectMatch)
                                    {
                                        streetNamePerfectMatch.Add(AddressUtility.CreateLineFromAddress(address));
                                    }
                                    matched = true;
                                }
                            }
                        }
                    }

                    if (!matched)
                    {
                        lock (failed)
                        {
                            failed.Add(address.RawAddress1);
                        }
                    }

                }
                else if (address.MatchQuality == MatchQuality.Unknown)
                {
                    lock (unknown)
                    {
                        unknown.Add(AddressUtility.CreateLineFromAddress(address, "UNKNOWN"));
                    }
                }
                else if (address.MatchQuality == MatchQuality.Homeless)
                {
                    lock (homeless)
                    {
                        homeless.Add(AddressUtility.CreateLineFromAddress(address, "HOMELESS"));
                    }
                }
            });

            using (StreamWriter fout = File.CreateText("perfectMatch.txt"))
            {
                for (int c = 0; c < perfectMatch.Count; c++)
                {
                    fout.WriteLine(perfectMatch[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("failed.txt"))
            {
                for (int c = 0; c < failed.Count; c++)
                {
                    fout.WriteLine(failed[c]);
                }
            }

            Console.WriteLine($"Couldn't process {numberCouldntProcess}");
            Console.ReadLine();

            return null;
        }

        static Matches FuzzyMatchStreetNames(string[] rows, string[] truth)
        {
            return FastEditDistanceGrouper.EditDistanceAtMostN(rows, truth, 1);
        }

        static Matches FuzzyMatchStreetNames2(string[] rows, string[] truth)
        {
            return FastBKTreeGrouper.EditDistanceAtMostN(rows, truth, 1);
        }

        static bool CompareTrees(BKTree tree1, BKTree tree2)
        {
            if (tree1 == null || tree2 == null)
            {
                return tree1 == tree2;
            }
            else if (tree1.Index == tree2.Index &&
                tree1.StringValue == tree2.StringValue &&
                tree1.Children.Length == tree2.Children.Length)
            {
                for (int c = 0; c < tree1.Children.Length; c++)
                {
                    if (!CompareTrees(tree1.Children[c], tree2.Children[c]))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        static void CreateBKTree(string outputSerializedPath)
        {
            List<string> allLines = File.ReadAllLines("c:/users/brush/desktop/allStreets.csv").ToList();

            Data data = DataLoader.LoadData();

            //List<string> allCorrectedStrings = new List<string>();
            //string[] allLines = File.ReadAllLines("C:/users/brush/desktop/allStreets.csv");

            //Dictionary<string, List<string>> streetNamesWithMultipleSuffixes = new Dictionary<string, List<string>>();

            List<string> justNames = new List<string>();
            foreach (string line in allLines)
            {
                string[] parts = line.Trim().Split(' ');

                string suffix = "";
                int index = -1;
                if ((index = data.Suffixes.IndexOf(parts[parts.Length - 1])) != -1)
                {
                    justNames.Add(string.Join(" ", parts.Take(parts.Length - 1)));
                }
                else
                {
                    justNames.Add(line);
                }
            }

            BKTree tree = BKTreeEngine.CreateBKTree(justNames);
            BKTreeSerializer.SerializeTo(tree, outputSerializedPath);
        }

        static void BuildDictionary(string outputSerializedPath)
        {
            Data data = DataLoader.LoadData();

            string[] stateWide = File.ReadAllLines(@"C:\Users\brush\Downloads\openaddr-collected-us_northeast\us\ny\statewide.csv").Select(n => n.ToUpper()).ToArray();
            string[] newYork = File.ReadAllLines(@"D:\repos\MitchMatch\UndressAddress\city_of_new_york.csv").Select(n => n.ToUpper()).ToArray();

            List<string[]> allLines = new List<string[]>();
            allLines.AddRange(stateWide.Skip(1).Select(n => DecisionTreeLearner.Data.DataLoader.SmartSplit(n.ToUpper())));
            allLines.AddRange(newYork.Skip(1).Select(n => DecisionTreeLearner.Data.DataLoader.SmartSplit(n.ToUpper())));

            Dictionary<string, List<int>> street2Zip = new Dictionary<string, List<int>>();

            int count = 0;
            Parallel.ForEach(allLines, line =>
            {
                Interlocked.Increment(ref count);

                if (count % 100000 == 0)
                {
                    Console.WriteLine($"{count}/{allLines.Count}");
                }

                if (line.Length == 11)
                {
                    string streetName = line[3];
                    string[] streetNameParts = streetName.Split(' ');
                    string possibleSuffix = streetNameParts[streetNameParts.Length - 1];

                    for (int c = 0; c < data.Suffixes.ShortSuffixes.Length; c++)
                    {
                        if (data.Suffixes.ShortSuffixes[c] == possibleSuffix)
                        {
                            streetName = string.Join(" ", streetNameParts.Take(streetNameParts.Length - 1)) + " " +
                                data.Suffixes.LongSuffixes[c];
                            break;
                        }
                    }

                    lock (street2Zip)
                    {
                        streetName = Regex.Replace(streetName, "( +)", " ");
                        streetName = Regex.Replace(streetName, "^N ", "NORTH ");
                        streetName = Regex.Replace(streetName, "^S ", "SOUTH ");
                        streetName = Regex.Replace(streetName, "^E ", "EAST ");
                        streetName = Regex.Replace(streetName, "^W ", "WEST ");
                        streetName = Regex.Replace(streetName, " N$", " NORTH");
                        streetName = Regex.Replace(streetName, " S$", " SOUTH");
                        streetName = Regex.Replace(streetName, " E$", " EAST");
                        streetName = Regex.Replace(streetName, " W$", " WEST");

                        streetName = Regex.Replace(streetName, " RD ", " ROAD ");
                        streetName = Regex.Replace(streetName, " AVE ", " AVENUE ");
                        streetName = Regex.Replace(streetName, "^AVE ", "AVENUE ");
                        streetName = Regex.Replace(streetName, "^BCH ", "BEACH ");
                        streetName = streetName.Replace("GRAND CONC", "GRAND CONCOURSE");

                        if (!string.IsNullOrEmpty(streetName))
                        {
                            if (!street2Zip.ContainsKey(streetName))
                            {
                                street2Zip.Add(streetName, new List<int>());
                            }

                            int zip = 0;
                            if (int.TryParse(line[8], out zip))
                            {
                                street2Zip[streetName].Add(zip);
                            }
                        }
                    }
                }
            });

            int numberWithNoZipCodes = 0;
            string[] keys = street2Zip.Keys.ToArray();
            for (int c = 0; c < keys.Length; c++)
            {
                street2Zip[keys[c]] = street2Zip[keys[c]].Distinct().ToList();

                if (street2Zip[keys[c]].Count == 0)
                {
                    numberWithNoZipCodes++;
                }
            }

            using (StreamWriter sw = File.CreateText("c:/users/brush/desktop/allStreets.csv"))
            {
                foreach (string key in keys)
                {
                    sw.WriteLine(key);
                }
            }

            Console.WriteLine("Number of streets: " + keys.Length.ToString());
            Console.WriteLine("Number with no zip codes: " + numberWithNoZipCodes.ToString());

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fout = File.Create("c:/users/brush/desktop/farts.dat"))
            {
                bf.Serialize(fout, street2Zip);
            }
        }

        static void Main(string[] args)
        {
            StreetSegmentParser.Parse("D:/streetSegment.csv");

            //BuildDictionary("");

            //BuildDictionary("c:/users/brush/desktop/streetZipLookup.dat");

            //CreateBKTree("C:/users/brush/desktop/bkTree.dat");

            //GetCleanedNYStreetList2();

            //Data data = DataLoader.LoadData();

            //List<string> allCorrectedStrings = new List<string>();
            //string[] allLines = File.ReadAllLines("C:/users/brush/desktop/allStreets.csv");

            //Dictionary<string, List<string>> streetNamesWithMultipleSuffixes = new Dictionary<string, List<string>>();

            //foreach (string line in allLines)
            //{
            //    string[] parts = line.Trim().Split(' ');

            //    string suffix = "";
            //    int index = -1;
            //    if ((index = data.Suffixes.IndexOf(parts[parts.Length - 1])) != -1)
            //    {
            //        suffix = data.Suffixes.LongSuffixes[index];

            //        string streetNameWithoutSuffix = string.Join(" ", parts.Take(parts.Length - 1).ToArray());
            //        if (!streetNamesWithMultipleSuffixes.ContainsKey(streetNameWithoutSuffix))
            //        {
            //            streetNamesWithMultipleSuffixes.Add(streetNameWithoutSuffix, new List<string>());
            //        }

            //        streetNamesWithMultipleSuffixes[streetNameWithoutSuffix].Add(suffix);
            //    }
            //}

            ////IEnumerable<string> uniqueCorrected = allCorrectedStrings.Distinct();

            ////File.WriteAllLines("C:/users/brush/desktop/uniqueStreetsCorrected.txt", uniqueCorrected);

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/streetToSuffixTable.csv"))
            //{
            //    foreach (KeyValuePair<string, List<string>> pairs in streetNamesWithMultipleSuffixes)
            //    {
            //        //if (pairs.Value.Count > 1)
            //        {
            //            string ending = string.Join(",", pairs.Value.ToArray());

            //            sw.WriteLine($"{pairs.Key}:{ending}");
            //        }
            //    }
            //}


            return;
        }
    }
}
