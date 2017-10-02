using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FastFuzzyDateGrouper : FastAbstractGrouper
    {
        public override Matches EditDistanceAtMostN(string[] strings, int n)
        {
            DateTime[] dates = strings.Select(s => DateTime.Parse(s)).ToArray();

            Matches toReturn = new Matches(dates.Length);

            var dateIndices = dates.Select((d, i) => new DateIndex { Date = d, Index = i }).ToArray();

            
            //Day month transpositions
            var groupedByYearAndNormalizedDateTime = dateIndices.GroupBy(d => System.Math.Min(d.Date.Day,d.Date.Month) + "/" + System.Math.Max(d.Date.Day, d.Date.Month)+ "/" + d.Date.Year);
            foreach (var group in groupedByYearAndNormalizedDateTime)
            {
                var groupArray = group.ToArray();

                for (int i = 0; i < groupArray.Length; i++)
                    for (int j = i + 1; j < groupArray.Length; j++)
                        toReturn.AddMatch(groupArray[i].Index, groupArray[j].Index);
            }

            //Day transposed, or off by one, or off by 1 digit
            var groupedByMonthAndYear = dateIndices.GroupBy(d => d.Date.Month + "/" + d.Date.Year);
            foreach (var group in groupedByMonthAndYear)
            {
                var groupArray = group.ToArray();
                for (int i = 0; i < groupArray.Length; i++)
                    for (int j = i + 1; j < groupArray.Length; j++)
                    {
                        int day1 = groupArray[i].Date.Day;
                        int day2 = groupArray[j].Date.Day;
                        if (MatchingManager.OneOrOneDigit(day1, day2) || MatchingManager.TransposedDigit(day1, day2))
                            toReturn.AddMatch(groupArray[i].Index, groupArray[j].Index);
                    }
                        
            }

            //Month transposed, or off by one, or off by 1 digit
            var groupedByDayAndYear = dateIndices.GroupBy(d => d.Date.Day + "/" + d.Date.Year);
            foreach (var group in groupedByDayAndYear)
            {
                var groupArray = group.ToArray();
                for (int i = 0; i < groupArray.Length; i++)
                    for (int j = i + 1; j < groupArray.Length; j++)
                    {
                        int month1 = groupArray[i].Date.Month;
                        int month2 = groupArray[j].Date.Month;
                        if (MatchingManager.OneOrOneDigit(month1, month2) || MatchingManager.TransposedDigit(month1, month2))
                            toReturn.AddMatch(groupArray[i].Index, groupArray[j].Index);
                    }
            }

            //Year has transposed digit or is off by one or one digit, or is off by 100
            var groupedByDayAndMonth = dateIndices.GroupBy(d => d.Date.Day + "/" + d.Date.Month);
            foreach (var group in groupedByDayAndMonth)
            {
                var groupArray = group.ToArray();
                for (int i = 0; i < groupArray.Length; i++)
                    for (int j = i + 1; j < groupArray.Length; j++)
                    {
                        int year1 = groupArray[i].Date.Year;
                        int year2 = groupArray[j].Date.Year;
                        if (MatchingManager.OneOrOneDigit(year1, year2) || MatchingManager.TransposedDigit(year1, year2) || MatchingManager.OffBy100(year1,year2))
                            toReturn.AddMatch(groupArray[i].Index, groupArray[j].Index);
                    }
                        
            }

            toReturn.Clean();

            return toReturn;
        }
    }

    class DateIndex
    {
        public DateTime Date { get; set; }
        public int Index { get; set; }
    }
}
