using DecisionTreeLearner.Data;
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

            // precompute these strings because otherwise we compute them in a for() loop and 
            // string.concat() becomes a wasteful operation. 
            List<string> streetNameSubStrings = new List<string>();
            streetNameSubStrings.AddRange(data.NewYorkStateStreetNames.Select(n => " " + n + " "));
            List<string> streetNameEndsWith = new List<string>();
            streetNameEndsWith.AddRange(data.NewYorkStateStreetNames.Select(n => " " + n));
            List<string> streetNames = new List<string>();
            streetNames.AddRange(data.NewYorkStateStreetNames);

            // create lists to store exact, no match and near matches. 
            //List<string> exactMatchesFound = new List<string>();
            //List<string> notMatched = new List<string>();
            //List<string> notMatchedButFormatIsGood = new List<string>();

            List<string> unknown = new List<string>();
            List<string> notMatched = new List<string>();
            List<string> homeless = new List<string>();
            List<string> streetMatched = new List<string>();

            // counter variables. 
            int exactMatches = 0;
            int nonZeroAddress1 = 0;
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

                    double averageTimeFor1000 = timeSpans.Average();
                    double numberOf1000sLeft = (data.FinalDataSet.Length - iterations) / 1000.0f;

                    double hoursLeft = (averageTimeFor1000 * numberOf1000sLeft) / 1000.0f / 60.0f / 60.0f;

                    if (timeSpans.Count > 100)
                    {
                        timeSpans.RemoveAt(0);
                    }

                    double percentage = (exactMatches / (iterations * 1.0)) * 100;

                    Console.WriteLine($"{iterations}/{data.FinalDataSet.Length}:{exactMatches}. Projected {percentage.ToString("0.00")}% match. {hoursLeft.ToString("0.00")} hours left.");

                    lastTime = now;
                }
                #endregion

                Address address = AddressUtility.InitializeAddress(data.FinalDataSet[c], data);

                //if (address.MatchQuality == MatchQuality.NotMatched)
                //{
                //    string address1 = address.StreetName + " " + address.Suffix;
                //    string matched = "";
                //    // look for street name matching. 

                //    const int MinimumLengthForEditDistance1ToStillCount = 7;
                //    for (int e = 0; e < streetNameSubStrings.Count; e++)
                //    {
                //        string streetName = streetNames[e];
                //        if ((address1 == streetName ||
                //            StringUtility.Contains(address1, streetNameSubStrings[e]) ||
                //            StringUtility.EndsWith(address1, streetNameEndsWith[e])) && streetName.Length > matched.Length)
                //        {
                //            matched = streetName;
                //            address.MatchQuality = MatchQuality.StreetMatched;
                //        }
                //    }

                //    if (address.MatchQuality == MatchQuality.StreetMatched)
                //    {
                //        lock (streetMatched)
                //        {
                //            streetMatched.Add($"{address.RawAddress1}=>{matched}");
                //        }
                //    }
                //}

                //if (address.MatchQuality == MatchQuality.Homeless)
                //{
                //    lock (homeless)
                //    {
                //        homeless.Add(address.RawAddress1);
                //    }
                //}
                //else if (address.MatchQuality == MatchQuality.Unknown)
                //{
                //    lock (unknown)
                //    {
                //        unknown.Add(address.RawAddress1);
                //    }
                //}
                //else if (address.MatchQuality == MatchQuality.NotMatched)
                //{
                //    lock (notMatched)
                //    {
                //        notMatched.Add(address.RawAddress1);
                //    }
                //}

                //// is there an address left? 
                //if (address1.Length != 0)
                //{
                //    // look for matches. 
                //    Interlocked.Increment(ref nonZeroAddress1);
                //    bool exactMatchFound = false, noMatchButFormatSeemsGood = false;
                //    string matched = "";

                //    // go through all of the uniques and find the 
                //    // longest match that matches. 
                //    foreach (string unique in uniques)
                //    {
                //        if ((address1 == unique ||
                //            address1.Contains(" " + unique + " ") ||
                //            address1.EndsWith(" " + unique)) && unique.Length > matched.Length)
                //        {
                //            exactMatchFound = true;
                //            matched = unique;
                //        }
                //    }

                //    // if no match found, is the format of this line at least sane? 
                //    if (!exactMatchFound)
                //    {
                //        string[] address1Parts = address1.Split(' ');
                //        if (address1Parts.Length >= 3)
                //        {
                //            string firstPart = address1Parts[0];
                //            string lastPart = address1Parts[address1Parts.Length - 1];

                //            if (IsNumber(firstPart) &&
                //                (data.Suffixes.ShortSuffixes.Contains(lastPart) || data.Suffixes.LongSuffixes.Contains(lastPart)))
                //            {
                //                string alternative = FindBestMatchedStreetNameWithinEditDistance(
                //                    address1, data);

                //                if (!string.IsNullOrEmpty(alternative))
                //                {
                //                    exactMatchFound = true;
                //                    matched = alternative;
                //                }
                //                else
                //                {
                //                    noMatchButFormatSeemsGood = true;
                //                }
                //            }
                //        }
                //    }

                //    if (exactMatchFound)
                //    {
                //        exactMatchesFound.Add($"{address1Raw} => {matched}");
                //        exactMatches++;
                //    }
                //    else if (noMatchButFormatSeemsGood)
                //    {
                //        notMatchedButFormatIsGood.Add(address1);
                //    }
                //    else
                //    {
                //        notMatched.Add(address1);
                //    }
                //}
            });

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/homeless.txt"))
            {
                for (int c = 0; c < homeless.Count; c++)
                {
                    fout.WriteLine(homeless[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/unknown.txt"))
            {
                for (int c = 0; c < unknown.Count; c++)
                {
                    fout.WriteLine(unknown[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/notMatched.txt"))
            {
                for (int c = 0; c < notMatched.Count; c++)
                {
                    fout.WriteLine(notMatched[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("C:/users/brush/desktop/streetMatched.txt"))
            {
                for (int c = 0; c < streetMatched.Count; c++)
                {
                    fout.WriteLine(streetMatched[c]);
                }
            }

            Console.WriteLine($"Exact matches: {exactMatches}/{nonZeroAddress1}");

            return null;
        }

        static challenge.Matches FuzzyMatchStreetNames(string[] rows, string[] truth)
        {
            return challenge.FastEditDistanceGrouper.EditDistanceAtMostN(rows, truth, 1);
        }

        static challenge.Matches FuzzyMatchStreetNames2(string[] rows, string[] truth)
        {
            return challenge.FastBKTreeGrouper.EditDistanceAtMostN(rows, truth, 1);
        }

        static void Main(string[] args)
        {
            //    string[] streetSuffixLines = File.ReadAllLines("StreetSuffixes.csv");
            //    string[] shortSuffixes = streetSuffixLines.Select(n => n.Split(',')[1]).ToArray();
            //    string[] longSuffixes = streetSuffixLines.Select(n => n.Split(',')[0]).ToArray();

            //    string[] finalDataSet = File.ReadAllLines("c:/users/brush/desktop/FinalDataset.csv");

            //    Dictionary<string, int> dict = new Dictionary<string, int>();
            //    Parallel.ForEach(finalDataSet, line =>
            //    {
            //        string[] bits = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);
            //        string address1 = bits[8];

            //        // remove apartment stuff
            //        address1 = Regex.Replace(address1, " +", " ");
            //        address1 = Regex.Replace(address1, @" (APT|APARTMENT) (\d+[A-Z]?)", " ");

            //        string[] address1Bits = address1.Split(' ');
            //        string lastBit = address1Bits[address1Bits.Length - 1];

            //        lastBit = Regex.Replace(lastBit, @"(\d+)([A-Z]+)", "$2");
            //        lastBit = Regex.Replace(lastBit, @"([A-Z]+)(\d+)", "$1");
            //        lastBit = lastBit.Replace(".", "");

            //        bool isKnownSuffix = false;

            //        if (lastBit.Length > 1 && Regex.IsMatch(lastBit, @"^[A-Z]+$"))
            //        {
            //            foreach (string shortSuffix in shortSuffixes)
            //            {
            //                if (lastBit == shortSuffix)
            //                {
            //                    isKnownSuffix = true;
            //                    break;
            //                }
            //            }

            //            if (!isKnownSuffix)
            //            {
            //                foreach (string longSuffix in longSuffixes)
            //                {
            //                    if (lastBit == longSuffix || (longSuffix.Length >= 4 && lastBit.EndsWith(longSuffix)))
            //                    {
            //                        isKnownSuffix = true;
            //                        break;
            //                    }
            //                }
            //            }

            //            if (!isKnownSuffix)
            //            {
            //                lock (dict)
            //                {
            //                    if (!dict.ContainsKey(lastBit))
            //                    {
            //                        dict.Add(lastBit, 1);
            //                    }
            //                    else
            //                    {
            //                        dict[lastBit] = dict[lastBit] + 1;
            //                    }
            //                }
            //            }
            //        }

            //    });

            //    using (StreamWriter sw = File.CreateText("c:/users/brush/desktop/suffixes.txt"))
            //    {
            //        foreach (KeyValuePair<string, int> pair in dict)
            //        {
            //            sw.WriteLine($"{pair.Key},{pair.Value}");
            //        }
            //    }

            //return;



            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //for (int c = 0; c < 1000000; c++)
            //{
            //    StringUtility.Contains("Hello World", " Worl");
            //}
            //sw.Stop();

            //Stopwatch sw2 = new Stopwatch();
            //sw2.Start();
            //for (int c = 0; c < 1000000; c++)
            //{
            //    "Hello World".Contains(" Worl");
            //}
            //sw2.Stop();

            //Console.WriteLine($"{sw.ElapsedMilliseconds}, {sw2.ElapsedMilliseconds}");
            //return;

            GetCleanedNYStreetList2();

            return;
        }
    }
}
