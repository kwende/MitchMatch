using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    [Serializable]
    public enum FieldEnum
    {
        FirstName = 0,
        MiddleName = 1,
        LastName = 2,
        Suffix = 3,
        Gender = 4,
        SSN = 5,
        DOB = 6,
        Phone1 = 7,
        Phone2 = 8,
        Address1 = 9, 
        Address2 = 10,
        City = 11,
        State = 12,
        Zip = 13,
        MothersMaidenName = 14,
        Email = 15, 
        MRN = 16,
        Alias = 18
    }
}
