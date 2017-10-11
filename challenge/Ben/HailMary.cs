using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class HailMary
    {
        private Dictionary<int, int> _eidLookup = new Dictionary<int, int>();

        private static string[] SmartSplit(string csvLine)
        {
            List<int> separatorIndices = new List<int>();
            bool withinQuotes = false;
            for (int c = 0; c < csvLine.Length; c++)
            {
                if (csvLine[c] == '"')
                {
                    withinQuotes = !withinQuotes;
                }
                else if (csvLine[c] == ',' && !withinQuotes)
                {
                    separatorIndices.Add(c);
                }
            }

            string[] ret = new string[separatorIndices.Count + 1];
            int startIndex = 0;
            for (int c = 0; c < separatorIndices.Count; c++)
            {
                int separatorIndex = separatorIndices[c];
                ret[c] =
                    csvLine.Substring(startIndex, separatorIndex - startIndex);
                startIndex += ret[c].Length + 1;
            }
            ret[ret.Length - 1] = csvLine.Substring(startIndex);

            return ret.Select(n => n.Trim()).ToArray();
        }

        public List<string> LoadFromAlternatesFile(string alternatesFile, int largestRealEid)
        {
            List<string> ret = new List<string>();
            _eidLookup.Clear();

            string[] allLinesFromAlternate = File.ReadAllLines(alternatesFile);

            int fauxEid = largestRealEid;
            foreach (string lineFromAlternate in allLinesFromAlternate)
            {
                string[] parts = SmartSplit(lineFromAlternate);

                int eid = int.Parse(parts[0]);
                fauxEid++;

                _eidLookup.Add(fauxEid, eid);
                parts[0] = fauxEid.ToString();

                ret.Add(string.Join(",", parts));
            }

            return ret;
        }

        public int TranslateEID(int toTranslate)
        {
            if (_eidLookup.ContainsKey(toTranslate))
            {
                return _eidLookup[toTranslate];
            }
            else
            {
                return toTranslate;
            }
        }

        public List<List<int>> Collapse(List<List<int>> input)
        {
            List<List<int>> toProcess = new List<List<int>>();
            for (int c = 0; c < input.Count; c++)
            {
                List<int> converted = input[c].Select(n => TranslateEID(n)).Distinct().ToList();
                toProcess.Add(converted);
            }

            return toProcess;
            //List<int> markedForDeletion = new List<int>();

            ////for (int c = 0; c < input.Count; c++)
            //int count = 0;
            //Parallel.For(0, input.Count, c =>
            //{
            //    Interlocked.Increment(ref count);
            //    if (c % 1000 == 0)
            //    {
            //        Console.WriteLine($"{count}/{input.Count} : {markedForDeletion.Count}");
            //    }

            //    List<int> toSearchFor = input[c];

            //    for (int d = c + 1; d < input.Count; d++)
            //    {
            //        List<int> toSearchWithin = input[d];

            //        bool isDifferent = false;
            //        foreach (int i in toSearchFor)
            //        {
            //            if (!toSearchWithin.Contains(i))
            //            {
            //                isDifferent = true;
            //                break;
            //            }
            //        }

            //        if (!isDifferent)
            //        {
            //            lock (markedForDeletion)
            //            {
            //                markedForDeletion.Add(d);
            //            }
            //        }
            //    }
            //});

            //foreach (int toDeleteIndex in markedForDeletion)
            //{
            //    input.RemoveAt(toDeleteIndex);
            //}

            return input;
        }
    }
}
