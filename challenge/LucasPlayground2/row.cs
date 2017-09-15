using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground2
{
    public class Row
    {
        public int EnterpriseID;
        public string LAST;
        public string FIRST;
        public string MIDDLE;
        public string SUFFIX;
        public DateTime DOB;
        public string GENDER;
        public int SSN;
        public string ADDRESS1;
        public string ADDRESS2;
        public int ZIP;
        public string MOTHERS_MAIDEN_NAME;
        public int MRN;
        public string CITY;
        public string STATE;
        public long PHONE;
        public string PHONE2;
        public string EMAIL;
        public string ALIAS;

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
                FIRST,
                MIDDLE,
                LAST,
                SUFFIX,
                GENDER,
                SSN,
                DOB.ToString("dd/MM/yyyy"),
                PHONE,
                PHONE2,//
                ADDRESS1,
                ADDRESS2,
                CITY.Replace("\"", ""),
                STATE.Replace("\"", ""),
                ZIP,
                MOTHERS_MAIDEN_NAME,//
                EMAIL,//
                MRN,
                EnterpriseID,
                ALIAS);
        }
    }
}
