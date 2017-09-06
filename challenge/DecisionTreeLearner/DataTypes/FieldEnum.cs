using DecisionTreeLearner.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.DataTypes
{
    [Serializable]
    public enum FieldEnum
    {
        [EditDistanceCapable]
        FirstName = 0,
        [EditDistanceCapable]
        MiddleName = 1,
        [EditDistanceCapable]
        LastName = 2,
        [EditDistanceCapable]
        Suffix = 3,
        [EditDistanceCapable]
        Gender = 4,
        [EditDistanceCapable]
        SSN = 5,
        [EditDistanceCapable]
        DOB = 6,
        [EditDistanceCapable]
        Phone1 = 7,
        [EditDistanceCapable]
        Phone2 = 8,
        [EditDistanceCapable]
        Address1 = 9,
        [EditDistanceCapable]
        Address2 = 10,
        [EditDistanceCapable]
        City = 11,
        [EditDistanceCapable]
        State = 12,
        [EditDistanceCapable]
        Zip = 13,
        [EditDistanceCapable]
        MothersMaidenName = 14,
        [EditDistanceCapable]
        Email = 15,
        MRN = 16,
        EnterpriseId = 17,
        [EditDistanceCapable]
        Alias = 18
    }
}
