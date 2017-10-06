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
            data.FinalDataSet = data.FinalDataSet.Where(b => b.Contains("754 COSTER AVE")).Take(1).ToArray();
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

                // clean the address and do what we can with pure NLP. 
                Address address = AddressUtility.InitializeAddress(data.FinalDataSet[c], data);

                string matched = "";

                // did NLP do anything? 
                if (address.MatchQuality == MatchQuality.NotMatched)
                {
                    // is this a PO BOX? 
                    if (address.POBoxNumber > 0)
                    {
                        address.MatchQuality = MatchQuality.StreetMatched;
                        matched = address.StreetName;
                    }
                    else // nope, we have more to do. 
                    {
                        // is this a known building address or building name variant? 
                        address = AddressUtility.CheckForBuildingsAndCenters(address, data);

                        // did we find it in thebuilding match above? 
                        if (address.MatchQuality == MatchQuality.NotMatched)
                        {
                            // nope, so get the list of all possible suffixes with this street name. 
                            List<string> streetWithAllPossibleSuffixes = new List<string>();
                            // we need to get the one we parsed out. 
                            // if we didn't parse on out, just put the street name without suffix. 
                            if (!string.IsNullOrEmpty(address.Suffix))
                            {
                                // we have a suffix, so add it. 
                                streetWithAllPossibleSuffixes.Add(address.StreetName + " " + address.Suffix);
                            }
                            else
                            {
                                // we don't have a suffix, so..just add the street name. 
                                streetWithAllPossibleSuffixes.Add(address.StreetName);
                            }

                            // are there alternate suffixes for this street name? 
                            if (data.AlternateSuffixList.ContainsKey(address.StreetName))
                            {
                                // if so, add them to the array. 
                                foreach (string alternateSuffix in data.AlternateSuffixList[address.StreetName])
                                {
                                    streetWithAllPossibleSuffixes.Add(address.StreetName + " " + alternateSuffix);
                                }
                            }

                            // get distincts. 
                            streetWithAllPossibleSuffixes = streetWithAllPossibleSuffixes.Distinct().ToList();

                            // iterate over the list. For most, this list will only have one item. 
                            //foreach (string address1 in streetWithAllPossibleSuffixes)
                            for (int s = 0; s < streetWithAllPossibleSuffixes.Count && address.MatchQuality == MatchQuality.NotMatched; s++)
                            {
                                // get the street. 
                                string address1 = streetWithAllPossibleSuffixes[s];

                                // find the closest matches. 
                                List<string> closestMatches = BKTreeEngine.LeastEditDistance(address1, data.BKTree);

                                // iterate over each match. 
                                foreach (string closestMatch in closestMatches)
                                {
                                    // are we familiar with this street name? 
                                    if (data.StreetNamesToStreetNumbers.ContainsKey(closestMatch))
                                    {
                                        // do we have a ZIP? 
                                        if (address.Zip != null)
                                        {
                                            // do we find the ZIP? 
                                            if (data.StreetNamesToStreetNumbers[closestMatch].ContainsKey(address.Zip.Value))
                                            {
                                                // get the known street numbers for this street. 
                                                List<string> streetNumbers = data.StreetNamesToStreetNumbers[closestMatch][address.Zip.Value];

                                                // do we match a known street number? 
                                                if (!string.IsNullOrEmpty(address.StreetNumber) && streetNumbers.Contains(address.StreetNumber))
                                                {
                                                    // yes, so match perfect. 
                                                    address.MatchQuality = MatchQuality.FullAddressMatched;
                                                    matched = closestMatch;
                                                    break;
                                                }
                                                else
                                                {
                                                    // no, but we got this far and matched on ZIP, so call it a street match. 
                                                    address.MatchQuality = MatchQuality.StreetMatched;
                                                    matched = closestMatch;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (address.MatchQuality == MatchQuality.NotMatched && closestMatches.Count == 1)
                                {
                                    address.MatchQuality = MatchQuality.StreetMatched;
                                }
                            }
                        }
                    }

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
                    else if (address.MatchQuality == MatchQuality.NotMatched)
                    {
                        lock (notMatched)
                        {
                            notMatched.Add(address.RawAddress1);
                        }
                    }
                    else if (address.MatchQuality == MatchQuality.FullAddressMatched)
                    {
                        lock (fullAddressMatched)
                        {
                            fullAddressMatched.Add($"{address.RawAddress1}=>{matched}");
                        }
                    }
                    else if (address.MatchQuality == MatchQuality.StreetMatched)
                    {
                        lock (streetMatched)
                        {
                            streetMatched.Add($"{address.RawAddress1}=>{matched}");
                        }
                    }
                }
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
