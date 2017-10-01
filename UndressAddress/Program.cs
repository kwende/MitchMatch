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
            //data.FinalDataSet = data.FinalDataSet.Where(b => b.Contains("2780 GRANDCONCOURSE")).Take(1).ToArray();

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

                if (address.MatchQuality == MatchQuality.NotMatched)
                {
                    string address1 = address.StreetName;
                    if (!string.IsNullOrEmpty(address.Suffix))
                    {
                        address1 += " " + address.Suffix;
                    }
                    string matched = "";
                    // look for street name matching. 

                    const int MinimumLengthForEditDistance1ToStillCount = 7;
                    for (int e = 0; e < streetNameSubStrings.Count; e++)
                    {
                        string streetName = streetNames[e];
                        if ((address1 == streetName ||
                            StringUtility.Contains(address1, streetNameSubStrings[e]) ||
                            StringUtility.EndsWith(address1, streetNameEndsWith[e]) ||
                            (!address.StreetNameIsNumber && address1.Length >= MinimumLengthForEditDistance1ToStillCount &&
                                StringUtility.IsDistance1OrLessApart(address1, streetName)) && streetName.Length > matched.Length))
                        {
                            matched = streetName;
                            address.MatchQuality = MatchQuality.StreetMatched;
                        }
                    }

                    if (address.MatchQuality == MatchQuality.StreetMatched)
                    {
                        lock (streetMatched)
                        {
                            streetMatched.Add($"{address.RawAddress1}=>{matched}");
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
                        notMatched.Add($"{address.RawAddress1}=>'{address}'");
                    }
                }
            });

            using (StreamWriter fout = File.CreateText("notMatched.txt"))
            {
                for (int c = 0; c < notMatched.Count; c++)
                {
                    fout.WriteLine(notMatched[c]);
                }
            }

            using (StreamWriter fout = File.CreateText("streetMatched.txt"))
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
            int edit = EditDistance.Compute("GRANDCONCOURSE", "GRAND CONCOURSE");
            bool yes = StringUtility.IsDistance1OrLessApart("GRANDCONCOURSE", "GRAND CONCOURSE");

            GetCleanedNYStreetList2();

            return;
        }
    }
}
