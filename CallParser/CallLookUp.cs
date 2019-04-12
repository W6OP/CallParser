using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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


        public Hit(Tuple<string, string> callAndprefix, PrefixData prefixData)
        {
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
        private List<PrefixData> _PrefixList;
        private List<PrefixData> _ChildPrefixList;
        private Hit[] _HitList;

        public CallLookUp(List<PrefixData> prefixList, List<PrefixData> childPrefixList)
        {
            _PrefixList = prefixList;
            _ChildPrefixList = childPrefixList;
        }

        public Hit[] LookUpCall(string callSign)
        {
            if (ValidateCallSign(callSign))
            {
                ProcessCallSign(callSign.ToUpper());
            }
            else
            {
                throw new Exception("Invalid call sign format"); // EMBELLISH
            }

            return _HitList;
        }

        private bool ValidateCallSign(string callSign)
        {
            // check for empty or null string
            if (string.IsNullOrEmpty(callSign)) { return false; }

            // check if first character is "/"
            if (callSign.IndexOf("/", 0, 1) == 0) { return false; }

            // can't be all numbers
            if (IsNumeric(callSign)) { return false; }

            // can't be all letters
            if (!isAlphaNumeric(callSign)) { return false; }

            // look for at least one number character
            if (!callSign.Where(x => Char.IsDigit(x)).Any()) { return false; }

            return true;
        }

        private void ProcessCallSign(string callSign)
        {

        }

        /// <summary>
        /// Check for non alpha numerics other than "/"
        /// </summary>
        /// <param name="strToCheck"></param>
        /// <returns></returns>
        private Boolean isAlphaNumeric(string strToCheck)
        {
            Regex rg = new Regex(@"^[a-zA-Z0-9\\]*$");
            return rg.IsMatch(strToCheck);
        }

        /// <summary>
        /// THIS IS DUPLICATE TO TWO CLASSES
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

    } // end class
}
