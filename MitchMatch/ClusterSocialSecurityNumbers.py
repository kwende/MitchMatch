from helpers.FileReader import LoadAndTrimToLastTenPercent
import numpy as np
from sklearn.cluster import MeanShift, estimate_bandwidth, DBSCAN
import pickle
from sklearn import metrics
from sklearn.preprocessing import StandardScaler
from math import *

SSNIndex = 7

def LoadSocialSecurityNumbers(results):
    nonZero = [r for r in results if r[7] != '']
    asStrings = [n[SSNIndex].replace('-','') for n in nonZero]
    asArrays = [[int(i) for i in s] for s in asStrings]
    return asArrays, asStrings

def MeanShiftClustering(social):
    bandwidth = estimate_bandwidth(social, quantile=0.3, n_samples=5000)

    ms = MeanShift(bandwidth=bandwidth, bin_seeding=True)
    ms.fit(social)

    return ms

def WriteToDisk(labels, rawSocialSecurityNumbers):
    # DO THEM ALL
    for i in range(0, len(labels)-1):
        label = labels[i]
        if i % 100 == 0:
            print(str(i) + " of " + str(len(labels)-1))
        with open('c:/users/ben/desktop/group_' + str(label) + '.csv', 'a') as fout:
            fout.write(rawSocialSecurityNumbers[i] + "\n")

def DbScanClustering(rawSocialSecurityNumbers, social):
    blanks = len([s for s in rawSocialSecurityNumbers if s == ''])

    transformedSocial = StandardScaler().fit_transform(social)
    db = DBSCAN(eps=0.3).fit(transformedSocial)
                             
    labels = db.labels_

    # Number of clusters in labels, ignoring noise if present.
    n_clusters_ = len(set(labels)) - (1 if -1 in labels else 0)

    print("number of dbscan clusters: " + str(n_clusters_))

    #https://stackoverflow.com/questions/40491707/get-cluster-members-elements-clustering-with-scikit-learn-dbscan

    # TRY CLUSTERING THE NOISE
    newList = np.array([social[l] for l in range(0, len(labels)-1) if labels[l] == -1])
    rawSocialSecurityNumbers = [rawSocialSecurityNumbers[l] for l in range(0, len(labels)-1) if labels[l] == -1]

    transformedNewList = StandardScaler().fit_transform(newList)
    db = DBSCAN(eps=0.5).fit(transformedNewList)

    newLabels = db.labels_

    # Number of clusters in labels, ignoring noise if present.
    n_clusters_ = len(set(newLabels)) - (1 if -1 in newLabels else 0)

    WriteToDisk(newLabels, rawSocialSecurityNumbers)
    return

def main():
    results = LoadAndTrimToLastTenPercent("C:/Users/Ben/Desktop/FInalDataset.csv")

    asArrays, asStrings = LoadSocialSecurityNumbers(results)

    social = np.array(asArrays)
    DbScanClustering(asStrings, social)

    #with open('c:/users/ben/desktop/learned.pickle', 'wb') as fout:
    #    pickle.dump(ms, fout)
    #return

main()

