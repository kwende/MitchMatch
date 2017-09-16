using DecisionTreeLearner.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress
{
    class Program
    {
        static void GenerateSlimmedDownFile()
        {
            string[] abbreviations = File.ReadAllLines("StreetSuffixes.csv").Select(n => n.Split(',')[1]).ToArray();

            IEnumerable<string> lines = File.ReadLines(@"C:\Users\ben\Desktop\city_of_new_york.csv");

            List<string> streets = new List<string>(), shortenedStreets = new List<string>();
            bool header = false;
            foreach (string line in lines)
            {
                if (!header)
                {
                    header = true;
                }
                else if (header)
                {
                    string[] bits = line.Split(',').Select(n => n.Trim()).ToArray();
                    string streetName = bits[3];
                    string originalStreetName = streetName;
                    if (!streets.Contains(originalStreetName))
                    {
                        foreach (string abbreviation in abbreviations)
                        {
                            int removeAt = streetName.IndexOf(" " + abbreviation);
                            if (removeAt != -1)
                            {
                                streetName = streetName.Substring(0, removeAt);
                            }
                        }
                        streets.Add(originalStreetName);
                        shortenedStreets.Add(streetName);
                    }
                }
            }

            using (StreamWriter sw = File.CreateText("C:/users/ben/desktop/justStreets.csv"))
            {
                foreach (string street in shortenedStreets)
                {
                    sw.WriteLine(street);
                }
            }

            Console.WriteLine($"Picked up {streets.Count} streets.");
        }



        static void Main(string[] args)
        {
            //GenerateSlimmedDownFile(); 

            IEnumerable<string> lines = File.ReadLines("c:/users/ben/desktop/finaldataset.csv");
            string[] addressBits = File.ReadAllLines("c:/users/ben/desktop/justStreets.csv").Select(n => " " + n).ToArray();

            List<string> outcasts = new List<string>();
            int lineNum = 0;
            foreach (string line in lines)
            {
                if(lineNum%1000 == 0)
                {
                    Console.WriteLine($"{lineNum}");
                }
                lineNum++;
                string[] bits = DataLoader.SmartSplit(line);

                string address = bits[8];

                bool matched = false;
                foreach (string addressBit in addressBits)
                {
                    if (address.Contains(addressBit))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched && !outcasts.Contains(address))
                {
                    outcasts.Add(address);
                }
            }

            using (StreamWriter sw = File.CreateText("C:/users/ben/desktop/outcasts.txt"))
            {
                foreach (string outcast in outcasts)
                {
                    sw.WriteLine(outcast);
                }
            }
        }
    }
}
