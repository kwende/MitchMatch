using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public enum MatchQuality
    {
        Unknown = 0,
        Homeless = 1,
        NotMatched = 2,
        StreetMatched = 3,
        FullAddressMatched = 4,
        CouldNotParseFormat = 5,
    }
}
