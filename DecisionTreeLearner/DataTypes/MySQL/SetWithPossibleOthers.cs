using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.DataTypes.MySQL
{
    public class SetWithPossibleOthers
    {
        public List<Record> OriginalSet { get; set; }
        public List<Record> PossibleMatches { get; set; }
    }
}
