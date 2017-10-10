using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    [Serializable]
    public class StreetNameAndCity
    {
        public string FullStreetName { get; set; }
        public string City { get; set; }

        public override int GetHashCode()
        {
            int hashCode = (FullStreetName + "_" + City).GetHashCode();

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is StreetNameAndCity)
            {
                StreetNameAndCity other = (StreetNameAndCity)obj;

                return other.FullStreetName == FullStreetName &&
                    other.City == City;
            }
            else
            {
                return false;
            }
        }
    }
}
