import csv
from math import *
from helpers.FileReader import LoadAndTrimToLastTenPercent

LastNameIndex = 1
FirstNameIndex = 2
SSNIndex = 7

def StupidVerbatimMatchSocialSecurityNumber(results):
    exactMatches = []

    socials = [row[SSNIndex].lower() for row in results]

    c = 0
    m = 0
    while True:
        if c % 100 == 0:
            print(str(c) + " of " + str(len(socials)))
        c = c + 1

        indicesToDelete = []
        for k in range(m, len(socials) - 1):
            if m != k and socials[m] == socials[k]:
                indicesToDelete = [k, m] #put in reverse order
                break

        if not len(indicesToDelete) == 0:
            exactMatches.append(socials[m])
            # remove all instances
            for indexToDelete in indicesToDelete:
                del socials[indexToDelete]
        else:
            # there is no match, move to the next index
            m = m + 1
            if m > len(socials)-1:
                break

    return exactMatches

def StupidVerbatimMatchFirstNameLastName(results):
    exactMatches = []

    firstLastNames = [row[LastNameIndex].lower() + row[FirstNameIndex].lower() for row in results]

    c = 0
    m = 0
    matchCounts = {}
    while True:
        if c % 100 == 0:
            print(str(c) + " of " + str(len(firstLastNames)))
        c = c + 1

        indicesToDelete = []
        for k in range(m, len(firstLastNames) - 1):
            if m != k and firstLastNames[m] == firstLastNames[k]:
                indicesToDelete = [k] 

        if len(indicesToDelete) > 0:
            indicesToDelete.append(m)
            matchCounts[firstLastNames[m]] = len(indicesToDelete)

        if not len(indicesToDelete) == 0:
            exactMatches.append(firstLastNames[m])
            # remove all instances
            for indexToDelete in sorted(indicesToDelete, reverse=True):
                del firstLastNames[indexToDelete]
        else:
            # there is no match, move to the next index
            m = m + 1
            if m > len(firstLastNames)-1:
                break

    return matchCounts

def main():
    results = LoadAndTrimToLastTenPercent("C:/Users/Ben/Desktop/FInalDataset.csv")

    #exactMatches = StupidVerbatimMatchFirstNameLastName(results)
    #exactMatches = StupidVerbatimMatchSocialSecurityNumber(results)

    with open('c:/users/ben/desktop/output.txt', "w") as fout:
        for name,count in exactMatches.items():
            fout.write(name + "," + str(count) + "\n")

    #print("Found " + str(len(exactMatches)) + " in " + str(len(results)) + "
    #results")

    return

main()