using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public abstract class FastAbstractGrouper
    {
        public abstract Matches EditDistanceAtMostN(string[] strings, int n);

        public RowMatchObject EditDistanceAtMostN(Row[] data, Func<Row, string> fieldSelector, int n)
        {
            //Start by grouping the data into fields
            Console.WriteLine("Grouping By Field Value");
            Dictionary<string, List<Row>> rowsByFieldValue = new Dictionary<string, List<Row>>();
            foreach (var d in data)
            {
                string field = fieldSelector(d);
                if (field == "")
                    continue;
                if (!rowsByFieldValue.ContainsKey(field))
                    rowsByFieldValue[field] = new List<Row>();

                rowsByFieldValue[field].Add(d);
            }



            Console.WriteLine("Creating EID <=> Index Maps");
            int[] eidToIndex = new int[data.Max(d => d.EnterpriseID) + 1];
            for (int i = 0; i < eidToIndex.Length; i++)
                eidToIndex[i] = -1;
            int groupIndex = 0;
            List<int>[] indexToEids = new List<int>[rowsByFieldValue.Count()];
            foreach (var pair in rowsByFieldValue)
            {
                foreach (var row in pair.Value)
                    eidToIndex[row.EnterpriseID] = groupIndex;

                indexToEids[groupIndex] = pair.Value.Select(r => r.EnterpriseID).ToList();
                groupIndex++;
            }

            var strings = rowsByFieldValue.Select(p => p.Key).ToArray();



            List<Row>[] rowsWithThisField = new List<Row>[strings.Length];
            for (int i = 0; i < strings.Length; i++)
                rowsWithThisField[i] = rowsByFieldValue[strings[i]];

            var stringMatches = EditDistanceAtMostN(strings, n);

            RowMatchObject toReturn = new RowMatchObject
            {
                Matches = stringMatches,
                EidToIndex = eidToIndex,
                IndexToEids = indexToEids
            };

            return toReturn;
        }
    }
}
