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
        static void TestTransitiveClosure()
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
        }

        static void TestOffByNSocial()
        {
            row[] rows = new row[9];

            rows[0] = new row { SSN = 123456789, EnterpriseID = 0 };
            rows[1] = new row { SSN = 123446789, EnterpriseID = 1 };
            rows[2] = new row { SSN = 123446799, EnterpriseID = 2 };
            rows[3] = new row { SSN = 555555555, EnterpriseID = 3 };
            rows[4] = new row { SSN = 666666666, EnterpriseID = 4 };
            rows[5] = new row { SSN = 666667666, EnterpriseID = 5 };
            rows[6] = new row { SSN = 666668666, EnterpriseID = 6 };
            rows[7] = new row { SSN = 555555555, EnterpriseID = 7 };
            rows[8] = new row { SSN = -1, EnterpriseID = 8 };

            Dictionary<int, List<int>> ret = OffByNSocialSecurityNumbers.Find(rows, 1); 

            foreach(int key in ret.Keys)
            {
                List<int> offs = ret[key];

                Console.WriteLine($"Social # {rows[key].SSN} off by one for..."); 
                foreach(int offKey in offs)
                {
                    Console.WriteLine($"\t{rows[offKey].SSN}");
                }
            }
        }

        static void Main(string[] args)
        {
            TestOffByNSocial(); 

            return;
        }
    }
}
