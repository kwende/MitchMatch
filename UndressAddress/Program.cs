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

        private static string CorrectAddress(Data data, ref Address address, string correctedString)
        {
            Address correctStreet = AddressUtility.NormalizeSuffix(correctedString, data);
            address.StreetName = correctStreet.StreetName;
            address.Suffix = correctStreet.Suffix;
            address.MatchQuality = MatchQuality.Alternate;
            return $"{address.StreetName} {address.Suffix}";

        }

        private static string LucasAddressMatch(Address address, Data data)
        {
            string matched = "";
            bool matchFound = (address.MatchQuality != MatchQuality.Alternate);// THIS HAS CHANGED!!!

            List<string> closestNeighbors;

            if (!matchFound)
            {
                // Buildings
                address = AddressUtility.CheckForBuildingsAndCenters(address, data);
                matchFound = address.MatchQuality == MatchQuality.Alternate; // THIS HAS CHANGED!!!
            }
            #region DONT TAKE THESE
            //if (!matchFound)
            //{
            //    // Exact street match
            //    closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName, data.BKTree);

            //    if (EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) == 0)
            //    {
            //        matched = CorrectAddress(data, ref address, closestNeighbors[0]);
            //        matchFound = true;
            //    }
            //}
            //if (!matchFound)
            //{
            //    // Search by ZIP
            //    if (address.Zip != null && !string.IsNullOrEmpty(address.StreetName) && !string.IsNullOrEmpty(address.StreetNumber))
            //    {
            //        StateOfNewYorkAddressRange[] streetsWithZip = data.NYCityStreets.Where(n => n.StreetNumber.IsInRange(address.StreetNumber) && n.ZipCode == address.Zip.Value).ToArray();

            //        List<string> streetsWithZipStrings = streetsWithZip.Select(s => s.FullStreetName).Distinct().ToList();
            //        BKTree bkTreeLocal = BKTreeEngine.CreateBKTree(streetsWithZipStrings);

            //        closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName, bkTreeLocal);

            //        if (closestNeighbors.Count == 1 && EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) <= 2)
            //        {
            //            matched = CorrectAddress(data, ref address, closestNeighbors[0]);
            //            matchFound = true;
            //        }
            //    }
            //}

            //if (!matchFound)
            //{
            //    // Exact street + ST match
            //    closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName + " ST", data.BKTree);

            //    if (EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) == 0)
            //    {
            //        matched = CorrectAddress(data, ref address, closestNeighbors[0]);
            //        matchFound = true;
            //    }
            //}
            //if (!matchFound)
            //{
            //    // Exact street + AVE match
            //    closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName + " AVE", data.BKTree);

            //    if (EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) == 0)
            //    {
            //        matched = CorrectAddress(data, ref address, closestNeighbors[0]);
            //        matchFound = true;
            //    }
            //}
            #endregion

            #region TAKE THESE (modified to fix edit distance and treat numbers better

            if (!matchFound)
            {
                // Approximate street match
                closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName, data.BKTree);

                if (EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) <= 2)
                {
                    matched = CorrectAddress(data, ref address, closestNeighbors[0]);
                    matchFound = true;
                }
            }
            if (!matchFound)
            {
                // Approximate street + ST match
                closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName + " ST", data.BKTree);

                if (EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) <= 2)
                {
                    matched = CorrectAddress(data, ref address, closestNeighbors[0]);
                    matchFound = true;
                }
            }
            if (!matchFound)
            {
                // Approximate street + AVE match
                closestNeighbors = BKTreeEngine.LeastEditDistance(address.FullStreetName + " AVE", data.BKTree);

                if (EditDistanceEngine.Compute(address.FullStreetName, closestNeighbors[0]) <= 2)
                {
                    matched = CorrectAddress(data, ref address, closestNeighbors[0]);
                    matchFound = true;
                }
            }

            #endregion

            // Debug
            if (!matchFound)
            {
                if (false)
                {
                    string addressRaw = $"{address.RawAddress1} / {address.RawAddress2}";
                    string addressCleaned = $"{ address.StreetNumber } / { address.StreetName} / { address.Suffix}";
                    if (!string.IsNullOrEmpty(address.ApartmentNumber))
                    {
                        addressCleaned += $" / {address.ApartmentNumber}";
                    }
                    string closestNeighborsConcatenated = string.Join(" OR ", BKTreeEngine.LeastEditDistance(address.FullStreetName, data.BKTree));

                    Console.WriteLine($"{addressRaw} => {addressCleaned} => {closestNeighborsConcatenated}");
                }
            }

            return matched;
        }

        static List<string> GetCleanedNYStreetList2()
        {
            Console.WriteLine("Loading data...");
            //// read from all the necessary files
            Data data = DataLoader.LoadData(regenerateBKTree: true);
            //data.FinalDataSet = data.FinalDataSet.Where(b => rand.Next() % 100 == 0).ToArray();


            Console.WriteLine("Data loaded.");

            List<string> alternate = new List<string>();
            List<string> leaveAlone = new List<string>();
            List<string> matchNotYetDetermined = new List<string>();
            List<string> perfectMatch = new List<string>();
            List<string> homeless = new List<string>();
            List<string> unknown = new List<string>();
            List<string> adjustmentNeededToStreetName = new List<string>();
            List<string> streetNamePerfectMatch = new List<string>();
            List<string> failed = new List<string>();

            int numberCouldntProcess = 0;

            // counter variables. 
            int iterations = 0;

            Address[] cleanedAddresses = new Address[data.FinalDataSet.Length];

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
                        //matched = LucasAddressMatch(address, data);
                        // check for known buildings. 
                        address = AddressUtility.CheckForBuildingsAndCenters(address, data);

                        if (address.MatchQuality == MatchQuality.Alternate)
                        {
                            perfectMatch.Add(AddressUtility.CreateLineFromAddress(address));
                            matched = true;
                        }
                        else
                        {
                            //// check for exact match by street name and zip. 
                            //// do nothing to it but just a blind check. 
                            //if (address.Zip.HasValue)
                            //{
                            //    string streetNameToMatch = address.StreetName;

                            //    // find all streets which this street name is a substring. 
                            //    // this is like a slightly fuzzier equals. 
                            //    List<string> toInspects = new List<string>();
                            //    foreach (string streetNameInDatabase in data.NYStateStreets)
                            //    {
                            //        if (StringUtility.Contains(streetNameInDatabase, streetNameToMatch))
                            //        {
                            //            toInspects.Add(streetNameInDatabase);
                            //        }
                            //    }

                            //    // go through those streets
                            //    foreach (string toInspect in toInspects)
                            //    {
                            //        // do we have a ZIP
                            //        if (data.StreetNamesToZips.ContainsKey(toInspect))
                            //        {
                            //            if (data.StreetNamesToZips[toInspect].Contains(address.Zip.Value))
                            //            {
                            //                lock (perfectMatch)
                            //                {
                            //                    perfectMatch.Add(address.OriginalLine);
                            //                    matched = true;
                            //                    break;
                            //                }
                            //            }
                            //        }
                            //    }

                            //}

                            //// assume zip is right, but the street name is a bit wonky. 
                            //if (!matched && address.Zip.HasValue)
                            //{
                            //    List<string> alternativeStreetNames = BKTreeEngine.LeastEditDistance(address.StreetName, data.BKTree).Distinct().ToList();
                            //    foreach (string alternativeStreetName in alternativeStreetNames)
                            //    {
                            //        if (data.AlternateSuffixList.ContainsKey(alternativeStreetName))
                            //        {
                            //            List<string> alternateSuffixes = data.AlternateSuffixList[alternativeStreetName];
                            //            foreach (string alternateSuffix in alternateSuffixes)
                            //            {
                            //                string streetWithAlternateSuffix = alternativeStreetName + " " +
                            //                    alternateSuffix;

                            //                if (data.StreetNamesToZips.ContainsKey(streetWithAlternateSuffix))
                            //                {
                            //                    List<int> zips = data.StreetNamesToZips[streetWithAlternateSuffix];
                            //                    if (zips.Contains(address.Zip.Value))
                            //                    {
                            //                        matched = true;
                            //                        lock (adjustmentNeededToStreetName)
                            //                        {
                            //                            adjustmentNeededToStreetName.Add(address.OriginalLine);
                            //                        }
                            //                        break;
                            //                    }
                            //                }
                            //            }
                            //        }

                            //    }
                            //}

                            //// street name perfect match, zip code is wonky. 
                            //if (!matched && !string.IsNullOrEmpty(address.StreetName))
                            //{
                            //    string streetNameToSearch = (address.StreetName + " " + address.Suffix).Trim();
                            //    if (data.NYStateStreets.Contains(streetNameToSearch))
                            //    {
                            //        lock (streetNamePerfectMatch)
                            //        {
                            //            streetNamePerfectMatch.Add(AddressUtility.CreateLineFromAddress(address));
                            //        }
                            //        matched = true;
                            //    }
                            //}
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

                cleanedAddresses[c] = address;

                if (address.MatchQuality == MatchQuality.Unknown)
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
                else if (address.MatchQuality == MatchQuality.Alternate)
                {
                    lock (alternate)
                    {
                        alternate.Add(address.RawAddress1);
                    }
                }
                else if (address.MatchQuality == MatchQuality.LeaveAlone)
                {
                    lock (leaveAlone)
                    {
                        leaveAlone.Add(address.RawAddress1);
                    }
                }
                else if (address.MatchQuality == MatchQuality.MatchNotYetDetermined)
                {
                    lock (matchNotYetDetermined)
                    {
                        matchNotYetDetermined.Add($"{address.RawAddress1}=>{address.FullStreetName}");
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

            File.WriteAllLines("CleanedAddresses.csv", cleanedAddresses.Select(a => (a.StreetNumber != "" ? a.StreetNumber + " " + a.FullStreetName : a.FullStreetName)));


            Console.WriteLine($"Finished. {iterations}/{data.FinalDataSet.Length}: {leaveAlone.Count} LeaveAlone {alternate.Count} Alternate {homeless.Count} Homeless {unknown.Count} Unknown {matchNotYetDetermined.Count} MatchNotYetDetermined.");

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

            Data data = DataLoader.LoadData(regenerateBKTree: true);

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
            Data data = DataLoader.LoadData(regenerateBKTree: true);

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
            DataSetParsers.DatFileGenerator.Generate("D:/StreetSegment.csv");
            //GetCleanedNYStreetList2();
            return;
        }
    }
}
