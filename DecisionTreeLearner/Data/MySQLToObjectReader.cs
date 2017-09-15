using DecisionTreeLearner.DataTypes;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Data
{
    public static class ReaderToObject
    {
        public static Record ReaderToRecord(MySqlDataReader reader)
        {
            Record ret = new Record(); 
            ret.Cache = new string[reader.FieldCount - 1];

            ret.EnterpriseId = (int)reader["EnterpriseId"];
            ret.Address1 = (string)reader["Address1"];
            ret.Address2 = (string)reader["Address2"];
            ret.Alias = (string)reader["Alias"];
            ret.City = (string)reader["City"];
            ret.DOB = (string)reader["DOB"];
            ret.Email = (string)reader["Email"];
            ret.FirstName = (string)reader["FirstName"];
            ret.MiddleName = (string)reader["MiddleName"];
            ret.LastName = (string)reader["LastName"];
            ret.MothersMaidenName = (string)reader["MothersMaidenName"];
            ret.Gender = (string)reader["Gender"];

            string mrn = (string)reader["MRN"];

            ret.MRN = !string.IsNullOrEmpty(mrn) ? int.Parse(mrn) : 0;
            ret.Phone1 = (string)reader["Phone"];
            ret.Phone2 = (string)reader["Phone2"];
            ret.SSN = (string)reader["SSN"];
            ret.State = (string)reader["State"];
            ret.Suffix = (string)reader["Suffix"];
            ret.Zip = (string)reader["Zip"];

            return ret; 
        }
    }
}
