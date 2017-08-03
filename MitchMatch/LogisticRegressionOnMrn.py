from sklearn.linear_model import LogisticRegression
import csv
from nltk import edit_distance
import numpy as np
import pickle
import sqlite3

VectorLength = 9

def computeDeltaVectorDb(record1, record2):
    firstName = handleRow(record1, record2, 1, '')
    lastName = handleRow(record1, record2, 3, '')
    social = handleRow(record1, record2, 6, '0')
    dob = handleRow(record1, record2, 7, '')
    phone = handleRow(record1, record2, 8, '')
    address1 = handleRow(record1, record2, 10, '')
    city = handleRow(record1, record2, 12, '')
    state = handleRow(record1, record2, 13, '')
    zip = handleRow(record1, record2, 14, '-1')

    vector = [firstName, lastName, social, dob, phone, address1, city, state, zip]
    return vector

#http://www.dummies.com/programming/big-data/data-science/using-logistic-regression-in-python-for-data-science/
def computeDeltaVector(allRows, i1, i2, isDb):
    firstRow = [str(r).strip() for r in allRows[i1]]
    secondRow = [str(r).strip() for r in allRows[i2]]

    if isDb:
        firstName = handleRow(firstRow, secondRow, 1, '')
        #middleName = handleRow(firstRow, secondRow, 1, '')
        lastName = handleRow(firstRow, secondRow, 3, '')
        #suffix = handleRow(firstRow, secondRow, 3, '')
        #gender = handleRow(firstRow, secondRow, 4, '')
        social = handleRow(firstRow, secondRow, 6, '0')
        dob = handleRow(firstRow, secondRow, 7, '')
        phone = handleRow(firstRow, secondRow, 8, '')
        #phone2 = handleRow(firstRow, secondRow, 8, '')
        address1 = handleRow(firstRow, secondRow, 10, '')
        #address2 = handleRow(firstRow, secondRow, 10, '')
        city = handleRow(firstRow, secondRow, 12, '')
        state = handleRow(firstRow, secondRow, 13, '')
        zip = handleRow(firstRow, secondRow, 14, '-1')
        #mothersMaidenName = handleRow(firstRow, secondRow, 14, '')
        #email = handleRow(firstRow, secondRow, 15, '')
        #alias = handleRow(firstRow, secondRow, 18, '')
    else:
        firstName = handleRow(firstRow, secondRow, 0, '')
        #middleName = handleRow(firstRow, secondRow, 1, '')
        lastName = handleRow(firstRow, secondRow, 2, '')
        #suffix = handleRow(firstRow, secondRow, 3, '')
        #gender = handleRow(firstRow, secondRow, 4, '')
        social = handleRow(firstRow, secondRow, 5, '0')
        dob = handleRow(firstRow, secondRow, 6, '')
        phone = handleRow(firstRow, secondRow, 7, '')
        #phone2 = handleRow(firstRow, secondRow, 8, '')
        address1 = handleRow(firstRow, secondRow, 9, '')
        #address2 = handleRow(firstRow, secondRow, 10, '')
        city = handleRow(firstRow, secondRow, 11, '')
        state = handleRow(firstRow, secondRow, 12, '')
        zip = handleRow(firstRow, secondRow, 13, '-1')
        #mothersMaidenName = handleRow(firstRow, secondRow, 14, '')
        #email = handleRow(firstRow, secondRow, 15, '')
        #alias = handleRow(firstRow, secondRow, 18, '')

    #goodVector = [firstName, middleName, lastName, suffix, gender, social,
    #dob, phone, phone2,
    #        address1, address2, city, state, zip, mothersMaidenName, email,
    #        alias]

    vector = [firstName, lastName, social, dob, phone, address1, city, state, zip]
    return vector

def handleRow(firstRow, secondRow, index, blankString):

    distance = 0

    col1 = str(firstRow[index]).lower()
    col2 = str(secondRow[index]).lower()

    distance = edit_distance(col1, col2)
    maxEditDistance = 0 
    if len(col1) > len(col2):
        maxEditDistance = len(col1)
    else:
        maxEditDistance = len(col2)

    if maxEditDistance > 0:
        distance = distance / maxEditDistance

    return distance

def Train(inputFile, savedOutput):
    with open(inputFile) as csvFile:
        csvReader = csv.reader(csvFile)
        lineNumber = 1
        allRows = [r for r in csvReader]

        numPairs = int(len(allRows) / 3)
        #vectors = np.zeros(shape=(numPairs * 2 - 1, VectorLength))
        #goodVectors = np.zeros(shape=(numPairs, 17))
        #badVectors = np.zeros(shape=(numPairs - 1, 17))
        goodVectors = []
        badVectors = []
        y = []

        print("Scanning...")
        for i in range(0, numPairs):
            if i % 10 == 0:
                print(str((i / numPairs) * 100) + "% done")
            #if len(allRows[i * 3]) == 19 and len(allRows[i * 3 + 1]) == 19:
            goodVector = computeDeltaVector(allRows, i * 3, i * 3 + 1)
            goodVectors.append(goodVector)
            for j in range(0, numPairs, 5):
                if i != j:
                    badVector = computeDeltaVector(allRows, i * 3, (j) * 3)
                    badVectors.append(badVector)

        vectors = np.zeros(shape=(len(goodVectors) + len(badVectors), VectorLength))
        m = 0
        for goodVector in goodVectors:
            vectors[m] = goodVector
            y.append(1)
            m = m + 1
        for badVector in badVectors:
            vectors[m] = badVector
            y.append(0)
            m = m + 1

        print("Learning...")
        logit = LogisticRegression()
        logit.fit(vectors, y)

        with open(savedOutput, "wb") as fout:
            pickle.dump(logit, fout)

def InsertRecordsIntoDatabase(inputFile, dbFile):
    with open(inputFile) as csvFile:
        csvReader = csv.reader(csvFile)

        count = 0
        possibles = []
        allRows = [r for r in csvReader]

        conn = sqlite3.connect(dbFile)
        cursor = conn.cursor()

        cursor.executemany("insert into records values (null,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", allRows)

        conn.commit()

    return

def MatchFromDb(dbFile, trainedFile):

    with open(trainedFile, "rb") as fout:
        logit = pickle.load(fout)

    conn = sqlite3.connect(dbFile)
    cursor = conn.cursor()

    cursor.execute("select * from records")
    allRecords = cursor.fetchall()

    for a in range(0, len(allRecords)):
        print(str(a / len(allRecords)) + "%")
        for b in range(a + 1, len(allRecords)):
            deltaVector = computeDeltaVector(allRecords, a, b, True)
                
            npDeltaVector = np.array(deltaVector)
            p = logit.predict_proba(npDeltaVector.reshape(1, -1))
            c = logit.predict(npDeltaVector.reshape(1, -1))

            if c == 1:
                aRecordId = int(allRecords[a][0])
                bRecordId = int(allRecords[b][0])
                cursor.execute("insert into matches values (null,?,?,?,?)", (aRecordId, bRecordId, p[0][1], 1))

        conn.commit()
    return


def Match(inputFile, trainedFile):

    logit = None
    with open(trainedFile, "rb") as fout:
        logit = pickle.load(fout)

    with open(inputFile) as csvFile:
        csvReader = csv.reader(csvFile)

        count = 0
        possibles = []
        allRows = [r for r in csvReader]
        
        indexToUse = 0
        print(allRows[indexToUse])
        print("========================")
        for i in range(0, len(allRows)):
            if i != indexToUse:
                deltaVector = computeDeltaVector(allRows, indexToUse, i, False)
                npDeltaVector = np.array(deltaVector)
                p = logit.predict_proba(npDeltaVector.reshape(1, -1))
                c = logit.predict(npDeltaVector.reshape(1, -1))

                if(c == 1):
                   print(p[0][1])
                   print(allRows[i])
                   print("")
    return

def MatchSets(dbFile, trainedFile, setsFile):
    enterpriseIdColumn = 18

    logit = None
    with open(trainedFile, "rb") as fout:
        logit = pickle.load(fout)

    conn = sqlite3.connect(dbFile)
    cursor = conn.cursor()

    allRecords = list(cursor.execute('select * from records'))
    enterpriseIds = [r[enterpriseIdColumn] for r in allRecords]

    with open(setsFile) as sets:
        allSetLines = sets.readlines()
        i = 0
        for setLine in allSetLines:
            bits = [int(b.replace('\n','')) for b in setLine.split(',')]

            if i % 500 == 0:
                print(str((i / len(allSetLines))*100) + "%")

            i = i + 1

            # is this a non-MRN match
            if bits[0] in enterpriseIds:
                for a in range(0, len(bits)):
                    aRecord = [r for r in allRecords if r[enterpriseIdColumn] == bits[a]][0]
                    for b in range(a + 1, len(bits)):
                        bRecord = [r for r in allRecords if r[enterpriseIdColumn] == bits[b]][0]
                        deltaVector = computeDeltaVectorDb(aRecord,bRecord)
                        npDeltaVector = np.array(deltaVector)
                        p = logit.predict_proba(npDeltaVector.reshape(1, -1))
                        c = logit.predict(npDeltaVector.reshape(1, -1))

                        cursor.execute("insert into matches values (null,?,?,?,1)", (aRecord[0], bRecord[0], p[0][1]))

    conn.commit()
    return

def main():
    #Train("c:/users/brush/desktop/logit/mrns.csv","c:/users/brush/desktop/logit/learnedModel.pickle")
    #Match("c:/users/brush/desktop/logit/remaining.csv","C:/users/brush/desktop/logit/learnedModel.pickle")
    #MatchFromDb("MitchMatch.db","c:/users/brush/desktop/logit/learnedModel.pickle")
    MatchSets("MitchMatch.db", "c:/users/brush/desktop/logit/learnedModel.pickle", "closedsets.txt")
    return

main()