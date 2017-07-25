using challenge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucasPlayground.Ben
{
    class EditDistance
    {
        public static double ComputeDistanceForRecordPair(row row1, row row2)
        {
            double distance1 = ComputeNormalized(row1.LAST, row2.LAST);
            double distance2 = ComputeNormalized(row1.FIRST, row2.FIRST);
            double distance3 = ComputeNormalized(row1.SSN.ToString(), row2.SSN.ToString());
            double distance4 = ComputeNormalized(row1.DOB.ToShortDateString(), row2.DOB.ToShortDateString()); 
            return (distance1 + distance2 + distance3 + distance4) / 4.0; 
        }

        public static double ComputeNormalized(string first, string second)
        {
            int editDistance = Compute(first, second);
            int maxLength = first.Length > second.Length ? first.Length : second.Length;

            return editDistance / (maxLength * 1.0);  
        }

        /// <SUMMARY>Computes the Levenshtein Edit Distance between two enumerables.</SUMMARY>

        /// <TYPEPARAM name="T">The type of the items in the enumerables.</TYPEPARAM>

        /// <PARAM name="x">The first enumerable.</PARAM>

        /// <PARAM name="y">The second enumerable.</PARAM>

        /// <RETURNS>The edit distance.</RETURNS>

        public static int Compute(string first, string second)
        {



            // Get the length of both.  If either is 0, return

            // the length of the other, since that number of insertions

            // would be required.

            int n = first.Length, m = second.Length;

            if (n == 0) return m;

            if (m == 0) return n;


            // Rather than maintain an entire matrix (which would require O(n*m) space),

            // just store the current row and the next row, each of which has a length m+1,

            // so just O(m) space. Initialize the current row.

            int curRow = 0, nextRow = 1;

            int[][] rows = new int[][] { new int[m + 1], new int[m + 1] };

            for (int j = 0; j <= m; ++j) rows[curRow][j] = j;


            // For each virtual row (since we only have physical storage for two)

            for (int i = 1; i <= n; ++i)

            {

                // Fill in the values in the row

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


            // Return the computed edit distance

            return rows[curRow][m];

        }
    }
}
