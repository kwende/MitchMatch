using DecisionTreeLearner.Data;
using DecisionTreeLearner.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.ForJosh
{
    public static class AutoPasser
    {
        public static object MysQLConnector { get; private set; }

        public static void DoIt(List<List<int>> allEnterpriseIdsToGroupedBySet)
        {
            // find the set by the first enterprise id. 
            MySQLConnector connector = MySQLConnector.Connect(); 

            connector.WipeAutoPassStatusFromAllSets();

            int counter = 1;
            foreach (List<int> setOfEnterpriseIds in allEnterpriseIdsToGroupedBySet)
            {
                Console.WriteLine($"Working on set {counter} of {allEnterpriseIdsToGroupedBySet.Count}");

                int firstEnterpriseId = setOfEnterpriseIds.First();

                Console.Write("\tVerifying the consistency of the data...");
                int[] enterpriseIdsForSetFromDb = connector.GetSetFromEnterpriseIdOfMember(firstEnterpriseId).Select(n => n.EnterpriseId).ToArray();

                bool allGood = true;
                foreach (int enterpriseId in setOfEnterpriseIds)
                {
                    if (!enterpriseIdsForSetFromDb.Contains(enterpriseId))
                    {
                        allGood = false;
                        break;
                    }
                }

                if (allGood)
                {
                    Console.WriteLine("...good! Whole set passed. ");

                    Console.Write("\tUpdating shit...");
                    int setId = connector.GetSetIdForSetGivenMembersEnterpriseId(firstEnterpriseId);
                    connector.SetSetsState(setId, true, true);
                    connector.SetSetMembersState(setId, true);
                    Console.WriteLine("...updated.");
                }
                else
                {
                    Console.WriteLine("...oops. You passed an incomplete set.");
                }
            }
        }
    }
}
