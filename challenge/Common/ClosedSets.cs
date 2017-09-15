using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class ClosedSets
    {
        private Row[] _allRows;
        private Dictionary<int, int> _eidToRowIndex;
        private List<Row>[] _rowToClosedRowSet;


        public ClosedSets(Row[] allRows)
        {
            _allRows = allRows;
            _rowToClosedRowSet = new List<Row>[allRows.Length];
            _eidToRowIndex = new Dictionary<int, int>();
            for (int i = 0; i < allRows.Length; i++)
            {
                Row row = allRows[i];
                _eidToRowIndex[row.EnterpriseID] = i;
                _rowToClosedRowSet[i] = new List<Row> { row };
            }
        }

        public bool AddMatch(List<int> match)
        {
            return AddMatch(match.Select(id => _allRows[_eidToRowIndex[id]]).ToList());
        }

        public bool AddMatch(Row row1, Row row2)
        {
            return AddMatch(new List<Row> { row1, row2 });
        }

        public bool AddMatch(List<Row> match)
        {
            var existingClosedSet = _rowToClosedRowSet[_eidToRowIndex[match[0].EnterpriseID]];
            if (match.Any(r => !existingClosedSet.Contains(r)))
            {
                int oldCounts = 0;
                List<Row> closedSet = match.SelectMany(row => _rowToClosedRowSet[_eidToRowIndex[row.EnterpriseID]]).Distinct().OrderBy(row => row.EnterpriseID).ToList();
                bool[] counted = new bool[closedSet.Count];
                for (int i = 0; i < closedSet.Count; i++)
                {
                    Row row = closedSet[i];
                    int rowIndex = _eidToRowIndex[row.EnterpriseID];
                    if (!counted[i])
                    {
                        List<Row> oldSet = _rowToClosedRowSet[rowIndex];
                        int oldLength = oldSet.Count;
                        oldCounts += (oldLength * (oldLength - 1)) / 2;
                        counted[i] = true;
                        for (int j = i + 1; j < closedSet.Count; j++)
                        {
                            if (oldSet.Contains(closedSet[j]))
                            {
                                counted[j] = true;
                            }
                        }
                    }
                    _rowToClosedRowSet[rowIndex] = closedSet;
                }
                int newLength = (closedSet.Count * (closedSet.Count - 1)) / 2;
                _numberOfMatches += newLength - oldCounts / 2; // oldCounts double counts every pair
                return newLength != oldCounts / 2;
            }
            return false;
        }

        int _numberOfMatches = 0;
        public int NumberOfMatches
        {
            get
            {
                return _numberOfMatches;
            }
        }

        public List<Row>[] RowToClosedRowSet
        {
            get
            {
                return _rowToClosedRowSet;
            }
        }

        public int GetRowIndexFromEID(int eid)
        {
            return _eidToRowIndex[eid];
        }

        public List<List<int>> ClosedRowSets()
        {
            List<List<int>> closedRowSets = new List<List<int>>();
            List<int> traversed = new List<int>();
            for (int i = 0; i < _allRows.Length; i++)
            {
                if (!traversed.Contains(_allRows[i].EnterpriseID))
                {
                    List<Row> set = _rowToClosedRowSet[i];
                    closedRowSets.Add(set.Select(row => row.EnterpriseID).ToList());
                    foreach (Row row in set)
                    {
                        traversed.Add(row.EnterpriseID);
                    }
                }
            }
            return closedRowSets;
        }
    }
}
