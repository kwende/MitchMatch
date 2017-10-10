using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    [Serializable]
    public class StreetNameAndZip
    {
        public string FullStreetName { get; set; }
        public int Zip { get; set; }

        public override int GetHashCode()
        {
            return (FullStreetName + "_" + Zip.ToString()).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is StreetNameAndZip)
            {
                StreetNameAndZip other = (StreetNameAndZip)obj;

                return other.FullStreetName == FullStreetName &&
                    other.Zip == Zip;
            }
            else
            {
                return false;
            }
        }
    }
}
