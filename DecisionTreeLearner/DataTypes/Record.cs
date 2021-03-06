﻿using DecisionTreeLearner.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DecisionTreeLearner.Attributes;

namespace DecisionTreeLearner.DataTypes
{
    [Serializable]
    public class Record
    {
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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
        [EditDistanceCapable]
        [PersonalInformation]
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

        [BookkeepingInformation]
        public int EnterpriseId { get; set; }
        [BookkeepingInformation]
        public int MRN { get; set; }

        public bool LivesInLargeResidence { get; set; }

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

                equals = other.EnterpriseId == other.EnterpriseId;
            }

            return equals;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
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
                QuoteIfNeeded(City.Replace("\"", "")),
                State.Replace("\"", ""),
                Zip,
                MothersMaidenName,//
                Email,//
                MRN,
                EnterpriseId,
                Alias);
        }

        private static string QuoteIfNeeded(string input)
        {
            if (input.Contains(","))
            {
                input = $"\"{input}\"";
            }

            return input;
        }

        public static Record FromFinalDatasetString(string[] bits)
        {
            Record record = new Record();

            record.Cache = new string[19];
            record.EnterpriseId = int.Parse(bits[0]);
            record.MRN = bits[12] != "" ? int.Parse(bits[12]) : 0;
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
            record.Cache[16] = bits[12];
            record.Cache[17] = bits[0];
            record.Alias = bits[18];

            return record;
        }

        public static Record FromFinalDatasetString(string csvString)
        {
            string[] bits = DataLoader.SmartSplit(csvString);
            if (bits.Length != 19)
            {
                new FormatException($"Line of format '{csvString}' is invalid.");
            }

            return FromFinalDatasetString(bits);
        }

        public static Record FromString(string csvString)
        {
            Record record = new Record();
            string[] bits = DataLoader.SmartSplit(csvString);
            if (bits.Length != 19)
            {
                new FormatException($"Line of format '{csvString}' is invalid.");
            }

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
            record.Cache[16] = bits[16];
            record.MRN = bits[16] != "" ? int.Parse(bits[16]) : 0;
            record.Cache[17] = bits[17];
            record.EnterpriseId = bits[17] != "" ? int.Parse(bits[17]) : 0;
            record.Alias = bits[18];

            return record;
        }
    }
}
