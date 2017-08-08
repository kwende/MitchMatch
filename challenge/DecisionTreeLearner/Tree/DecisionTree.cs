using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.Tree
{
    public class DecisionTree
    {
        private static double ComputeShannonEntropy(List<RecordPair> pairs)
        {
            double entropy = 0.0;

            int numberInMatchColumn = pairs.Count(n => n.IsMatch);
            int numberInNoMatchColum = pairs.Count(n => !n.IsMatch);
            double totalLength = pairs.Count;

            double ratio = numberInMatchColumn / totalLength;
            entropy = -(ratio * System.Math.Log(ratio, 2));

            ratio = numberInNoMatchColum / totalLength;
            entropy += -(ratio * System.Math.Log(ratio, 2));

            return entropy;
        }

        private static SplittingQuestion[] GenerateSplittingQuestions(FieldEnum[] fields, int maximumEditDistance)
        {
            List<SplittingQuestion> splittingQuestions = new List<SplittingQuestion>();

            foreach (FieldEnum field in fields)
            {
                splittingQuestions.Add(new SplittingQuestion
                {
                    Field = field,
                    MatchType = MatchTypeEnum.EmptyMatch,
                    BothFieldValuesAreEmpty = true,
                    OneFieldValueIsEmpty = false,
                });

                splittingQuestions.Add(new SplittingQuestion
                {
                    Field = field,
                    MatchType = MatchTypeEnum.EmptyMatch,
                    BothFieldValuesAreEmpty = false,
                    OneFieldValueIsEmpty = true,
                });

                for (int editDistance = 0; editDistance < maximumEditDistance; editDistance++)
                {
                    splittingQuestions.Add(new SplittingQuestion
                    {
                        Field = field,
                        MatchType = MatchTypeEnum.EditDistance,
                        MaximumEditDistance = editDistance,
                    });
                }
            }

            return splittingQuestions.ToArray();
        }

        public void Train(List<RecordPair> trainingData)
        {
            FieldEnum[] fieldsOnWhichToTrain = new FieldEnum[]
            {
                FieldEnum.Address1,
                FieldEnum.Address2,
                FieldEnum.City,
                FieldEnum.DOB,
                FieldEnum.Email,
                FieldEnum.FirstName,
                FieldEnum.Gender,
                FieldEnum.LastName,
                FieldEnum.MiddleName,
                FieldEnum.Phone1,
                FieldEnum.Phone2,
                FieldEnum.SSN,
                FieldEnum.State,
                FieldEnum.Zip
            };

            SplittingQuestion[] splittingQuestions = GenerateSplittingQuestions(fieldsOnWhichToTrain, 3);
            double currentEntropy = ComputeShannonEntropy(trainingData); 
        }
    }
}
