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
        public string LastName
        {
            get
            {
                return Cache[(int)FieldEnum.LastName];
            }
            set
            {
                Cache[(int)FieldEnum.LastName] = value;
            }
        }
        public string FirstName
        {
            get
            {
                return Cache[(int)FieldEnum.FirstName];
            }
            set
            {
                Cache[(int)FieldEnum.FirstName] = value;
            }
        }
        public string MiddleName
        {
            get
            {
                return Cache[(int)FieldEnum.MiddleName];
            }
            set
            {
                Cache[(int)FieldEnum.MiddleName] = value;
            }
        }
        public string Suffix
        {
            get
            {
                return Cache[(int)FieldEnum.Suffix];
            }
            set
            {
                Cache[(int)FieldEnum.Suffix] = value;
            }
        }
        public string DOB
        {
            get
            {
                return Cache[(int)FieldEnum.DOB];
            }
            set
            {
                Cache[(int)FieldEnum.DOB] = value;
            }
        }
        public string Gender
        {
            get
            {
                return Cache[(int)FieldEnum.Gender];
            }
            set
            {
                Cache[(int)FieldEnum.Gender] = value;
            }
        }
        public string SSN
        {
            get
            {
                return Cache[(int)FieldEnum.SSN];
            }
            set
            {
                Cache[(int)FieldEnum.SSN] = value;
            }
        }
        public string Address1
        {
            get
            {
                return Cache[(int)FieldEnum.Address1];
            }
            set
            {
                Cache[(int)FieldEnum.Address1] = value;
            }
        }
        public string Address2
        {
            get
            {
                return Cache[(int)FieldEnum.Address2];
            }
            set
            {
                Cache[(int)FieldEnum.Address2] = value;
            }
        }
        public string Zip
        {
            get
            {
                return Cache[(int)FieldEnum.Zip];
            }
            set
            {
                Cache[(int)FieldEnum.Zip] = value;
            }
        }
        public string MothersMaidenName
        {
            get
            {
                return Cache[(int)FieldEnum.MothersMaidenName];
            }
            set
            {
                Cache[(int)FieldEnum.MothersMaidenName] = value;
            }
        }
        public string City
        {
            get
            {
                return Cache[(int)FieldEnum.City];
            }
            set
            {
                Cache[(int)FieldEnum.City] = value;
            }
        }
        public string State
        {
            get
            {
                return Cache[(int)FieldEnum.State];
            }
            set
            {
                Cache[(int)FieldEnum.State] = value;
            }
        }
        public string Phone1
        {
            get
            {
                return Cache[(int)FieldEnum.Phone1];
            }
            set
            {
                Cache[(int)FieldEnum.Phone1] = value;
            }
        }
        public string Phone2
        {
            get
            {
                return Cache[(int)FieldEnum.Phone2];
            }
            set
            {
                Cache[(int)FieldEnum.Phone2] = value;
            }
        }
        public string Email
        {
            get
            {
                return Cache[(int)FieldEnum.Email];
            }
            set
            {
                Cache[(int)FieldEnum.Email] = value;
            }
        }
        public string Alias
        {
            get
            {
                return Cache[(int)FieldEnum.Alias];
            }
            set
            {
                Cache[(int)FieldEnum.Alias] = value;
            }
        }

        public int EnterpriseId { get; set; }
        public int MRN { get; set; }

        public string[] Cache { get; set; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is Record)
            {
                Record other = (Record)obj;

                equals = other.LastName == LastName &&
                    other.FirstName == FirstName &&
                    other.MiddleName == MiddleName &&
                    other.Suffix == Suffix &&
                    other.DOB == DOB &&
                    other.Gender == Gender &&
                    other.Address1 == Address1 &&
                    other.Address2 == Address2 &&
                    other.Zip == Zip &&
                    other.MothersMaidenName == MothersMaidenName &&
                    other.City == City &&
                    other.State == State &&
                    other.Phone1 == Phone1 &&
                    other.Phone2 == Phone2 &&
                    other.Email == Email &&
                    other.Alias == Alias;
            }

            return equals;
        }

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
            record.EnterpriseId = int.Parse(bits[0]);
            record.MRN = int.Parse(bits[12]); 
            record.FirstName = bits[2];
            record.MiddleName = bits[3];
            record.LastName = bits[1];
            record.Suffix = bits[4];
            record.Gender = bits[6];
            record.SSN = bits[7];
            record.DOB = bits[5];
            record.Phone1 = bits[15];
            record.Phone2 = bits[16];
            record.Address1 = bits[8];
            record.Address2 = bits[9];
            record.City = bits[13];
            record.State = bits[14];
            record.Zip = bits[10];
            record.MothersMaidenName = bits[11];
            record.Email = bits[17];
            record.Cache[16] = "";
            record.Cache[17] = "";
            record.Alias = bits[18];

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
            string[] bits = csvString.Split(',').Select(n => n.Trim()).ToArray();
            if (bits.Length != 19 && bits.Length != 17)
            {
                throw new Exception();
            }

            if (bits.Length == 19)
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
                record.Cache[16] = "";
                record.Cache[17] = "";
                record.Alias = bits[18];
            }
            else if (bits.Length == 17)
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
