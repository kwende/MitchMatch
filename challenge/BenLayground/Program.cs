using challenge;
using challenge.Ben;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenLayground
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<int, List<int>> matches = new Dictionary<int, List<int>>();

            row[] rows = new row[7];
            for (int c = 0; c < rows.Length; c++)
            {
                rows[c] = new row { EnterpriseID = c };
            }

            matches.Add(0, new List<int>(new int[] { 1, 2, 3 }));
            matches.Add(1, new List<int>(new int[] { 0 }));
            matches.Add(2, new List<int>(new int[] { 0 }));
            matches.Add(3, new List<int>(new int[] { 0, 4 }));
            matches.Add(4, new List<int>(new int[] { 3 }));
            matches.Add(5, new List<int>(new int[] { 6 }));
            matches.Add(6, new List<int>(new int[] { 5 })); 

            TransitiveClosure closure = TransitiveClosure.Compute(matches, rows);
            row[] shouldHave5 = closure.FindClosedSetForRow(rows[0]);
            row[] shouldHave2 = closure.FindClosedSetForRow(rows[5]);


            return;
            ////// 
        }
    }
}
