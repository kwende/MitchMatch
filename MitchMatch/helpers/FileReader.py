import csv

def LoadAndTrimToLastTenPercent(filePath):
    trimmedRows = []
    with open(filePath, 'r') as csvfile:
        rows = csv.reader(csvfile)
        trimmedRows = [row for row in rows if row[6].lower() == 'm' or row[6].lower() == 'f']
    return trimmedRows