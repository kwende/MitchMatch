using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    [Serializable]
    public class StreetName
    {
        public StreetName(string preDirection, string preType, string name, string suffix, List<int> zipCodes, List<string> cities)
        {
            PreDirection = preDirection;
            PreType = preType;
            Name = name;
            Suffix = suffix;

            FullStreetName = (PreDirection + " " + (PreType + " " + Name + " " + Suffix).Trim()).Trim();

            ZipCodes = zipCodes;
            Cities = cities;
        }

        public string FullStreetName { get; private set; }
        public string PreDirection { get; private set; }
        public string PreType { get; private set; }
        public string Name { get; private set; }
        public string Suffix { get; private set; }
        public List<int> ZipCodes { get; private set; }
        public List<string> Cities { get; private set; }

        public override string ToString()
        {
            return FullStreetName;
        }

        public static bool operator ==(StreetName a, StreetName b)
        {
            if (Object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.FullStreetName == b.FullStreetName;
        }

        public static bool operator !=(StreetName a, StreetName b)
        {
            return !(a == b);
        }

        public static bool operator ==(StreetName a, string b)
        {
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.FullStreetName == b;
        }

        public static bool operator !=(StreetName a, string b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StreetName) || obj == null)
            {
                return false;
            }

            StreetName other = (StreetName)obj;

            return this.FullStreetName == other.FullStreetName;
        }

        public override int GetHashCode()
        {
            return this.FullStreetName.GetHashCode();
        }
    }
}
