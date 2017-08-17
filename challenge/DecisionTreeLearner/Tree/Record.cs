using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    [Serializable]
    public class Record
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Suffix { get; set; }
        public string DOB { get; set; }
        public string Gender { get; set; }
        public string SSN { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Zip { get; set; }
        public string MothersMaidenName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string Alias { get; set; }

        public string[] Cache { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}",
                FirstName,
                MiddleName,
                LastName,
                Suffix,
                Gender,
                SSN,
                DOB,
                Phone1,
                Phone2,//
                Address1,
                Address2,
                City.Replace("\"", ""),
                State.Replace("\"", ""),
                Zip,
                MothersMaidenName,//
                Email,//
                Alias);
        }

        public static Record FromFinalDatasetString(string[] bits)
        {
            Record record = new Record();

            record.Cache = new string[19];
            record.FirstName = record.Cache[0] = bits[2];
            record.MiddleName = record.Cache[1] = bits[3];
            record.LastName = record.Cache[2] = bits[1];
            record.Suffix = record.Cache[3] = bits[4];
            record.Gender = record.Cache[4] = bits[6];
            record.SSN = record.Cache[5] = bits[7];
            record.DOB = record.Cache[6] = bits[5];
            record.Phone1 = record.Cache[7] = bits[15];
            record.Phone2 = record.Cache[8] = bits[16];
            record.Address1 = record.Cache[9] = bits[8];
            record.Address2 = record.Cache[10] = bits[9];
            record.City = record.Cache[11] = bits[13];
            record.State = record.Cache[12] = bits[14];
            record.Zip = record.Cache[13] = bits[10];
            record.MothersMaidenName = record.Cache[14] = bits[11];
            record.Email = record.Cache[15] = bits[17];
            record.Cache[16] = "";
            record.Cache[17] = "";
            record.Alias = record.Cache[18] = bits[18];

            return record;
        }

        public static Record FromFinalDatasetString(string csvString)
        {
            string[] bits = csvString.Split(',');
            if (bits.Length != 19)
            {
                throw new Exception();
            }

            return FromFinalDatasetString(bits); 
        }

        public static Record FromString(string csvString)
        {
            Record record = new Record();
            string[] bits = csvString.Split(',');
            if (bits.Length != 19 && bits.Length != 17)
            {
                throw new Exception();
            }

            if(bits.Length == 19)
            {
                record.Cache = bits; 
                record.FirstName = bits[0];
                record.MiddleName = bits[1];
                record.LastName = bits[2];
                record.Suffix = bits[3];
                record.Gender = bits[4];
                record.SSN = bits[5];
                record.DOB = bits[6];
                record.Phone1 = bits[7];
                record.Phone2 = bits[8];
                record.Address1 = bits[9];
                record.Address2 = bits[10];
                record.City = bits[11];
                record.State = bits[12];
                record.Zip = bits[13];
                record.MothersMaidenName = bits[14];
                record.Email = bits[15];
                record.Alias = bits[18];
            }
            else if(bits.Length == 17)
            {
                record.Cache = new string[19]; 
                record.FirstName = record.Cache[0] = bits[0];
                record.MiddleName = record.Cache[1] = bits[1];
                record.LastName = record.Cache[2] = bits[2];
                record.Suffix = record.Cache[3] = bits[3];
                record.Gender = record.Cache[4] = bits[4];
                record.SSN = record.Cache[5] = bits[5];
                record.DOB = record.Cache[6] = bits[6];
                record.Phone1 = record.Cache[7] = bits[7];
                record.Phone2 = record.Cache[8] = bits[8];
                record.Address1 = record.Cache[9] = bits[9];
                record.Address2 = record.Cache[10] = bits[10];
                record.City = record.Cache[11] = bits[11];
                record.State = record.Cache[12] = bits[12];
                record.Zip = record.Cache[13] = bits[13];
                record.MothersMaidenName = record.Cache[14] = bits[14];
                record.Email = record.Cache[15] = bits[15];
                record.Cache[16] = "";
                record.Cache[17] = ""; 
                record.Alias = record.Cache[18] = bits[16];
            }

            return record;
        }
    }
}
