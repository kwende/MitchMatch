using Common;
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
            Console.WriteLine("Loading data...");
            //// read from all the necessary files
            Data data = DataLoader.LoadData();
            //data.FinalDataSet = data.FinalDataSet.Where(b => b.Contains("23 OLYMPUS STREET,")).Take(1).ToArray();

            List<Address> streetsNormalized = data.NewYorkStateStreetNames.Select(street => AddressUtility.NormalizeSuffix(street, data)).ToList();
            List<string> streetsDistinct = streetsNormalized.Select(street => street.FullStreetName).Distinct().ToList();

            BKTree bkTree = BKTreeEngine.CreateBKTree(streetsDistinct);

            Console.WriteLine("Data loaded.");

            // create lists to store exact, no match and near matches. 
            List<string> unknown = new List<string>();
            List<string> notMatched = new List<string>();
            List<string> homeless = new List<string>();
            List<string> streetMatched = new List<string>();
            List<string> fullAddressMatched = new List<string>();
            List<string> couldNotParse = new List<string>();
            List<string> databaseCorrected = new List<string>();

            // counter variables. 
            int iterations = 0;

            List<Address> cleanedAddresses = new List<Address>();

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

                    Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}: {fullAddressMatched.Count} Full. {streetMatched.Count} Street. {homeless.Count} Homeless. {unknown.Count} Unknown. {couldNotParse.Count} Couln't parse. {notMatched.Count} Not matched. {databaseCorrected.Count} database corrected. Projected {percentage.ToString("0.00")}% match. {hoursLeft.ToString("0.00")} hours left.");

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
                        // Look for building matching
                        address = AddressUtility.CheckForBuildingsAndCenters(address, data);

                        if (address.MatchQuality != MatchQuality.FullAddressMatched)
                        {
                            // Look for street name matching
                            List<string> closestNeigbors = BKTreeEngine.LeastEditDistance(address.FullStreetName, bkTree);

                            if (closestNeigbors.Count == 1 && EditDistanceEngine.Compute(address.FullStreetName, closestNeigbors[0]) <= 2)
                            {
                                Address correctStreet = AddressUtility.NormalizeSuffix(closestNeigbors[0], data);
                                address.StreetName = correctStreet.StreetName;
                                address.Suffix = correctStreet.Suffix;
                                address.MatchQuality = MatchQuality.StreetMatched;
                            }
                            else
                            {
                                bool isDatabaseCorrected = false;
                                if (address.MatchQuality == MatchQuality.NotMatched && address.Zip != null &&
                                    !string.IsNullOrEmpty(address.StreetName) && !string.IsNullOrEmpty(address.StreetNumber))
                                {
                                    StateOfNewYorkAddressRange[] streetsWithZip = data.AllAddresses.Where(n => n.StreetNumber.IsInRange(address.StreetNumber) && n.ZipCode == address.Zip.Value).ToArray();

                                    List<string> streetsWithZipStrings = streetsWithZip.Select(s => s.FullStreetName).ToList();
                                    BKTree bkTreeLocal = BKTreeEngine.CreateBKTree(streetsWithZipStrings);

                                    List<string> closestNeigborsWithZip = BKTreeEngine.LeastEditDistance(address.FullStreetName, bkTreeLocal);

                                    if (closestNeigborsWithZip.Count == 1 && EditDistanceEngine.Compute(address.FullStreetName, closestNeigborsWithZip[0]) <= 2)
                                    {
                                        Address correctStreet = AddressUtility.NormalizeSuffix(closestNeigborsWithZip[0], data);
                                        address.StreetName = correctStreet.StreetName;
                                        address.Suffix = correctStreet.Suffix;
                                        address.MatchQuality = MatchQuality.StreetMatched;
                                    }
                                    else
                                    {
                                        address.MatchQuality = MatchQuality.NotMatched;
                                        if (true)
                                        {
                                            const int Address2Column = 9;
                                            string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(data.FinalDataSet[c]);
                                            string addressRaw = $"{address.RawAddress1}";
                                            if (bits[Address2Column].Length <= 5)
                                            {
                                                addressRaw += $" / {bits[Address2Column]}";
                                            }
                                            string addressCleaned = $"{ address.StreetNumber } / { address.StreetName} / { address.Suffix}";
                                            if (!string.IsNullOrEmpty(address.ApartmentNumber))
                                            {
                                                addressCleaned += $" / {address.ApartmentNumber}";
                                            }
                                            string closestNeighborsConcatenated = string.Concat(closestNeigbors.Select(s => s + " OR "));

                                            if (isDatabaseCorrected)
                                            {
                                                Console.WriteLine($"{addressRaw} => {addressCleaned} => {closestNeighborsConcatenated}");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"{addressRaw} => {addressCleaned} => {closestNeighborsConcatenated}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }



                if (address.MatchQuality == MatchQuality.StreetMatched)
                {
                    lock (streetMatched)
                    {
                        streetMatched.Add($"{address.RawAddress1}=>{matched}");
                    }
                }

                if (address.MatchQuality == MatchQuality.FullAddressMatched)
                {
                    lock (fullAddressMatched)
                    {
                        fullAddressMatched.Add(address.StreetName);
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
                        notMatched.Add($"{address.RawAddress1}");
                    }
                }
                else if (address.MatchQuality == MatchQuality.CouldNotParseFormat)
                {
                    lock (couldNotParse)
                    {
                        couldNotParse.Add($"{address.RawAddress1}");
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

        static void Main(string[] args)
        {
            int edit = EditDistanceEngine.Compute("GRANDCONCOURSE", "GRAND CONCOURSE");
            bool yes = StringUtility.IsDistance1OrLessApart("GRANDCONCOURSE", "GRAND CONCOURSE");

            GetCleanedNYStreetList2();

            return;
        }
    }
}
