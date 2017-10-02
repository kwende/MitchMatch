using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public class Data
    {
        public AddressSuffixes Suffixes { get; set; }
        public string[] FinalDataSet { get; set; }
        public List<StateOfNewYorkAddressRange> AllAddresses { get; set; }
        public string[] NewYorkStateStreetNames { get; set; }
        public string[] UnknownAddresses { get; set; }
        public string[] HomelessAddresses { get; set; }
        public Dictionary<string, string> Abbreviations { get; set; }
        public Dictionary<string, string> SuffixReplacementKey { get; set; }
        public Dictionary<string, Address> KnownCenters { get; set; }
    }
}
