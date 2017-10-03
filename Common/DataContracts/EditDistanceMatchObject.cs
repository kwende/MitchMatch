using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class EditDistanceMatchObject
    {
        public int Index { get; set; }
        public string Substring { get; set; }
        //These are stored in the indexing scheme for the original string
        public List<int> DeletedIndices { get; set; }
    }
}
