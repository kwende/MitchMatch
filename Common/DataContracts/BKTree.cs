using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Serializable]
    public class BKTree
    {
        public string StringValue;
        public int Index;
        public BKTree[] Children;
    }
}
