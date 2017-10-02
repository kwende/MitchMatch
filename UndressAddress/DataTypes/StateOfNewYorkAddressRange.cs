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
        public string NonNumberBuildingNumber { get; set; }
        public int BuildingNumberStart { get; set; }
        public int BuildingNumberEnd { get; set; }
        public string StreetName { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
    }
}
