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


        static List<string> GetCleanedNYStreetList2()
        {
            //// read from all the necessary files
            Data data = DataLoader.LoadData();
            //data.FinalDataSet = data.FinalDataSet.Where(b => b.Contains("SO 3 STREET")).ToArray();
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

            // create lists to store exact, no match and near matches. 
            List<string> unknown = new List<string>();
            List<string> notMatched = new List<string>();
            List<string> homeless = new List<string>();
            List<string> streetMatched = new List<string>();
            List<string> fullAddressMatched = new List<string>();
            List<string> couldNotParse = new List<string>();
            List<string> databaseCorrected = new List<string>();

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

                    double percentage = ((fullAddressMatched.Count + homeless.Count + unknown.Count + streetMatched.Count) / (iterations * 1.0)) * 100;

                    Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}: {fullAddressMatched.Count} Full. {streetMatched.Count} Street. {homeless.Count} Homeless. {unknown.Count} Unknown. {couldNotParse.Count} Couln't parse. {notMatched.Count} Not matched. Projected {percentage.ToString("0.00")}% match. {hoursLeft.ToString("0.00")} hours left.");

                    lastTime = now;
                }
                #endregion

                Address address = AddressUtility.InitializeAddress(data.FinalDataSet[c], data);

                string matched = "";

                if (address.MatchQuality == MatchQuality.NotMatched)
                {
                    if (address.POBoxNumber > 0)
                    {
                        address.MatchQuality = MatchQuality.StreetMatched;
                        matched = address.StreetName;
                    }
                    else
                    {
                        string address1 = address.StreetName;
                        if (!string.IsNullOrEmpty(address.Suffix))
                        {
                            address1 += " " + address.Suffix;
                        }
                        // look for street name matching. 

                        List<string> closestMatches = BKTreeEngine.LeastEditDistance(address1, data.BKTree);

                        List<string> databasedMatchedStreetNames = new List<string>();
                        foreach (string closestMatch in closestMatches)
                        {
                            if (data.StreetNamesToStreetNumbers.ContainsKey(closestMatch))
                            {
                                databasedMatchedStreetNames.Add(closestMatch);
                                if (address.Zip != null)
                                {
                                    if (data.StreetNamesToStreetNumbers[closestMatch].ContainsKey(address.Zip.Value))
                                    {
                                        List<string> streetNumbers = data.StreetNamesToStreetNumbers[closestMatch][address.Zip.Value];
                                        if (!string.IsNullOrEmpty(address.StreetNumber) && streetNumbers.Contains(address.StreetNumber))
                                        {
                                            lock (fullAddressMatched)
                                            {
                                                fullAddressMatched.Add($"{address.RawAddress1}=>{closestMatch}");
                                                address.MatchQuality = MatchQuality.FullAddressMatched;
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            lock (streetMatched)
                                            {
                                                streetMatched.Add($"{address.RawAddress1}=>{closestMatches[0]}");
                                                address.MatchQuality = MatchQuality.StreetMatched;
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // do we have a match? 
                        if (address.MatchQuality == MatchQuality.NotMatched)
                        {
                            // no? let's see if we can normalize by building name. 
                            address = AddressUtility.CheckForBuildingsAndCenters(address, data);
                        }

                        if (address.MatchQuality == MatchQuality.NotMatched && closestMatches.Count == 1)
                        {
                            lock (streetMatched)
                            {
                                streetMatched.Add($"{address.RawAddress1}=>{closestMatches[0]}");
                            }
                            address.MatchQuality = MatchQuality.StreetMatched;
                        }
                        else if (address.MatchQuality == MatchQuality.NotMatched && closestMatches.Count > 1)
                        {
                            lock (notMatched)
                            {
                                string closestMatchesStrings = string.Join(",", closestMatches.ToArray());
                                notMatched.Add($"{address.RawAddress1}|{address1}=>{closestMatchesStrings}");
                            }
                        }

                        //const int MinimumLengthForEditDistance1ToStillCount = 7;
                        //for (int e = 0; e < streetNameSubStrings.Count; e++)
                        //{
                        //    //string streetName = streetNames[e];
                        //    //if ((address1 == streetName ||
                        //    //    StringUtility.Contains(address1, streetNameSubStrings[e]) ||
                        //    //    StringUtility.EndsWith(address1, streetNameEndsWith[e]) ||
                        //    //    (!address.StreetNameIsNumber && address1.Length >= MinimumLengthForEditDistance1ToStillCount &&
                        //    //        StringUtility.IsDistance1OrLessApart(address1, streetName))) && streetName.Length > matched.Length)
                        //    //{
                        //    //    matched = streetName;
                        //    //    address.MatchQuality = MatchQuality.StreetMatched;
                        //    //}
                        //    //else if (string.IsNullOrEmpty(address.Suffix))
                        //    //{
                        //    //    string[] commonSuffixes = { " STREET", " AVENUE" };
                        //    //    // street is a far and away the most common name.
                        //    //    // what happens if we just append that? 
                        //    //    foreach (string commonSuffix in commonSuffixes)
                        //    //    {
                        //    //        string adjustedAddress1 = address1 + commonSuffix;
                        //    //        if ((adjustedAddress1 == streetName ||
                        //    //            StringUtility.Contains(adjustedAddress1, streetNameSubStrings[e]) ||
                        //    //            StringUtility.EndsWith(adjustedAddress1, streetNameEndsWith[e])
                        //    //            && streetName.Length > matched.Length))
                        //    //        {
                        //    //            matched = streetName;
                        //    //            address.MatchQuality = MatchQuality.StreetMatched;
                        //    //        }

                        //    //        if (address.MatchQuality == MatchQuality.StreetMatched)
                        //    //        {
                        //    //            break;
                        //    //        }
                        //    //    }
                        //    //}
                        //}
                    }
                }

                //bool isDatabaseCorrected = false;
                //if (address.MatchQuality == MatchQuality.NotMatched)
                //{
                //    if (address.Zip != null && !string.IsNullOrEmpty(address.StreetName) && !string.IsNullOrEmpty(address.StreetNumber))
                //    {
                //        StateOfNewYorkAddressRange[] addrs =
                //        data.AllAddresses.Where(n => n.StreetNumber.IsInRange(address.StreetNumber) && n.ZipCode == address.Zip.Value && !string.IsNullOrEmpty(n.StreetName)).ToArray();

                //        StateOfNewYorkAddressRange best = null;
                //        double lowestEditDistance = double.MaxValue;

                //        foreach (StateOfNewYorkAddressRange addr in addrs)
                //        {
                //            // first characters seem to usually match. 
                //            double editDistance = EditDistance.ComputeNormalized(address.StreetName, addr.StreetName);
                //            if (editDistance < .5 || (address.StreetName[0] == addr.StreetName[0]) && editDistance < .85)
                //                if (editDistance < lowestEditDistance)
                //                {
                //                    lowestEditDistance = editDistance;
                //                    best = addr;
                //                }
                //        }

                //        if (best != null)
                //        {
                //            lock (databaseCorrected)
                //            {
                //                string corrected = $"{best.StreetNumber} {best.CardinalDirection} {best.StreetName} {best.Suffix}: edit distance {lowestEditDistance}".Trim();
                //                databaseCorrected.Add($"{address.RawAddress1}=>{corrected}");
                //                isDatabaseCorrected = true;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        Interlocked.Increment(ref numberCouldntProcess);
                //    }
                //}

                //if (!isDatabaseCorrected)
                //{
                //    if (address.MatchQuality == MatchQuality.StreetMatched)
                //    {
                //        lock (streetMatched)
                //        {
                //            streetMatched.Add($"{address.RawAddress1}=>{matched}");
                //        }
                //    }

                //    if (address.MatchQuality == MatchQuality.FullAddressMatched)
                //    {
                //        lock (fullAddressMatched)
                //        {
                //            fullAddressMatched.Add(address.StreetName);
                //        }
                //    }

                if (address.MatchQuality == MatchQuality.Homeless)
                {
                    lock (homeless)
                    {
                        homeless.Add(address.RawAddress1);
                    }
                }
                else if (address.MatchQuality == MatchQuality.Unknown)
                {
                    lock (unknown)
                    {
                        unknown.Add(address.RawAddress1);
                    }
                }
                //    else if (address.MatchQuality == MatchQuality.NotMatched)
                //    {
                //        lock (notMatched)
                //        {
                //            notMatched.Add($"{address.RawAddress1}");
                //        }
                //    }
                //    else if (address.MatchQuality == MatchQuality.CouldNotParseFormat)
                //    {
                //        lock (couldNotParse)
                //        {
                //            couldNotParse.Add($"{address.RawAddress1}");
                //        }
                //    }
                //}

            });

            using (StreamWriter fout = File.CreateText("streetMatched.txt"))
            {
                for (int c = 0; c < streetMatched.Count; c++)
                {
                    fout.WriteLine(streetMatched[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("databaseCorrected.txt"))
            {
                for (int c = 0; c < databaseCorrected.Count; c++)
                {
                    fout.WriteLine(databaseCorrected[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("fullAddressMatched.txt"))
            {
                for (int c = 0; c < fullAddressMatched.Count; c++)
                {
                    fout.WriteLine(fullAddressMatched[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("homeless.txt"))
            {
                for (int c = 0; c < homeless.Count; c++)
                {
                    fout.WriteLine(homeless[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("unknown.txt"))
            {
                for (int c = 0; c < unknown.Count; c++)
                {
                    fout.WriteLine(unknown[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("notMatched.txt"))
            {
                for (int c = 0; c < notMatched.Count; c++)
                {
                    fout.WriteLine(notMatched[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("couldNotParse.txt"))
            {
                for (int c = 0; c < couldNotParse.Count; c++)
                {
                    fout.WriteLine(couldNotParse[c]);
                }
            }

            Console.WriteLine($"Finished. {iterations}/{data.FinalDataSet.Length}: {fullAddressMatched.Count} Full {streetMatched.Count} Street {homeless.Count} Homeless {unknown.Count} Unknown {couldNotParse.Count} Couln't parse {notMatched.Count} Not matched.");

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
            List<string> uniques = File.ReadAllLines("c:/users/brush/desktop/distinctStreets.csv").ToList();

            BKTree tree = BKTreeEngine.CreateBKTree(uniques.Distinct().ToList());
            BKTreeSerializer.SerializeTo(tree, outputSerializedPath);
        }

        static void BuildDictionary(string outputSerializedPath)
        {
            string[] stateWide = File.ReadAllLines(@"C:\Users\brush\Downloads\openaddr-collected-us_northeast\us\ny\statewide.csv").Select(n => n.ToUpper()).ToArray();
            string[] newYork = File.ReadAllLines(@"D:\repos\MitchMatch\UndressAddress\city_of_new_york.csv").Select(n => n.ToUpper()).ToArray();

            List<string> allLines = new List<string>();
            allLines.AddRange(stateWide.Skip(1));
            allLines.AddRange(newYork.Skip(1));

            Dictionary<string, Dictionary<int, List<string>>> farts = new Dictionary<string, Dictionary<int, List<string>>>();

            foreach (string line in allLines)
            {
                string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);
                if (bits.Length == 11)
                {
                    string streetName = bits[3].ToUpper();

                    Dictionary<int, List<string>> zipDictionary = null;
                    if (!farts.ContainsKey(streetName))
                    {
                        zipDictionary = new Dictionary<int, List<string>>();
                        farts.Add(streetName, zipDictionary);
                    }
                    else
                    {
                        zipDictionary = farts[streetName];
                    }

                    int zip = 0;
                    if (int.TryParse(bits[8], out zip))
                    {
                        List<string> houseNumbers = null;
                        if (!zipDictionary.ContainsKey(zip))
                        {
                            houseNumbers = new List<string>();
                            zipDictionary.Add(zip, houseNumbers);
                        }
                        else
                        {
                            houseNumbers = zipDictionary[zip];
                        }

                        int streetNumber = 0;
                        if (int.TryParse(bits[2], out streetNumber))
                        {
                            houseNumbers.Add(streetNumber.ToString());
                        }
                        else if (Regex.IsMatch(bits[2], @"^(\d+)-(\d+)$"))
                        {
                            int[] parts = bits[2].Split('-').Select(n => int.Parse(n.Trim())).ToArray();

                            int smaller = parts[1];
                            int larger = parts[0];

                            if (larger < smaller)
                            {
                                larger = parts[1];
                                smaller = parts[0];
                            }

                            for (int c = smaller; c <= larger; c++)
                            {
                                houseNumbers.Add(c.ToString());
                            }
                        }
                        else if (!string.IsNullOrEmpty(bits[2]))
                        {
                            if (bits[2].Contains("-"))
                            {
                                string[] parts = bits[2].Split('-');
                                // what else to do? it's not a number range. 
                                houseNumbers.Add(parts[0]);
                                houseNumbers.Add(parts[1]);
                            }
                            else
                            {
                                houseNumbers.Add(bits[2]);
                            }
                        }
                    }
                }
            }

            foreach (string key in farts.Keys)
            {
                Dictionary<int, List<string>> value = farts[key];

                int[] keys = value.Keys.ToArray();
                foreach (int zip in keys)
                {
                    List<string> distinctNumbers = value[zip].Distinct().ToList();
                    value[zip] = distinctNumbers;
                }
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fout = File.Create(outputSerializedPath))
            {
                bf.Serialize(fout, farts);
            }
        }

        static void Main(string[] args)
        {

            //CreateBKTree("C:/users/brush/desktop/bkTree.dat"); 

            GetCleanedNYStreetList2();


            return;
        }
    }
}
