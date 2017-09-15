using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Data
{
    public static class LargeResidenceFileLoader
    {
        private static string[] _largeResidenceList = null;
        private static object _syncObject = new object();

        public static string[] GetLargeResidenceList()
        {
            lock (_syncObject)
            {
                if (_largeResidenceList == null)
                {
                    _largeResidenceList = File.ReadLines("largeResidence.txt").Select(n => n.Replace("\"", "")).ToArray();
                }
            }
            return _largeResidenceList;
        }
    }
}
