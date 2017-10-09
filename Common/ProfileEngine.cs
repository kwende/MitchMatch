using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class ProfileEngine
    {
        static int EditDistanceUpTo(string a, string b, int max)
        {
            if (a == "" || b == "")
                return -1;
            int ed = EditDistanceEngine.Compute(a, b);
            return ed <= max ? ed : max + 1;
        }

        static int NumericEditDistanceUpTo(long a, long b, int max)
        {
            if (a <= 0 || b <= 0)
                return -1;

            return EditDistanceUpTo(a.ToString(), b.ToString(), max);
        }

        static int Equals(string a, string b)
        {
            if (a == "" || b == "")
                return -1;

            if (a == b)
                return 0;

            return 1;
        }

        static int MRNDistance(int a, int b)
        {
            if (a <= 0 || b <= 0)
                return -1;
            int mrnDiff = System.Math.Abs(a - b);
            if (mrnDiff > 1200)
                return 14;
            if (mrnDiff == 0)
                return 0;
            return mrnDiff / 100 + 1;
        }

        public static string[] OrderedFields = new string[] { "LAST", "FIRST", "MIDDLE", "SUFFIX", "DOB", "GENDER","SSN","ADDRESS1","ADDRESS2","ZIP","MOTHERS_MAIDEN_NAME","MRN","CITY","STATE","PHONE","EMAIL"};

        public static int[] CreateProfile(Row a, Row b)
        {
            List<int> toReturn = new List<int>();
            int i = 0;

            toReturn.Add(EditDistanceUpTo(a.LAST, b.LAST, 2));
            toReturn.Add(EditDistanceUpTo(a.FIRST, b.FIRST, 2));
            toReturn.Add(EditDistanceUpTo(a.MIDDLE, b.MIDDLE, 2));
            toReturn.Add(EditDistanceUpTo(a.SUFFIX, b.SUFFIX, 2));
            toReturn.Add(a.DOB == default(DateTime) ? -1 : a.DOB == b.DOB ? 0 : MatchingManager.FuzzyDateEquals(a.DOB, b.DOB) ? 1 : 2);
            toReturn.Add(Equals(a.GENDER, b.GENDER));
            toReturn.Add(NumericEditDistanceUpTo(a.SSN, b.SSN, 2));
            toReturn.Add(EditDistanceUpTo(a.ADDRESS1, b.ADDRESS1, 2));
            toReturn.Add(EditDistanceUpTo(a.ADDRESS2, b.ADDRESS2, 2));
            toReturn.Add(NumericEditDistanceUpTo(a.ZIP, b.ZIP, 2));
            toReturn.Add(EditDistanceUpTo(a.MOTHERS_MAIDEN_NAME, b.MOTHERS_MAIDEN_NAME, 2));
            toReturn.Add(MRNDistance(a.MRN, b.MRN));
            toReturn.Add(Equals(a.CITY, b.CITY));
            toReturn.Add(Equals(a.STATE, b.STATE));
            toReturn.Add(NumericEditDistanceUpTo(a.PHONE, b.PHONE, 1));
            toReturn.Add(EditDistanceUpTo(a.EMAIL, b.EMAIL, 2));
            //Skipping PHONE2.  Might come back to this later, probably by making copies of this person and putting PHONE2 into PHONE1
            //Skipping ALIAS.  Might come back to this later, probably by making copies of this person and putting ALIAS into ALIAS

            return toReturn.ToArray();
        }

        public static ProfileDistribution CreateProfileDistribution(List<List<int>> knownMatches, Row[] allData)
        {
            string directoryPath = @"C:\Users\jbrownkramer\Desktop\PatientMatchingData";

            ProfileDistribution toReturn = new ProfileDistribution();
            toReturn.Factors = new ConditionedDistribution[OrderedFields.Length];

            for (int i = 0; i < OrderedFields.Length; i++)
            {
                string fieldName = OrderedFields[i];
                Console.WriteLine("Creating distribution for " + fieldName);

                RowMatchObject matchObject;
                if (fieldName == "MRN")
                {
                    matchObject = FastFuzzyMatchEngine.FuzzyMRNMatches(allData);
                }
                else
                {
                    string filePath = Path.Combine(directoryPath, fieldName + "Matches.dat");
                    matchObject = Serializer.Deserialize<RowMatchObject>(filePath);
                }
                var probabilityObject = ProbabilityEngine.ComputeConditionedDistribution(knownMatches, matchObject, allData.Length);
                toReturn.Factors[i] = probabilityObject;
            }

            //Compute the probability of match
            long totalMatches = 0;
            foreach(var set in knownMatches)
            {
                totalMatches += Choose2(set.Count);
            }
            toReturn.PMatch = ((double)totalMatches) / ((double)Choose2(allData.Length));

            return toReturn;
        }

        static long Choose2(long n)
        {
            return n * (n - 1) / 2;
        }
    }

    //public int EnterpriseID;
    //public string LAST;
    //public string FIRST;
    //public string MIDDLE;
    //public string SUFFIX;
    //public DateTime DOB;
    //public string GENDER;
    //public int SSN;
    //public string ADDRESS1;
    //public string ADDRESS2;
    //public int ZIP;
    //public string MOTHERS_MAIDEN_NAME;
    //public int MRN;
    //public string CITY;
    //public string STATE;
    //public long PHONE;
    //public string PHONE2;
    //public string EMAIL;
    //public string ALIAS;

    [Serializable]
    public class ProfileDistribution
    {
        /// <summary>
        /// Indexed the same as profile
        /// </summary>
        public ConditionedDistribution[] Factors;
        public double PMatch;
    }
}
