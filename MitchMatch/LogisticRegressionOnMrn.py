from sklearn.linear_model import LogisticRegression
import csv
from nltk import edit_distance
import numpy as np
import pickle

#http://www.dummies.com/programming/big-data/data-science/using-logistic-regression-in-python-for-data-science/
def computeDeltaVector(allRows, i1, i2):
    firstRow = [r.strip() for r in allRows[i1]]
    secondRow = [r.strip() for r in allRows[i2]]

    firstName = handleRow(firstRow, secondRow, 0, '')
    middleName = handleRow(firstRow, secondRow, 1, '')
    lastName = handleRow(firstRow, secondRow, 2, '')
    suffix = handleRow(firstRow, secondRow, 3, '')
    gender = handleRow(firstRow, secondRow, 4, '')
    social = handleRow(firstRow, secondRow, 5, '0')
    dob = handleRow(firstRow, secondRow, 6, '')
    phone = handleRow(firstRow, secondRow, 7, '')
    phone2 = handleRow(firstRow, secondRow, 8, '')
    address1 = handleRow(firstRow, secondRow, 9, '')
    address2 = handleRow(firstRow, secondRow, 10, '')
    city = handleRow(firstRow, secondRow, 11, '')
    state = handleRow(firstRow, secondRow, 12, '')
    zip = handleRow(firstRow, secondRow, 13, '-1')
    mothersMaidenName = handleRow(firstRow, secondRow, 14, '')
    email = handleRow(firstRow, secondRow, 15, '')
    alias = handleRow(firstRow, secondRow, 18, '')

    goodVector = [firstName, middleName, lastName, suffix, gender, social, dob, phone, phone2, 
            address1, address2, city, state, zip, mothersMaidenName, email, alias]
    return goodVector

def handleRow(firstRow, secondRow, index, blankString):

    distance = 0

    col1 = firstRow[index].lower()
    col2 = secondRow[index].lower()

    if col1 == blankString and col2 == blankString:
        distance = -2
    elif col1 == blankString or col2 == blankString:
        distance = -1
    else:
        distance = edit_distance(col1, col2)

    return distance

def Train(inputFile, savedOutput):
    with open(inputFile) as csvFile:
        csvReader = csv.reader(csvFile)
        lineNumber = 1
        allRows = [r for r in csvReader]

        numPairs = int(len(allRows) / 3)
        vectors = np.zeros(shape=(numPairs * 2 - 1, 17))
        #goodVectors = np.zeros(shape=(numPairs, 17))
        #badVectors = np.zeros(shape=(numPairs - 1, 17))
        y = []

        print("Scanning...")
        for i in range(0, numPairs):
            if len(allRows[i * 3]) == 19 and len(allRows[i * 3 + 1]) == 19:
                goodVector = computeDeltaVector(allRows, i * 3, i * 3 + 1)
                vectors[i * 2] = goodVector 
                y.append(1)
                #goodVectors[i] = goodVector

                if i < numPairs - 1:
                    badVector = computeDeltaVector(allRows, i * 3, (i + 1) * 3)
                    #badVectors[i] = badVector
                    vectors[i * 2 + 1] = badVector
                    y.append(0)

        print("Learning...")
        logit = LogisticRegression()
        logit.fit(vectors, y)

        with open(savedOutput, "wb") as fout:
            pickle.dump(logit, fout)


def Match(inputFile, trainedFile):

    logit = None
    with open(trainedFile, "rb") as fout:
        logit = pickle.load(fout)

    with open(inputFile) as csvFile:
        csvReader = csv.reader(csvFile)

        possibles = []
        allRows = [r for r in csvReader]
        for i in range(1, len(allRows)):
            rowA = allRows[0]
            rowB = allRows[i]
            deltaVector = computeDeltaVector(allRows, 0, i)
            npDeltaVector = np.array(deltaVector)
            #p = logit.predict_proba(npDeltaVector)
            c = logit.predict(npDeltaVector.reshape(1, -1))

            if(c == 1):
                possibles.append(allRows[i])

    return

def main():
    #Train("c:/users/brush/desktop/mrns.csv",
    #"c:/users/brush/desktop/learnedModel.pickle")
    Match("c:/users/brush/desktop/logit/remaining.csv", "C:/users/brush/desktop/logit/learnedModel.pickle")
    return




main()