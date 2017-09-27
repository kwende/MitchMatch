using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public class Address    
    {
        public string ApartmentNumber { get; set; }
        public string StreetName { get; set; }
        public string Suffix { get; set; }
        public string StreetNumber { get; set; }
        public int? Zip { get; set; }
        public string City { get; set; }
        public string Other { get; set; }
        public string Original { get; set; }
        public MatchQuality MatchQuality { get; set; }
    }
}
