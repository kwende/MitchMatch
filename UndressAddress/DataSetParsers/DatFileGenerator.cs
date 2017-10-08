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
            Data data = DataLoader.LoadData(false);

            Dictionary<StreetName, List<int>> zipCodes = new Dictionary<StreetName, List<int>>();
            Dictionary<StreetName, List<string>> cities = new Dictionary<StreetName, List<string>>();

            const int PreTypeColumn = 9;
            const int StreetNameColumn = 11;
            const int StreetSuffixColumn = 12;

            const int ZipLeftColumn = 33;
            const int ZipRightColumn = 34;

            const int CityLeftColumn = 35;
            const int CityRightColumn = 36;

            string[] allLines = File.ReadAllLines(filePath).Skip(1).ToArray();

            Parallel.ForEach(allLines, line =>
            {
                string[] lineBits = line.Split(',').Select(n => n.Trim()).ToArray();

                string preType = lineBits[PreTypeColumn].ToUpper();
                string streetName = lineBits[StreetNameColumn].ToUpper();
                if (streetName != "DRIVEWAY" && !streetName.Contains("UNNAMED"))
                {
                    string streetSuffix = lineBits[StreetSuffixColumn].ToUpper();

                    for (int c = 0; c < data.Suffixes.ShortSuffixes.Length; c++)
                    {
                        if (data.Suffixes.ShortSuffixes[c] == streetSuffix)
                        {
                            streetSuffix = data.Suffixes.LongSuffixes[c];
                        }
                    }

                    int zipLeft = 0, zipRight = 0;

                    int.TryParse(lineBits[ZipLeftColumn], out zipLeft);
                    int.TryParse(lineBits[ZipRightColumn], out zipRight);

                    string cityLeft = lineBits[CityLeftColumn].ToUpper();
                    string cityRight = lineBits[CityRightColumn].ToUpper();

                    StreetName name = new StreetName(preType, streetName, streetSuffix, null, null);

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
                string cleanedName = key.Name;
                cleanedName = Regex.Replace(cleanedName, @"(\d+)(TH|ST|ND|RD)", "$1");

                StreetName newStreetName = new StreetName(key.PreType, cleanedName,
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

            BKTree citiesTree = BKTreeEngine.CreateBKTree(uniqueCities.ToList());

            bf = new BinaryFormatter();
            using (FileStream fout = File.Create("c:/users/brush/desktop/citiesBKTree.dat"))
            {
                bf.Serialize(fout, citiesTree);
            }

            return allStreetNames;
        }
    }
}
