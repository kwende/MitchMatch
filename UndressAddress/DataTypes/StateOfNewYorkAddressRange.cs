using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    [Serializable]
    public class StateOfNewYorkAddressRange
    {
        public StreetNumberRange StreetNumber { get; set; }
        public string StreetName { get; set; }
        public int ZipCode { get; set; }
        public string City { get; set; }
        public string Suffix { get; set; }
        public string FullStreetName { get; set; }
    }
}
