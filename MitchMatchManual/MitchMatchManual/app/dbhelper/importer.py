import csv
from app.models import Record, Set, SetMember

def ImportAllRecords(csvFile):

    num = 0
    with open(csvFile) as csvTextFile:
        csvReader = csv.reader(csvTextFile)

        for line in csvReader:
            print(line[0])

            if num > 0:
                record = Record(
                    EnterpriseId = int(line[0]),
                    LastName = line[1],
                    FirstName = line[2],
                    MiddleName = line[3],
                    Suffix = line[4],
                    DOB = line[5],
                    Gender = line[6],
                    SSN = line[7],
                    Address1 = line[8],
                    Address2 = line[9],
                    Zip = line[10],
                    MothersMaidenName = line[11],
                    MRN = line[12],
                    City = line[13],
                    State = line[14],
                    Phone = line[15],
                    Phone2 = line[16],
                    Email = line[17],
                    Alias = line[18])
                record.save()
            
            num = num + 1
    return
