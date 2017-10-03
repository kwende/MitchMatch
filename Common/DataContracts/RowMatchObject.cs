using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Serializable]
    public class RowMatchObject
    {
        public Matches Matches { get; set; }
        public int[] EidToIndex { get; set; }
        public List<int>[] IndexToEids { get; set; }
    }
}
