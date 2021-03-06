﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndressAddress.DataTypes
{
    public class Data
    {
        public AddressSuffixes Suffixes { get; set; }
        public string[] FinalDataSet { get; set; }
        public string[] NewYorkStateStreetNamesWithZip { get; set; }
        public string[] UnknownAddresses { get; set; }
        public string[] HomelessAddresses { get; set; }
        public Dictionary<string, string> Abbreviations { get; set; }
        public Dictionary<string, string> AbbreviationsShortened { get; set; }
        public Dictionary<string, string> SuffixReplacementKey { get; set; }
        public Dictionary<string, Address> KnownCenters { get; set; }
        public BKTree StreetNameBKTree { get; set; }
        public BKTree CityNameBKTree { get; set; }
        public Dictionary<string, List<string>> AlternateSuffixList { get; set; }
        public Dictionary<StreetNameAndCity, List<int>> StreetNameCity2Zips { get; set; }
        public Dictionary<StreetNameAndZip, List<string>> StreetNameZip2Cities { get; set; }
        public List<StreetName> StreetData = new List<StreetName>();
        public List<string> KnownCities = new List<string>();
        public List<StateOfNewYorkAddressRange> NYCityStreets { get; set; }
        public string[] NYStateStreets { get; set; }
    }
}
