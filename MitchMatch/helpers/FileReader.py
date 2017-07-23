import csv

SSNIndex = 7

def LoadAndTrimToLastTenPercent(filePath):
    trimmedRows = []
    with open(filePath, 'r') as csvfile:
        rows = csv.reader(csvfile)
        trimmedRows = [row for row in rows if row[6].lower() == 'm' or row[6].lower() == 'f']
    return trimmedRows

def LoadNonZeroSocialSecurityNumbers(results):
    nonZero = [r for r in results if r[7] != '']
    asStrings = [n[SSNIndex].replace('-','') for n in nonZero]
    asArrays = [[int(i) for i in s] for s in asStrings]
    return asArrays, asStrings