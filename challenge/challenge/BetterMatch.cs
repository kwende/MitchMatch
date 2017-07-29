using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace challenge
{
    public class BetterMatch
    {
        public row TheRow { get; set; }
        public row TheOriginalMatch { get; set; }
        public double OriginalMatchDistance { get; set; }
        public row ABetterMatch { get; set; }
        public double BetterMatchDistance { get; set; }
    }

}
