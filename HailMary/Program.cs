using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HailMary
{
    class Program
    {
        private static long[] BadPhones =
        {
            -1,
            0000000000,
            1111111111,
            2222222222,
            3333333333,
            4444444444,
            5555555555,
            6666666666,
            7777777777,
            8888888888,
            9999999999,
            1234567890,
        };

        static void Main2()
        {
            string[] allLines = File.ReadAllLines("c:/users/brush/desktop/FinalDataSet.csv").Skip(1).ToArray();

            string[] submission01 = File.ReadAllLines("c:/users/brush/desktop/submission01.csv");

            List<string> originalSubmissions = new List<string>();

            foreach (string line in submission01)
            {
                string[] parts = line.Split(',');

                int p1 = int.Parse(parts[0]);
                int p2 = int.Parse(parts[1]);

                if (p1 < p2)
                {
                    originalSubmissions.Add($"{p1},{p2}");
                }
                else
                {
                    originalSubmissions.Add($"{p2},{p1}");
                }
            }

            const int Phone1Field = 15;
            const int Phone2Field = 16;

            Dictionary<string, List<int>> phone1Key = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> phone2Key = new Dictionary<string, List<int>>();

            int count = 0;
            foreach (string line in allLines)
            {
                count++;

                if (count % 10000 == 0)
                {
                    Console.WriteLine($"{count}/{allLines.Length}");
                }

                string[] parts = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);

                if (parts[0] != "")
                {
                    int eid = int.Parse(parts[0]);

                    string phone1 = parts[Phone1Field].Replace("-", "");
                    string phone2 = parts[Phone2Field].Replace("-", "");
                    string phone3 = "";

                    if (phone2.Contains("^^"))
                    {
                        string[] partsToPhone2 = phone2.Split(new string[] { "^^" }, StringSplitOptions.None);

                        phone2 = partsToPhone2[0];
                        phone3 = partsToPhone2[1];
                    }

                    if (!string.IsNullOrEmpty(phone1))
                    {
                        if (!phone1Key.ContainsKey(phone1))
                        {
                            phone1Key.Add(phone1, new List<int>());
                        }

                        phone1Key[phone1].Add(eid);
                    }

                    if (!string.IsNullOrEmpty(phone2))
                    {
                        if (!phone2Key.ContainsKey(phone2))
                        {
                            phone2Key.Add(phone2, new List<int>());
                        }

                        phone2Key[phone2].Add(eid);
                    }

                    if (!string.IsNullOrEmpty(phone3))
                    {
                        if (!phone2Key.ContainsKey(phone3))
                        {
                            phone2Key.Add(phone3, new List<int>());
                        }

                        phone2Key[phone3].Add(eid);
                    }
                }
            }

            Console.Write("A");

            string[] key1 = phone1Key.Keys.ToArray();
            foreach (string key in key1)
            {
                phone1Key[key] = phone1Key[key].Distinct().ToList();
            }

            string[] key2 = phone2Key.Keys.ToArray();
            foreach (string key in key2)
            {
                phone2Key[key] = phone2Key[key].Distinct().ToList();
            }

            Console.Write("B");

            List<int> eidsWithPhone1AlsoInPhone2 = new List<int>();

            List<string> pairs = new List<string>();
            List<string> pairs2 = new List<string>();

            foreach (string key in phone1Key.Keys)
            {
                if (phone2Key.ContainsKey(key))
                {
                    int[] phone1Eids = phone1Key[key].ToArray();
                    int[] phone2Eids = phone2Key[key].ToArray();

                    foreach (int phone1Eid in phone1Eids)
                    {
                        foreach (int phone2Eid in phone2Eids)
                        {
                            if (phone1Eid != phone2Eid &&
                                !phone1Eids.Contains(phone2Eid))
                            {
                                string toWrite = null;

                                if (phone1Eid < phone2Eid)
                                {
                                    toWrite = $"{phone1Eid},{phone2Eid}";
                                }
                                else
                                {
                                    toWrite = $"{phone2Eid},{phone1Eid}";
                                }

                                if (!originalSubmissions.Contains(toWrite))
                                {
                                    pairs.Add(toWrite + ",1");
                                }
                            }
                        }
                    }
                }
            }

            Console.Write("C");

            foreach (string key in phone2Key.Keys)
            {
                List<int> eids = phone2Key[key];
                List<int> compareEids = new List<int>();

                if (phone1Key.ContainsKey(key))
                {
                    compareEids = phone1Key[key];
                }

                foreach (int eid1 in eids)
                {
                    foreach (int eid2 in eids)
                    {
                        if (eid1 != eid2 && !compareEids.Contains(eid1) &&
                            !compareEids.Contains(eid1))
                        {
                            string toWrite = null;

                            if (eid1 < eid2)
                            {
                                toWrite = $"{eid1},{eid2}";
                            }
                            else
                            {
                                toWrite = $"{eid2},{eid1}";
                            }

                            if (!originalSubmissions.Contains(toWrite))
                            {
                                pairs2.Add(toWrite + ",1");
                            }
                        }
                    }
                }
            }

            Console.Write("D");

            pairs = pairs.Distinct().ToList();
            pairs2 = pairs2.Distinct().ToList();

            File.WriteAllLines("C:/users/brush/desktop/phone1_phone2.txt",
                    pairs.ToArray());
            File.WriteAllLines("C:/users/brush/desktop/phone2.txt",
                pairs2.ToArray());

            //BinaryFormatter bf = new BinaryFormatter();
            //using (FileStream fout = File.Create("c:/users/brush/desktop/phone1Key.dat"))
            //{
            //    bf.Serialize(fout, phone1Key);
            //}

            //bf = new BinaryFormatter();
            //using (FileStream fout = File.Create("c:/users/brush/desktop/phone2Key.dat"))
            //{
            //    bf.Serialize(fout, phone2Key);
            //}


            //Dictionary<string, int> numbers = new Dictionary<string, int>();

            //foreach (string line in allLines)
            //{
            //    string[] parts = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);

            //    if (parts[8].Contains("219 EAST 121"))
            //    {
            //        string phone = parts[15].Replace("-", "");

            //        if (!numbers.ContainsKey(phone))
            //        {
            //            numbers.Add(phone, 0);
            //        }

            //        numbers[phone] = numbers[phone] + 1;
            //    }
            //}

            //KeyValuePair<string,int>[] nums = numbers.Where(n => n.Value > 1).ToArray();

            //using (StreamWriter sw = File.CreateText("C:/users/brush/desktop/phones.csv"))
            //{
            //    foreach (KeyValuePair<string, int> pair in nums)
            //    {
            //        sw.WriteLine($"{pair.Key},{pair.Value}");
            //    }
            //}


            //List<string> newLines = new List<string>();

            //for (int c = 0; c < allLines.Length; c++)
            //{
            //    newLines.Add($"{c},{allLines[c]}");
            //}

            //File.WriteAllLines("d:/alternates2.csv", newLines.ToArray());


            //Dictionary<int, List<string[]>> linesByEid = new Dictionary<int, List<string[]>>();

            //foreach (string line in allLines)
            //{
            //    string[] lines = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);

            //    int eid = int.Parse(lines[0]);

            //    if (!linesByEid.ContainsKey(eid))
            //    {
            //        linesByEid.Add(eid, new List<string[]>());
            //    }

            //    linesByEid[eid].Add(lines);

            //}
            //int maxNumber = linesByEid.Max(n => n.Value.Count);

            //const int AddressColumn = 9;
            //const int ZipColumn = 11;
            //const int CityColumn = 14;

            ////List<string> matches = new List<string>();
            //Dictionary<string, string> matches = new Dictionary<string, string>();

            //int counter = 0;
            ////foreach (KeyValuePair<int, List<string[]>> group in linesByEid)
            //List<KeyValuePair<int, List<string[]>>> all = linesByEid.ToList();

            //Parallel.For(0, all.Count, c =>
            //{
            //    KeyValuePair<int, List<string[]>> group = all[c];

            //    Interlocked.Increment(ref counter);

            //    if (counter % 100 == 0)
            //    {
            //        Console.WriteLine($"{counter}/{linesByEid.Count}: {((counter / (linesByEid.Count * 1.0f)) * 100).ToString("000.00")}: {matches.Count}");
            //    }

            //    int key = group.Key;
            //    List<string[]> values = group.Value;

            //    foreach (string[] value in values)
            //    {
            //        string addressValue = value[AddressColumn];
            //        if (addressValue != "UNKNOWN" && addressValue != "HOMELESS")
            //        {
            //            int id = int.Parse(value[0]);
            //            string zipValue = value[ZipColumn];
            //            string cityValue = value[CityColumn];

            //            //foreach (KeyValuePair<int, List<string[]>> otherGroup in linesByEid)
            //            for (int d = c + 1; d < all.Count; d++)
            //            {
            //                KeyValuePair<int, List<string[]>> otherGroup = all[d];

            //                int otherKey = otherGroup.Key;
            //                List<string[]> otherValues = otherGroup.Value;

            //                if (key != otherKey)
            //                {
            //                    foreach (string[] otherValue in otherValues)
            //                    {
            //                        string otherAddressValue = otherValue[AddressColumn];

            //                        if (otherAddressValue != "UNKNOWN" && otherAddressValue != "HOMELESS")
            //                        {
            //                            int otherId = int.Parse(otherValue[0]);
            //                            string otherZipValue = otherValue[ZipColumn];
            //                            string otherCityValue = otherValue[CityColumn];

            //                            if (addressValue == otherAddressValue &&
            //                                zipValue == otherZipValue &&
            //                                cityValue == otherCityValue)
            //                            {
            //                                lock (matches)
            //                                {
            //                                    string hashString = "";
            //                                    if (key < otherKey)
            //                                    {
            //                                        hashString = $"{key}_{otherKey}";
            //                                    }
            //                                    else
            //                                    {
            //                                        hashString = $"{otherKey}_{key}";
            //                                    }

            //                                    if (!matches.ContainsKey(hashString))
            //                                    {
            //                                        matches.Add(hashString, string.Join(",", otherValue) + "\n" + string.Join(",", value) + "\n" + "\n");
            //                                    }

            //                                    //matches = matches.Distinct().ToList();
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //});

            //File.WriteAllLines("c:/user/brush/desktop/matches.txt", matches.Values.Distinct().ToArray());
        }

        static void Main(string[] args)
        {
            string[] allLines = File.ReadAllLines("c:/users/brush/desktop/FinalDataSet.csv").Skip(1).ToArray();

            string[] submission01 = File.ReadAllLines("c:/users/brush/desktop/submission01.csv");

            List<string> originalSubmissions = new List<string>();

            foreach (string line in submission01)
            {
                string[] parts = line.Split(',');

                int p1 = int.Parse(parts[0]);
                int p2 = int.Parse(parts[1]);

                if (p1 < p2)
                {
                    originalSubmissions.Add($"{p1},{p2}");
                }
                else
                {
                    originalSubmissions.Add($"{p2},{p1}");
                }
            }

            Dictionary<string, List<int>> aliasEids = new Dictionary<string, List<int>>();

            foreach (string line in allLines)
            {
                string[] parts = DecisionTreeLearner.Data.DataLoader.SmartSplit(line);

                if (parts[0] != "")
                {
                    string alias = parts[18].Trim(); 
                    if (!string.IsNullOrEmpty(alias) && alias != "JOHN DOE" && alias != "JANE DOE")
                    {
                        if (!aliasEids.ContainsKey(alias))
                        {
                            aliasEids.Add(alias, new List<int>());
                        }

                        aliasEids[alias].Add(int.Parse(parts[0]));
                    }
                }

            }

            string[] keys = aliasEids.Keys.ToArray();
            foreach (string key in keys)
            {
                aliasEids[key] = aliasEids[key].Distinct().ToList();
            }

            Console.Write("B");

            int counter = 0;
            List<string> pairs = new List<string>();
            foreach (KeyValuePair<string, List<int>> aliasWithEids in aliasEids)
            {
                counter++;

                if (counter % 100 == 0)
                {
                    Console.WriteLine($"{counter}/{aliasEids.Count}");
                }
                List<int> eids = aliasWithEids.Value;

                //Console.WriteLine("\t" + eids.Count + ": " + aliasWithEids.Key); 

                foreach (int eid1 in eids)
                {
                    foreach (int eid2 in eids)
                    {
                        if (eid1 != eid2)
                        {
                            string toWrite = null;

                            if (eid1 < eid2)
                            {
                                toWrite = $"{eid1},{eid2}";
                            }
                            else
                            {
                                toWrite = $"{eid2},{eid1}";
                            }

                            if (!originalSubmissions.Contains(toWrite))
                            {
                                pairs.Add(toWrite + ",1");
                            }
                        }
                    }
                }
            }
            File.WriteAllLines("c:/users/brush/desktop/alias.csv",
                pairs.ToArray());
        }
    }
}
