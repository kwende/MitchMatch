from helpers.FileReader import LoadAndTrimToLastTenPercent
import numpy as np
from sklearn.cluster import MeanShift, estimate_bandwidth
import pickle

SSNIndex = 7

def LoadSocialSecurityNumbers(results):
    return [[int(i) for i in r[7].replace('-','')] for r in results]


def main():
    results = LoadAndTrimToLastTenPercent("C:/Users/Ben/Desktop/FInalDataset.csv")
    social = [l for l in LoadSocialSecurityNumbers(results) if len(l) > 0]

    bandwidth = estimate_bandwidth(X, quantile=0.2, n_samples=500)

    ms = MeanShift(bandwidth=bandwidth, bin_seeding=True)
    ms.fit(social)

    with open('c:/users/ben/desktop/learned.pickle', 'wb') as fout:
        pickle.dump(ms, fout)
    return

main()

