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

        private static string TestAddress(Data data, ref Address address, string stringToTest, int maxEditDistance)
        {
            int distance;
            List<string> closestNeighbors = BKTreeEngine.LeastEditDistanceWithDistance(stringToTest, data.StreetNameBKTree, out distance);

            if (distance <= maxEditDistance)
            {
                return CorrectAddress(data, ref address, closestNeighbors[0]);
            }
            else
            {
                return null;
            }
        }


        private static List<string> alternate = new List<string>();
        private static List<string> leaveAlone = new List<string>();
        private static List<string> matchNotYetDetermined = new List<string>();
        private static List<string> homeless = new List<string>();
        private static List<string> unknown = new List<string>();
        private static Address LucasAddressMatch(string line, Data data)
        {
            string matched = line;

            Address address = AddressUtility.InitializeAddress(line, data);

            bool matchFound = (address.MatchQuality != MatchQuality.MatchNotYetDetermined);

            if (matchFound)
            {
                level1Match.Add(address.FullStreetName);
            }

            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName, 0);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level2Match.Add(address.FullStreetName);
                }
            }

            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName + " ST", 0);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level3Match.Add(address.FullStreetName);
                }
            }
            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName + " AVE", 0);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level3Match.Add(address.FullStreetName);
                }
            }
            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName + " BLVD", 0);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level3Match.Add(address.FullStreetName);
                }
            }

            if (!matchFound)
            {
                List<string> closestNeighbors;
                int distance;
                // Search by ZIP
                if (address.Zip != null && !string.IsNullOrEmpty(address.StreetName) && !string.IsNullOrEmpty(address.StreetNumber))
                {
                    StateOfNewYorkAddressRange[] streetsWithZip = data.NYCityStreets.Where(n => n.StreetNumber.IsInRange(address.StreetNumber) && n.ZipCode == address.Zip.Value).ToArray();

                    List<string> streetsWithZipStrings = streetsWithZip.Select(s => s.FullStreetName).Distinct().ToList();
                    BKTree bkTreeLocal = BKTreeEngine.CreateBKTree(streetsWithZipStrings);

                    closestNeighbors = BKTreeEngine.LeastEditDistanceWithDistance(address.FullStreetName, bkTreeLocal, out distance);

                    if (closestNeighbors.Count == 1 && distance <= 1)
                    {
                        matched = CorrectAddress(data, ref address, closestNeighbors[0]);
                        matchFound = true;
                        level5Match.Add(address.FullStreetName);
                    }
                    else if (closestNeighbors.Count == 1 && distance <= 2)
                    {
                        matched = CorrectAddress(data, ref address, closestNeighbors[0]);
                        matchFound = true;
                        level6Match.Add(address.FullStreetName);
                    }
                }
            }

            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName, 2);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level7Match.Add(address.FullStreetName);
                }
            }
            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName + " ST", 2);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level8Match.Add(address.FullStreetName);
                }
            }
            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName + " AVE", 2);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level8Match.Add(address.FullStreetName);
                }
            }
            if (!matchFound)
            {
                matched = TestAddress(data, ref address, address.FullStreetName + " BLVD", 2);
                matchFound = (matched != null);
                if (matchFound)
                {
                    level8Match.Add(address.FullStreetName);
                }
            }

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
                    string closestNeighborsConcatenated = string.Join(" OR ", BKTreeEngine.LeastEditDistance(address.FullStreetName, data.StreetNameBKTree));

                    Console.WriteLine($"{addressRaw} => {addressCleaned} => {closestNeighborsConcatenated}");
                }
            }


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
            return address;
        }

        private static List<string> level1Match = new List<string>();
        private static List<string> level2Match = new List<string>();
        private static List<string> level3Match = new List<string>();
        private static List<string> level4Match = new List<string>();
        private static List<string> level5Match = new List<string>();
        private static List<string> level6Match = new List<string>();
        private static List<string> level7Match = new List<string>();
        private static List<string> level8Match = new List<string>();
        private static List<string> level9Match = new List<string>();
        private static List<string> level10Match = new List<string>();
        private static List<string> failed = new List<string>();
        private static List<string> BenAddressMatch(string line, Data data)
        {
            // clean the address and do what we can with pure NLP. 
            Address address = AddressUtility.InitializeAddressBen(line, data);

            List<string> alternateLines = new List<string>();

            if (address.MatchQuality == MatchQuality.MatchNotYetDetermined)
            {
                bool matched = false;

                if (address.POBoxNumber != 0)
                {
                    matched = true;
                    level1Match.Add(address.OriginalLine);

                    string alternateLine =
                        AddressUtility.CreateLineFromAddress(address, "PO BOX " + address.POBoxNumber.ToString());
                    alternateLines.Add(alternateLine);
                }
                else
                {
                    //matched = LucasAddressMatch(address, data);
                    //if (address.MatchQuality == MatchQuality.Alternate)
                    //{
                    //    lock (level1Match)
                    //    {
                    //        level1Match.Add(AddressUtility.CreateLineFromAddress(address));
                    //    }
                    //    matched = true;
                    //}
                    //else
                    //{
                    if (MatchEngine.IsPerfectMatchIncludingZipAndCity(address, data))
                    {
                        lock (level1Match)
                        {
                            lock (level1Match)
                            {
                                level1Match.Add(address.OriginalLine);
                            }
                        }
                    }
                    else if (MatchEngine.IsRearrangedAddressAndCityZipMatch(address, data, alternateLines))
                    {
                        lock (level2Match)
                        {
                            level2Match.Add(address.OriginalLine);
                        }
                    }
                    else if (MatchEngine.IsRearrangedAddressAndCityOrZipMatch(address, data, alternateLines))
                    {
                        lock (level3Match)
                        {
                            level3Match.Add(address.OriginalLine);
                        }
                    }
                    else if (MatchEngine.IsRearrangedAddressAndSoftCityMatch(address, data, alternateLines))
                    {
                        lock (level4Match)
                        {
                            level4Match.Add(address.OriginalLine);
                        }
                    }
                    else if (MatchEngine.IsSolidAddressMatchWithNoZipCityAvailable(address, data, alternateLines))
                    {
                        lock (level5Match)
                        {
                            level5Match.Add(address.OriginalLine);
                        }
                    }

                    else if (MatchEngine.IsSoftAddressAndSoftCityHardZipMatch(address, data, alternateLines))
                    {
                        lock (level6Match)
                        {
                            level6Match.Add(address.OriginalLine);
                        }
                    }
                    else if (MatchEngine.IsSoftAddressAndHardZipMatch(address, data, alternateLines))
                    {
                        lock (level7Match)
                        {
                            level7Match.Add(address.OriginalLine);
                        }
                    }
                    else if (MatchEngine.IsSoftAddressAndSoftCityMatch(address, data, alternateLines))
                    {
                        lock (level8Match)
                        {
                            level8Match.Add(address.OriginalLine);
                        }

                    }
                    else if (MatchEngine.IsSolidStreetMatchOnly(address, data, alternateLines))
                    {
                        lock (level9Match)
                        {
                            level9Match.Add(address.OriginalLine);
                        }
                    }
                    else if (MatchEngine.IsSoftAddressMatch(address, data, alternateLines))
                    {
                        lock (level10Match)
                        {
                            level10Match.Add(address.OriginalLine);
                        }
                    }
                    else
                    {
                        lock (failed)
                        {
                            failed.Add(address.OriginalLine);
                        }
                    }
                    //}
                }
            }
            else if (address.MatchQuality == MatchQuality.Unknown)
            {
                lock (alternateLines)
                {
                    alternateLines.Add(AddressUtility.CreateLineFromAddress(address, "UNKNOWN"));
                    unknown.Add(address.OriginalLine); 
                }
            }
            else if (address.MatchQuality == MatchQuality.Homeless)
            {
                lock (alternateLines)
                {
                    alternateLines.Add(AddressUtility.CreateLineFromAddress(address, "HOMELESS"));
                    homeless.Add(address.OriginalLine); 
                }
            }

            return alternateLines;
        }

        static List<string> GetCleanedNYStreetListBen()
        {
            Console.WriteLine("Loading data...");
            //// read from all the necessary files
            //Data data = DataLoader.LoadData(regenerateBKTree: true);
            Data data = DataLoader.LoadDataBen(regenerateBKTree: true);

            //KeyValuePair<StreetNameAndCity, List<int>>[] rest = data.StreetNameCity2Zips.Where(n => n.Key.FullStreetName == "73 ST").ToArray();

            //data.FinalDataSet = data.FinalDataSet.Where(n => n.Contains("14121753")).Take(1).ToArray();

            //StreetName[] all = data.StreetData.Where(n => n.FullStreetName.Contains("56")).ToArray(); 

            //Random rand = new Random();
            //data.FinalDataSet = data.FinalDataSet.Where(b => rand.Next() % 10 == 0).ToArray();

            Console.WriteLine("Data loaded.");

            int numberCouldntProcess = 0;

            // counter variables. 
            int iterations = 0;

            List<string> allAlternates = new List<string>();

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
                    int sum = (level1Match.Count + level2Match.Count +
                        level3Match.Count + level4Match.Count +
                        level5Match.Count + level6Match.Count +
                        level7Match.Count + level8Match.Count +
                        level9Match.Count + level10Match.Count +
                        homeless.Count + unknown.Count);

                    double percentage = (sum / ((iterations * 1.0))) * 100;

                    Console.Clear();
                    //Console.WriteLine();
                    //Console.WriteLine();
                    //Console.WriteLine();
                    Console.WriteLine($"Level 1 Match: {level1Match.Count}");
                    Console.WriteLine($"Level 2 Match: {level2Match.Count}");
                    Console.WriteLine($"Level 3 Match: {level3Match.Count}");
                    Console.WriteLine($"Level 4 Match: {level4Match.Count}");
                    Console.WriteLine($"Level 5 Match: {level5Match.Count}");
                    Console.WriteLine($"Level 6 Match: {level6Match.Count}");
                    Console.WriteLine($"Level 7 Match: {level7Match.Count}");
                    Console.WriteLine($"Level 8 Match: {level8Match.Count}");
                    Console.WriteLine($"Level 9 Match: {level9Match.Count}");
                    Console.WriteLine($"Level 10 Match: {level10Match.Count}");
                    Console.WriteLine($"Homeless or Unknown: {homeless.Count + unknown.Count}");
                    Console.WriteLine($"Failed: {failed.Count}");
                    Console.WriteLine("========SUMMARY=======");
                    Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}: {percentage.ToString("0.00")}% matched. {hoursLeft.ToString("0.00")} hours left.");

                    lastTime = now;
                }
                #endregion

                //Address address = LucasAddressMatch(data.FinalDataSet[c], data);
                List<string> alternates = BenAddressMatch(data.FinalDataSet[c], data);

                //lock (allAlternates)
                //{
                //    allAlternates.AddRange(alternates);
                //}
            });


            //using (StreamWriter fout = File.CreateText("perfectMatch.txt"))
            //{
            //    for (int c = 0; c < perfectMatch.Count; c++)
            //    {
            //        fout.WriteLine(perfectMatch[c]);
            //    }
            //}

            using (StreamWriter fout = File.CreateText("failed.txt"))
            {
                for (int c = 0; c < failed.Count; c++)
                {
                    fout.WriteLine(failed[c]);
                }
            }

            File.WriteAllLines("Alternates.csv", allAlternates.ToArray());

            //File.WriteAllLines("CleanedAddresses.csv", cleanedAddresses.Select(a => (a.StreetNumber != "" ? a.StreetNumber + " " + a.FullStreetName : a.FullStreetName)));

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Level 1 Match: {level1Match.Count}");
            Console.WriteLine($"Level 2 Match: {level2Match.Count}");
            Console.WriteLine($"Level 3 Match: {level3Match.Count}");
            Console.WriteLine($"Level 4 Match: {level4Match.Count}");
            Console.WriteLine($"Level 5 Match: {level5Match.Count}");
            Console.WriteLine($"Level 6 Match: {level6Match.Count}");
            Console.WriteLine($"Level 7 Match: {level7Match.Count}");
            Console.WriteLine($"Level 8 Match: {level8Match.Count}");
            Console.WriteLine($"Level 9 Match: {level9Match.Count}");
            Console.WriteLine($"Homeless or Unknown: {homeless.Count + unknown.Count}");
            Console.WriteLine($"Finished. {iterations}/{data.FinalDataSet.Length}.");

            Console.ReadLine();

            return null;
        }

        static List<string> GetCleanedNYStreetList2()
        {
            Console.WriteLine("Loading data...");
            //// read from all the necessary files
            Data data = DataLoader.LoadData(regenerateBKTree: true);
            //Data data = DataLoader.LoadDataBen(regenerateBKTree: true);

            //KeyValuePair<StreetNameAndCity, List<int>>[] rest = data.StreetNameCity2Zips.Where(n => n.Key.FullStreetName == "73 ST").ToArray();

            //data.FinalDataSet = data.FinalDataSet.Where(n => n.Contains("14423865")).Take(1).ToArray();

            //StreetName[] all = data.StreetData.Where(n => n.FullStreetName.Contains("56")).ToArray(); 

            //Random rand = new Random();
            //data.FinalDataSet = data.FinalDataSet.Where(b => rand.Next() % 10 == 0).ToArray();

            Console.WriteLine("Data loaded.");

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
                    int sum = (level1Match.Count + level2Match.Count +
                        level3Match.Count + level4Match.Count +
                        level5Match.Count + level6Match.Count +
                        level7Match.Count + level8Match.Count +
                        level9Match.Count + level10Match.Count +
                        homeless.Count + unknown.Count);

                    double percentage = (sum / ((iterations * 1.0))) * 100;

                    Console.Clear();
                    //Console.WriteLine();
                    //Console.WriteLine();
                    //Console.WriteLine();
                    Console.WriteLine($"Level 1 Match: {level1Match.Count}");
                    Console.WriteLine($"Level 2 Match: {level2Match.Count}");
                    Console.WriteLine($"Level 3 Match: {level3Match.Count}");
                    Console.WriteLine($"Level 4 Match: {level4Match.Count}");
                    Console.WriteLine($"Level 5 Match: {level5Match.Count}");
                    Console.WriteLine($"Level 6 Match: {level6Match.Count}");
                    Console.WriteLine($"Level 7 Match: {level7Match.Count}");
                    Console.WriteLine($"Level 8 Match: {level8Match.Count}");
                    Console.WriteLine($"Level 9 Match: {level9Match.Count}");
                    Console.WriteLine($"Level 10 Match: {level10Match.Count}");
                    Console.WriteLine($"Homeless or Unknown: {homeless.Count + unknown.Count}");
                    Console.WriteLine($"Failed: {failed.Count}");
                    Console.WriteLine("========SUMMARY=======");
                    Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}: {percentage.ToString("0.00")}% matched. {hoursLeft.ToString("0.00")} hours left.");

                    lastTime = now;
                }
                #endregion

                Address address = LucasAddressMatch(data.FinalDataSet[c], data);
                //Address address = BenAddressMatch(data.FinalDataSet[c], data);

                cleanedAddresses[c] = address;
            });


            //using (StreamWriter fout = File.CreateText("perfectMatch.txt"))
            //{
            //    for (int c = 0; c < perfectMatch.Count; c++)
            //    {
            //        fout.WriteLine(perfectMatch[c]);
            //    }
            //}

            using (StreamWriter fout = File.CreateText("failed.txt"))
            {
                for (int c = 0; c < failed.Count; c++)
                {
                    fout.WriteLine(failed[c]);
                }
            }

            File.WriteAllLines("CleanedAddresses.csv", cleanedAddresses.Select(a => (a.StreetNumber != "" ? a.StreetNumber + " " + a.FullStreetName : a.FullStreetName)));

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Level 1 Match: {level1Match.Count}");
            Console.WriteLine($"Level 2 Match: {level2Match.Count}");
            Console.WriteLine($"Level 3 Match: {level3Match.Count}");
            Console.WriteLine($"Level 4 Match: {level4Match.Count}");
            Console.WriteLine($"Level 5 Match: {level5Match.Count}");
            Console.WriteLine($"Level 6 Match: {level6Match.Count}");
            Console.WriteLine($"Level 7 Match: {level7Match.Count}");
            Console.WriteLine($"Level 8 Match: {level8Match.Count}");
            Console.WriteLine($"Level 9 Match: {level9Match.Count}");
            Console.WriteLine($"Homeless or Unknown: {homeless.Count + unknown.Count}");
            Console.WriteLine($"Finished. {iterations}/{data.FinalDataSet.Length}.");

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

            //bool contains = StringUtility.Contains("MORRIS AVE", "BVE"); 

            //DataSetParsers.DatFileGenerator.Generate("D:/StreetSegment.csv");
            GetCleanedNYStreetList2();
            //GetCleanedNYStreetListBen();
            return;
        }
    }
}
