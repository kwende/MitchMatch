using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class PrintingLibrary
    {
        private static int _printCount = 0;

        public static void Print(Row row)
        {
            string ssn = row.SSN <= 0 ? "   -  -    " : string.Format("{0:000-00-0000}", row.SSN);
            string phone = row.PHONE <= 0 ? "   -   -    " : string.Format("{0:000-000-0000}", row.PHONE);
            Console.WriteLine("{0,-15} {1, -15} {2,-20} {3, -3} AKA {14, -25} ({4,-1}): S:{5}  {6}  P:{7}. {9,25}, {10,5}, {11,10}, {12,2}, {13,5}. {17, -10}, {18}",
                row.FIRST,
                row.MIDDLE,
                row.LAST,
                row.SUFFIX,
                row.GENDER,
                ssn,
                row.DOB.ToString("dd/MM/yyyy"),
                phone,
                row.PHONE2,//
                row.ADDRESS1,
                row.ADDRESS2,
                row.CITY,
                row.STATE,
                row.ZIP,
                row.ALIAS,
                row.MOTHERS_MAIDEN_NAME,//
                row.EMAIL,//
                row.MRN,
                row.EnterpriseID);
        }

        private static void PrintCheckCount()
        {
            if ((++_printCount + 25) % 50 == 0)
            {
                Console.WriteLine("Press enter to continue...");
                string answer = Console.ReadLine();
            }
        }
        public static void PrintRows(IEnumerable<Row> rows)
        {
            foreach (Row row in rows)
            {
                Print(row);
            }
            Console.WriteLine();
            PrintCheckCount();
        }

        public static void PrintPair(Row a, Row b)
        {
            Print(a);
            Print(b);
            Console.WriteLine();
            PrintCheckCount();
        }

        public static void PrintRemainingRowCount(Row[] data, ClosedSets matches)
        {
            int remaining = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (matches.RowToClosedRowSet[i].Count == 1)
                {
                    remaining++;
                }
            }
            Console.WriteLine($"Remaining: {remaining}");
        }
    }
}
