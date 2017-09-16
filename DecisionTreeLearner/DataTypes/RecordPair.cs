using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.DataTypes
{
    [Serializable]
    public class RecordPair
    {
        public Record Record1 { get; set; }
        public Record Record2 { get; set; }
        public bool IsMatch { get; set; }

        public override string ToString()
        {
            return IsMatch.ToString() + "\n" + Record1.ToString() + "\n" + Record2.ToString() + "\n";
        }

        public override bool Equals(object obj)
        {
            bool equals = false;
            RecordPair other = obj as RecordPair;
            if (other != null)
            {
                return (other.Record1.EnterpriseId == Record1.EnterpriseId ||
                    other.Record1.EnterpriseId == Record2.EnterpriseId) &&
                    (other.Record2.EnterpriseId == Record1.EnterpriseId ||
                    other.Record2.EnterpriseId == Record2.EnterpriseId);
            }
            return equals;
        }
    }
}
