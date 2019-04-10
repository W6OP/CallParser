using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallParser
{
    public enum PrefixKind
    {
        pfNone,
        pfDXCC,
        pfProvince,
        pfStation,
        pfDelDXCC,
        pfOldPrefix,
        pfNonDXCC,
        pfInvalidPrefix,
        pfDelProvince,
        pfCity
    }

    public struct Hit
    {
        public string call;         //call sign as input
        public string prefix;       //what I determined the prefix to be - mostly for debugging
        public PrefixKind kind;     //kind
        public string country;       //country
        public string province;     //province
        public string city;         //city
        public string dxcc_entity;  //dxcc_entity
        public string cq;           //cq_zone
        public string itu;          //itu_zone
        public string continent;     //continent
        public string timeZone;     //time_zone
        public string latitude;     //lat
        public string longitude;    //long


    public Hit(Tuple<string, string> callAndprefix, PrefixData prefixData) {
            call = callAndprefix.Item1;
            prefix = callAndprefix.Item2;
            kind = prefixData.kind;
            country = prefixData.country;
            province = prefixData.province;
            city = prefixData.city;
            dxcc_entity = prefixData.dxcc_entity;
            cq = prefixData.cq;
            itu = prefixData.itu;
            continent = prefixData.continent;
            timeZone = prefixData.timeZone;
            latitude = prefixData.latitude;
            longitude = prefixData.longitude;
    }
}

public class CallLookUp
    {
    } // end class
}
