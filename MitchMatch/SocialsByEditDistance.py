import nltk
import numpy as np
from helpers.FileReader import LoadAndTrimToLastTenPercent, LoadNonZeroSocialSecurityNumbers
import time

def main():
    asArrays, asStrings = LoadNonZeroSocialSecurityNumbers(LoadAndTrimToLastTenPercent("C:/Users/Ben/Desktop/FInalDataset.csv"))

    c = 0
    allAverages = []
    while True:
        startTime = time.time()
        print(str(c) + " of " + str(len(asStrings)))
        c = c + 1

        matchedIndices = []
        toMatchString = asStrings[0]

        # find all those with edit distance 1 or less
        for k in range(1, len(asStrings)):
            #if k % 1000 == 0:
            #    print("\t" + str(k) + " of " + str(len(asStrings)))
            if nltk.edit_distance(toMatchString, asStrings[k]) <= 2:
                matchedIndices.append(k)

        # if we found some matches, include self to the group.
        if len(matchedIndices) > 0:
            # go through and put into group
            toWrite = [asStrings[i] for i in matchedIndices]
            print("\t" + str(len(toWrite)) + " matches found.")
            with open("c:/users/ben/desktop/matches/match_" + toMatchString + ".csv", "w") as fout:
                for m in toWrite:
                    fout.write(m + "\n")

        del asStrings[0]
        if len(asStrings) == 0:
            break

        endTime = time.time()
        averageTimePerItem = (endTime - startTime) / len(asStrings)
        allAverages.append(averageTimePerItem)

        updatedAverage = np.mean(allAverages)
        x = len(asStrings)
        daysLeft = (updatedAverage * (x+1)*x/2) / 60 / 60 / 24
        print("\tI'll be done in " + str(daysLeft) + " days")

    print("Done...saving to disk...")

    return

nltk.data.path.append('C:/Users/Ben/AppData/Roaming/nltk_data')
main()


