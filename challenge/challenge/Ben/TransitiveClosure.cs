using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class TransitiveClosure
    {
        private Dictionary<int, List<int>> _matches;
        private row[] _allRows;
        private List<List<int>> _closedRowSets;

        public List<List<int>> ClosedRowSets
        {
            get
            {
                return _closedRowSets;
            }
        }

        private TransitiveClosure(Dictionary<int, List<int>> matches, row[] allRows)
        {
            //  make my own copy
            _matches = matches;
            _allRows = allRows;

            _closedRowSets = new List<List<int>>();
        }

        private void RecursiveFindClosure(int key, List<int> closedSet, Dictionary<int, List<int>> matches)
        {
            foreach (int matchedKey in matches[key])
            {
                if(!closedSet.Contains(matchedKey))
                {
                    closedSet.Add(matchedKey);
                    RecursiveFindClosure(matchedKey, closedSet, matches);
                }
            }
        }

        private void FindAllClosures()
        {
            // get the keys for all the matches. 
            List<int> keysToMatch = _matches.Keys.ToList();

            // continue so long as there are keys to match
            while (keysToMatch.Count > 0)
            {
                // will hold the actual row instances. 
                List<int> rowsInClosedSet = new List<int>();

                // grab the first key. 
                int key = keysToMatch[0];

                // recursively find all of those who are in any way
                // linked to this key by walking the graph. 
                List<int> keysInClosedSet = new List<int>();
                keysInClosedSet.Add(key); 
                RecursiveFindClosure(key, keysInClosedSet, _matches);

                // we don't want to encounter any of these keys again since 
                // this would result in us getting back a duplicate set. 
                // this includes the 0th entry. 
                // Also, map to the Enterprise ID
                foreach (int keyInClosedSet in keysInClosedSet)
                {
                    keysToMatch.Remove(keyInClosedSet);
                    rowsInClosedSet.Add(keyInClosedSet);
                }

                _closedRowSets.Add(rowsInClosedSet); 
            }
        }

        public row[] FindClosedSetForRow(row input)
        {
            row[] toReturn = null; 
            int enterpriseId = input.EnterpriseID;
            foreach (List<int> rowSet in _closedRowSets)
            {
                if(rowSet.Any(n=>n == enterpriseId))
                {
                    toReturn = _allRows.Where(r => rowSet.Contains(r.EnterpriseID)).ToArray();
                    break; 
                }
            }
            return toReturn; 
        }

        public static TransitiveClosure Compute(Dictionary<int, List<int>> matches, row[] allRows)
        {
            TransitiveClosure transitiveClosure = new TransitiveClosure(matches, allRows);
            transitiveClosure.FindAllClosures();

            return transitiveClosure;
        }
    }
}
