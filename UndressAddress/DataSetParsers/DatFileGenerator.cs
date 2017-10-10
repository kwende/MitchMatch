using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndressAddress.DataTypes;

namespace UndressAddress.DataSetParsers
{
    public static class DatFileGenerator
    {
        public static List<StreetName> Generate(string filePath)
        {
            Data data = DataLoader.LoadJustSuffixes();

            Dictionary<StreetName, List<int>> zipCodes = new Dictionary<StreetName, List<int>>();
            Dictionary<StreetName, List<string>> cities = new Dictionary<StreetName, List<string>>();

            Dictionary<StreetNameAndCity, List<int>> streetNameCity2Zips = new Dictionary<StreetNameAndCity, List<int>>();
            Dictionary<StreetNameAndZip, List<string>> streetNameZip2Cities = new Dictionary<StreetNameAndZip, List<string>>();

            const int PreTypeColumn = 9;
            const int StreetNameColumn = 11;
            const int StreetSuffixColumn = 12;

            const int ZipLeftColumn = 33;
            const int ZipRightColumn = 34;

            const int CityLeftColumn = 35;
            const int CityRightColumn = 36;

            const int CityLeftAlternate = 37;
            const int CityRightAlternate = 38;

            const int PreDirectionColumn = 8;

            string[] allLines = File.ReadAllLines(filePath).Skip(1).ToArray();

            Parallel.ForEach(allLines, line =>
            {
                string[] lineBits = line.Split(',').Select(n => n.Trim()).ToArray();

                string preType = lineBits[PreTypeColumn].ToUpper();
                string streetName = lineBits[StreetNameColumn].ToUpper();
                if (streetName != "DRIVEWAY" && !streetName.Contains("UNNAMED"))
                {
                    string streetSuffix = lineBits[StreetSuffixColumn].ToUpper();

                    for (int c = 0; c < data.Suffixes.LongSuffixes.Length; c++)
                    {
                        if (data.Suffixes.LongSuffixes[c] == streetSuffix)
                        {
                            streetSuffix = data.Suffixes.ShortSuffixes[c];
                        }
                    }

                    int zipLeft = 0, zipRight = 0;

                    int.TryParse(lineBits[ZipLeftColumn], out zipLeft);
                    int.TryParse(lineBits[ZipRightColumn], out zipRight);


                    string cityLeft = lineBits[CityLeftColumn].ToUpper();
                    string cityRight = lineBits[CityRightColumn].ToUpper();

                    if (string.IsNullOrEmpty(cityLeft))
                    {
                        cityLeft = lineBits[CityLeftAlternate].ToUpper();
                    }
                    if (string.IsNullOrEmpty(cityRight))
                    {
                        cityRight = lineBits[CityRightAlternate].ToUpper();
                    }

                    string preDirection = lineBits[PreDirectionColumn].ToUpper();

                    if (preDirection == "E")
                    {
                        preDirection = "EAST";
                    }
                    else if (preDirection == "W")
                    {
                        preDirection = "WEST";
                    }
                    else if (preDirection == "N")
                    {
                        preDirection = "NORTH";
                    }
                    else if (preDirection == "S")
                    {
                        preDirection = "SOUTH";
                    }

                    string cleanedName = streetName;
                    cleanedName = Regex.Replace(cleanedName, @"(\d+)(TH|ST|ND|RD)", "$1");

                    StreetName name = new StreetName(preDirection, preType, cleanedName, streetSuffix, null, null);


                    List<int> localZips = new List<int>();
                    if (zipLeft != 0) localZips.Add(zipLeft);
                    if (zipRight != 0) localZips.Add(zipRight);

                    List<string> localCities = new List<string>();
                    if (!string.IsNullOrEmpty(cityLeft)) localCities.Add(cityLeft);
                    if (!string.IsNullOrEmpty(cityRight)) localCities.Add(cityRight);

                    lock (streetNameCity2Zips)
                    {
                        string fullStreetName = Regex.Replace(name.FullStreetName, @"(\d+)(TH|ST|ND|RD)", "$1");

                        if (zipLeft != 0 && !string.IsNullOrEmpty(cityLeft))
                        {
                            StreetNameAndCity key1 = new StreetNameAndCity
                            {
                                City = cityLeft,
                                FullStreetName = fullStreetName,
                            };

                            if (!streetNameCity2Zips.ContainsKey(key1))
                            {
                                streetNameCity2Zips.Add(key1, new List<int>());
                            }

                            streetNameCity2Zips[key1].Add(zipLeft);
                            streetNameCity2Zips[key1] = streetNameCity2Zips[key1].Distinct().ToList();

                            StreetNameAndZip key2 = new StreetNameAndZip
                            {
                                FullStreetName = fullStreetName,
                                Zip = zipLeft,
                            };

                            if (!streetNameZip2Cities.ContainsKey(key2))
                            {
                                streetNameZip2Cities.Add(key2, new List<string>());
                            }

                            streetNameZip2Cities[key2].Add(cityLeft);

                            streetNameZip2Cities[key2] = streetNameZip2Cities[key2].Distinct().ToList();
                        }

                        if (zipRight != 0 && !string.IsNullOrEmpty(cityRight))
                        {
                            StreetNameAndCity key1 = new StreetNameAndCity
                            {
                                City = cityRight,
                                FullStreetName = fullStreetName,
                            };

                            if (!streetNameCity2Zips.ContainsKey(key1))
                            {
                                streetNameCity2Zips.Add(key1, new List<int>());
                            }

                            streetNameCity2Zips[key1].Add(zipRight);
                            streetNameCity2Zips[key1] = streetNameCity2Zips[key1].Distinct().ToList();

                            StreetNameAndZip key2 = new StreetNameAndZip
                            {
                                FullStreetName = fullStreetName,
                                Zip = zipRight,
                            };

                            if (!streetNameZip2Cities.ContainsKey(key2))
                            {
                                streetNameZip2Cities.Add(key2, new List<string>());
                            }

                            streetNameZip2Cities[key2].Add(cityRight);
                            streetNameZip2Cities[key2] = streetNameZip2Cities[key2].Distinct().ToList();
                        }
                    }

                    lock (zipCodes)
                    {
                        if (!zipCodes.ContainsKey(name))
                        {
                            zipCodes.Add(name, new List<int>());
                        }

                        if (zipLeft != 0)
                        {
                            zipCodes[name].Add(zipLeft);
                        }
                        if (zipRight != 0 && zipLeft != zipRight)
                        {
                            zipCodes[name].Add(zipRight);
                        }
                    }

                    lock (cities)
                    {
                        if (!cities.ContainsKey(name))
                        {
                            cities.Add(name, new List<string>());
                        }

                        if (!string.IsNullOrEmpty(cityLeft))
                        {
                            cities[name].Add(cityLeft);
                        }
                        if (!string.IsNullOrEmpty(cityRight) && cityRight != cityLeft)
                        {
                            cities[name].Add(cityRight);
                        }
                    }
                }
            });

            List<StreetName> allStreetNames = new List<StreetName>();

            StreetName[] keys = zipCodes.Keys.ToArray();

            foreach (StreetName key in keys)
            {

                StreetName newStreetName = new StreetName(key.PreDirection, key.PreType, key.Name,
                    key.Suffix, zipCodes[key].Distinct().ToList(), cities[key].Distinct().ToList());

                allStreetNames.Add(newStreetName);
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream sw = File.Create("c:/users/brush/desktop/streetNames.dat"))
            {
                bf.Serialize(sw, allStreetNames);
            }

            string[] uniqueCities = allStreetNames.SelectMany(n => n.Cities).Distinct().ToArray();

            File.WriteAllLines("C:/users/brush/desktop/knownCities.csv",
                uniqueCities);

            string[] uniqueStreets = allStreetNames.Select(n => n.Name).Distinct().ToArray();

            File.WriteAllLines("C:/users/brush/desktop/knownStreets.csv",
                uniqueStreets);

            BKTree citiesTree = BKTreeEngine.CreateBKTree(uniqueCities.ToList());
            BKTreeSerializer.SerializeTo(citiesTree, "c:/users/brush/desktop/citiesBKTree.dat");

            BKTree streetsTree = BKTreeEngine.CreateBKTree(uniqueStreets.ToList());
            BKTreeSerializer.SerializeTo(streetsTree, "c:/users/brush/desktop/streetsBKTree.dat");

            bf = new BinaryFormatter();
            using (FileStream fw = File.Create("C:/users/brush/desktop/streetNameCity2Zips.dat"))
            {
                bf.Serialize(fw, streetNameCity2Zips);
            }

            bf = new BinaryFormatter();
            using (FileStream fw = File.Create("C:/users/brush/desktop/streetNameZip2Cities.dat"))
            {
                bf.Serialize(fw, streetNameZip2Cities);
            }


            return allStreetNames;
        }
    }
}
