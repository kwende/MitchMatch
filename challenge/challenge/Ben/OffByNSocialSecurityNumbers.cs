using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class OffByNSocialSecurityNumbers
    {
        public static Dictionary<int, List<int>> Find(row[] rows, int n)
        {
            Dictionary<int, List<int>> sets = new Dictionary<int, List<int>>();

            int count = 0; 
            foreach (row row1 in rows)
            {
                List<int> offByN = new List<int>(); 
                foreach (row row2 in rows)
                {
                    if (row1 != row2 && row1.SSN > 1 && row2.SSN > 1)
                    {
                        string row1SSNString = row1.SSN.ToString();
                        string row2SSNString = row2.SSN.ToString();

                        int numberOff = 0; 
                        for (int c = 0; c < row1SSNString.Length; c++)
                        {
                            if(row1SSNString[c] != row2SSNString[c])
                            {
                                numberOff++; 
                            }

                            if(numberOff > n)
                            {
                                break; 
                            }
                        }

                        if(numberOff == n)
                        {
                            offByN.Add(row2.EnterpriseID); 
                        }
                    }
                }
                if(offByN.Count > 0)
                {
                    sets.Add(row1.EnterpriseID, offByN);
                }
                count++;
                if(count % 100==0)
                {
                    Console.WriteLine($"{count}/{rows.Length}");
                }
            }

            return sets; 
        }
    }
}
