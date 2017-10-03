using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public class StreetNumberRange
    {
        public StreetNumberRange(int startNumber, int endNumber)
        {
            StartNumber = startNumber;
            EndNumber = endNumber;
            StreetNumberIsNumber = true;
        }

        public StreetNumberRange(string nonNumberStreetNumber)
        {
            NonNumberStreetNumber = nonNumberStreetNumber.ToUpper();
            StreetNumberIsNumber = false;
        }

        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
        public string NonNumberStreetNumber { get; set; }
        public bool StreetNumberIsNumber { get; private set; }

        public bool IsInRange(string value)
        {
            int intValue = 0;
            if (StreetNumberIsNumber && int.TryParse(value, out intValue))
            {
                return intValue >= StartNumber && intValue <= EndNumber;
            }
            else if (!StreetNumberIsNumber)
            {
                return value.ToUpper() == NonNumberStreetNumber;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            if (StreetNumberIsNumber)
            {
                if (StartNumber != EndNumber)
                {
                    return $"{StartNumber}-{EndNumber}";
                }
                else
                {
                    return StartNumber.ToString();
                }
            }
            else
            {
                return NonNumberStreetNumber;
            }
        }
    }
}
