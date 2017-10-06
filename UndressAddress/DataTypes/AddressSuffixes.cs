using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public class AddressSuffixes
    {
        public string[] ShortSuffixes { get; set; }
        public string[] LongSuffixes { get; set; }

        public bool Contains(string input)
        {
            bool contains = false;
            foreach (string shortSuffix in ShortSuffixes)
            {
                if (shortSuffix == input)
                {
                    contains = true;
                    break;
                }
            }

            foreach (string longSuffix in LongSuffixes)
            {
                if (longSuffix == input)
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }
    }
}
