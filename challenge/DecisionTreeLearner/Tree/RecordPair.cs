using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    public class RecordPair
    {
        public Record Record1 { get; set; }
        public Record Record2 { get; set; }
        public bool IsMatch { get; set; } 
    }
}
