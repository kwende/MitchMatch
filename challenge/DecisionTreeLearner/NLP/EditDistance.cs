using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecisionTreeLearner.NLP
{
    public class EditDistance
    {
        public static double ComputeNormalized(string first, string second)
        {
            int editDistance = Compute(first, second);
            int maxLength = first.Length > second.Length ? first.Length : second.Length;

            return editDistance / (maxLength * 1.0);
        }

        public static int Compute(string first, string second)
        {
            int n = first.Length, m = second.Length;
            if (n == 0) return m;
            if (m == 0) return n;

            int curRow = 0, nextRow = 1;
            int[][] rows = new int[][] { new int[m + 1], new int[m + 1] };
            for (int j = 0; j <= m; ++j) rows[curRow][j] = j;

            for (int i = 1; i <= n; ++i)
            {
                rows[nextRow][0] = i;
                for (int j = 1; j <= m; ++j)
                {
                    int dist1 = rows[curRow][j] + 1;
                    int dist2 = rows[nextRow][j - 1] + 1;
                    int dist3 = rows[curRow][j - 1] +
                        (first[i - 1].Equals(second[j - 1]) ? 0 : 1);
                    rows[nextRow][j] = Math.Min(dist1, Math.Min(dist2, dist3));
                }
                // Swap the current and next rows
                if (curRow == 0)
                {
                    curRow = 1;
                    nextRow = 0;
                }

                else
                {
                    curRow = 0;
                    nextRow = 1;
                }
            }
            return rows[curRow][m];
        }
    }
}
