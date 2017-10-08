using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public class Address
    {
        public int EnterpriseId { get; set; }
        public string ApartmentNumber { get; set; }
        public string StreetName { get; set; }
        public string Suffix { get; set; }
        public string FullStreetName { get; set; }
        public string StreetNumber { get; set; }
        public int? Zip { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Other { get; set; }
        public string RawAddress1 { get; set; }
        public string RawAddress2 { get; set; }
        public MatchQuality MatchQuality { get; set; }
        public string CenterName { get; set; }
        public bool StreetNameIsNumber { get; set; }
        public int POBoxNumber { get; set; }
        public string[] OriginalLineBits { get; set; }
        public bool ApartmentNumberFromAddress2Field { get; set; }
        public string OriginalLine { get; internal set; }

        public override string ToString()
        {
            return $"Apartment#: {ApartmentNumber}, StreetName: {StreetName}, StreetNumber: {StreetNumber}, Suffix {Suffix}";
        }
    }
}
