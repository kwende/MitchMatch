using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress
{
    public static class StringUtility
    {
        public static bool EndsWith(string sourceString, string searchString)
        {
            bool endsWith = false;

            if (sourceString == "" || searchString == "")
            {
                endsWith = false;
            }
            else if (sourceString.Length < searchString.Length)
            {
                endsWith = false;
            }
            else if (sourceString == searchString)
            {
                endsWith = true;
            }
            else
            {
                endsWith = true;
                for (int c = sourceString.Length - 1, d = searchString.Length - 1; d >= 0; c--, d--)
                {
                    if (sourceString[c] != searchString[d])
                    {
                        endsWith = false;
                        break;
                    }
                }
            }

            return endsWith;
        }

        public static bool Contains(string sourceString, string searchString)
        {
            bool contains = false;

            if (sourceString == "" || searchString == "")
            {
                contains = false;
            }
            else if (sourceString.Length < searchString.Length)
            {
                contains = false;
            }
            else if (sourceString == searchString)
            {
                contains = true;
            }
            else
            {
                int searchStringLength = searchString.Length;
                int sourceStringLength = sourceString.Length;
                int lastIndexToCheck = sourceStringLength - searchStringLength;

                int matchCount = 0;
                for (int c = 0; c < lastIndexToCheck && matchCount < searchStringLength; c++)
                {
                    char sourceChar = sourceString[c];
                    char searchChar = searchString[0];

                    if (sourceChar == searchChar)
                    {
                        matchCount++;
                        for (int d = 1; d < searchString.Length; d++)
                        {
                            sourceChar = sourceString[c + d];
                            searchChar = searchString[d];
                            if (searchChar == sourceChar)
                            {
                                matchCount++;
                            }
                            else
                            {
                                matchCount = 0;
                                break;
                            }
                        }
                    }
                }

                contains = matchCount == searchStringLength;
            }

            return contains;
        }
    }
}
