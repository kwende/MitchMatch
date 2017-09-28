using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge.Common
{
    public static class FastFuzzyDateGrouper
    {
        //RowMatchObject 

        //Duplicating this:
        //public static bool FuzzyDateEquals(DateTime a, DateTime b)
        //{
        //    if (a == default(DateTime) || b == default(DateTime))
        //        return false;

        //    if (a.Month == b.Day && a.Day == b.Month && a.Year == b.Year)
        //        return true;
        //    if (a.Month == b.Month && a.Year == b.Year && (OneOrOneDigit(a.Day, b.Day) || TransposedDigit(a.Day, b.Day)))
        //        return true;
        //    if (a.Day == b.Day && a.Year == b.Year && (OneOrOneDigit(a.Month, b.Month) || TransposedDigit(a.Month, b.Month)))
        //        return true;
        //    if (a.Month == b.Month && a.Day == b.Day && (OneOrOneDigit(a.Year, b.Year) || TransposedDigit(a.Year, b.Year) || OffBy100(a.Year, b.Year)))
        //        return true;

        //    return false;
        //}

        //Matches Match(DateTime[] dates)
        //{
        //    Matches toReturn = new Matches(dates.Length);

        //    var dateIndices = dates.Select((d, i) => new DateIndex { Date = d, Index = i }).ToArray();

        //    //First matches that require year
        //    var groupedByYear = dateIndices.GroupBy(d => d.Date.Year);
        //    foreach(var group in groupedByYear)
        //    {

        //    }

        //}

        //DateTime[] Neighborhood(DateTime date)
        //{
        //    List<DateTime> toReturn = new List<DateTime>();

        //    int y = date.Year;
        //    int m = date.Month;
        //    int d = date.Day;
        //    int daysInMonth = DateTime.DaysInMonth(y, m);

        //    toReturn.Add(date);

        //    //Transpose day and month
        //    toReturn.Add(new DateTime(y, d, m));

        //    //Off by one day
        //    if (d < daysInMonth) toReturn.Add(new DateTime(y, m, d + 1));
        //    if (d > 1) toReturn.Add(new DateTime(y, m, d - 1));
            
        //}

        //static List<int> OffByOneDigit(int n)
        //{
        //    List<int> toReturn = new List<int>();
        //    string s = n.ToString();
        //    for (int i = 0; i < s.Length; i++)
        //    {
        //        for (int d = 0; d < )
        //    }
        //}

        //static List<int> Transpositions(int n)
        //{
        //    List<int> toReturn = new List<int>();
        //    string s = n.ToString();
        //    for (int i = 0; i < s.Length - 1; i++)
        //    {
        //        string t = Replace(Replace(s, i, s[i + 1]), i + 1, s[i]);
        //        toReturn.Add(int.Parse(t));
        //    }

        //    return toReturn;
        //}

        //static string Replace(string s, int i, char c)
        //{
        //    return s.Remove(i, 1).Insert(i, c.ToString());
        //}
    }

    class DateIndex
    {
        public DateTime Date { get; set; }
        public int Index { get; set; }
    }
}
