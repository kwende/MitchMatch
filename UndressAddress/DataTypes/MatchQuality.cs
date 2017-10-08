using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public enum MatchQuality
    {
        /// <summary>
        /// Nothing about the address can be used to determine location. 
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The address indicates the person is homeless. 
        /// </summary>
        Homeless = 1,
        /// <summary>
        /// Catch-all for not full processed yet. 
        /// </summary>
        MatchNotYetDetermined = 2,
        /// <summary>
        /// There is nothing to do to this address, we can't process any better than it is. 
        /// </summary>
        LeaveAlone = 3,
        /// <summary>
        /// Alternates have been found for this address. 
        /// </summary>
        Alternate = 4,
    }
}
