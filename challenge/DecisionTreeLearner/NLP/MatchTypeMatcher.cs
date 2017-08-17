using DecisionTreeLearner.Data;
using DecisionTreeLearner.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.NLP
{
    public static class MatchTypeMatcher
    {
        public static bool BasedOnEditDistance(SplittingQuestion question, string column1, string column2)
        {
            bool matches = false;

            int editDistance = NLP.EditDistance.Compute(column1, column2);

            if (editDistance <= question.MaximumEditDistance)
            {
                matches = true;
            }

            return matches;
        }

        public static bool BasedOnEmptyFields(SplittingQuestion question, string column1, string column2)
        {
            bool matches = false;
            if (question.OneFieldValueIsEmpty)
            {
                if (question.Field == FieldEnum.SSN || question.Field == FieldEnum.Phone1 || question.Field == FieldEnum.SSN)
                {
                    if (column1 == "" || column1 == "0" || column1 == "-1")
                    {
                        matches = !(column2 == "" || column2 == "0" || column2 == "-1");
                    }
                    else
                    {
                        matches = (column2 == "" || column2 == "0" || column2 == "-1");
                    }
                }
                else
                {
                    if (column1 == "")
                    {
                        matches = column2 != "";
                    }
                    else
                    {
                        matches = column2 == "";
                    }
                }
            }
            else if (question.BothFieldValuesAreEmpty)
            {
                if (question.Field == FieldEnum.SSN || question.Field == FieldEnum.Phone1 || question.Field == FieldEnum.SSN)
                {
                    matches = (column1 == "" || column1 == "0" || column1 == "-1") &&
                        (column2 == "" || column2 == "0" || column2 == "-1");
                }
                else
                {
                    matches = column1 == "" && column2 == "";
                }
            }
            else
            {
                throw new ArgumentException();
            }

            return matches;
        }

        public static bool BasedOnPhone2SoftMatch(SplittingQuestion question, string column1, string column2)
        {
            bool matches = false;

            if (column1.Contains("^^"))
            {
                string[] column1Bits = column1.Split(new string[] { "^^" }, StringSplitOptions.None);
                matches = column1Bits.Contains(column2);
            }
            else if (column2.Contains("^^"))
            {
                string[] column2Bits = column2.Split(new string[] { "^^" }, StringSplitOptions.None);
                matches = column2Bits.Contains(column1);
            }

            return matches;
        }

        public static bool BasedOnDateSoftMatch(SplittingQuestion question, string column1, string column2)
        {
            bool passedSoftMatch = false;
            DateTime column1Date;
            DateTime column2Date;

            if (DateTime.TryParse(column1, out column1Date) &&
                DateTime.TryParse(column2, out column2Date))
            {
                passedSoftMatch = (column1Date.Day == column2Date.Month ||
                    column2Date.Day == column1Date.Month);

                if (!passedSoftMatch)
                {
                    passedSoftMatch = column1Date.Day == column2Date.Day &&
                        column1Date.Month == column2Date.Month &&
                        System.Math.Abs(column1Date.Year - column1Date.Year) == 10;

                    if (!passedSoftMatch)
                    {
                        passedSoftMatch = System.Math.Abs(column1Date.Day - column2Date.Day) == 1 &&
                            column1Date.Month == column2Date.Month &&
                            column1Date.Year == column2Date.Year;

                        if (!passedSoftMatch)
                        {
                            if (column1.Length == column2.Length)
                            {
                                int visuallySimilarChars = 0;
                                for (int c = 0; c < column1.Length; c++)
                                {
                                    char column1Char = column1[c];
                                    char column2Char = column2[c];

                                    if (column1Char != column2Char)
                                    {
                                        if ((column1Char == '3' && column2Char == '2') ||
                                            (column2Char == '2' && column1Char == '3') ||
                                            (column1Char == '5' && column2Char == '6') ||
                                            (column1Char == '6' && column2Char == '5') ||
                                            (column1Char == '1' && column2Char == '7') ||
                                            (column1Char == '7' && column2Char == '1'))
                                        {
                                            visuallySimilarChars++;
                                        }
                                    }

                                    // too dissimilar
                                    if (visuallySimilarChars >= 2)
                                        break;
                                }

                                passedSoftMatch = visuallySimilarChars == 1;
                            }
                        }
                    }
                }
            }
            return passedSoftMatch;
        }

        public static bool BasedOnAddressSoftMatch(SplittingQuestion question, string column1, string column2)
        {
            bool passedSoftMatch = false;

            string[] address1Bits = column1.Split(' ');
            string[] address2Bits = column2.Split(' ');

            if (address1Bits.Length > 1 && address2Bits.Length > 1)
            {
                List<string> matches = new List<string>();

                string[] suffixes = AddressSuffixLoader.GetAllStreetSuffixes();

                bool integerFound = false;
                foreach (string address1Bit in address1Bits)
                {
                    foreach (string address2Bit in address2Bits)
                    {
                        if (address1Bit == address2Bit && !matches.Contains(address1Bit)
                            && !suffixes.Contains(address1Bit))
                        {
                            matches.Add(address1Bit);

                            int throwAway = 0;
                            if (int.TryParse(address1Bit, out throwAway))
                            {
                                integerFound = true;
                            }
                        }
                    }
                }

                passedSoftMatch = (matches.Count >= 2 && integerFound);
            }
            return passedSoftMatch;
        }
    }
}
