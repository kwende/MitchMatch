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
        static void Main(string[] args)
        {
            string[] abbreviations = File.ReadAllLines("StreetSuffixes.csv").Select(n => n.Split(',')[1]).ToArray();

            IEnumerable<string> lines = File.ReadLines(@"C:\Users\brush\Desktop\city_of_new_york.csv");

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

            using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/justStreets.csv"))
            {
                foreach (string street in shortenedStreets)
                {
                    sw.WriteLine(street);
                }
            }

            Console.WriteLine($"Picked up {streets.Count} streets.");
        }
    }
}
