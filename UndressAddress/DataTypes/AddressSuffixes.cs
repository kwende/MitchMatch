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

        public int IndexOf(string input)
        {
            int index = -1;
            for (int c = 0; c < ShortSuffixes.Length; c++)
            {
                if (ShortSuffixes[c] == input)
                {
                    index = c;
                    break;
                }
            }

            for (int c = 0; c < LongSuffixes.Length; c++)
            {
                if (LongSuffixes[c] == input)
                {
                    index = c;
                    break;
                }
            }

            return index;
        }
    }
}
