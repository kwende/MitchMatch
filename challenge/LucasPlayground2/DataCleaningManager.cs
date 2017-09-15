using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground2
{
    public class DataCleaningManager
    {
        public static void CleanData(ref Row[] data, Row[] realData)
        {
            // Clean Data
            var badSSNs = realData.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            DateTime badDOB = new DateTime(1, 1, 1);
            var badPhones = realData.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();
            var badAddresses = realData.GroupBy(r => r.ADDRESS1).Where(g => !g.Key.Contains(' ') && g.Count() > 2).Select(g => g.Key).ToArray();


            var badSSNs2 = data.GroupBy(r => r.SSN).Where(g => g.Count() >= 4).Select(g => g.Key).ToArray();
            var badPhones2 = data.GroupBy(r => r.PHONE).Where(g => g.Count() >= 5).Select(g => g.Key).ToArray();
            var badAddresses2 = data.GroupBy(r => r.ADDRESS1).Where(g => !g.Key.Contains(' ')).Select(g => g.Key).ToArray();

            Console.WriteLine(badSSNs2.Count() + badPhones2.Count() + badAddresses2.Count());

            foreach (Row row in data)
            {
                if (badSSNs.Contains(row.SSN))
                {
                    row.SSN = 0;
                }
                if (badDOB.Equals(row.DOB))
                {
                    row.DOB = default(DateTime);
                }
                if (badPhones.Contains(row.PHONE))
                {
                    row.PHONE = 0;
                }
                if (badAddresses.Contains(row.ADDRESS1))
                {
                    row.ADDRESS1 = "";
                }
            }
        }
    }
}
