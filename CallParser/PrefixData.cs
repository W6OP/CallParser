using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallParser
{
    [Serializable]
    public class PrefixData
    {
        public PrefixData()
        {
            expandedMaskSetList = new HashSet<string>();
            primaryMaskSets = new HashSet<string>();
            secondaryMaskSets = new HashSet<string>();  //[[Set<String>]]()
        }

        enum CharacterType
        {
            [Description("#")]
            numeric,
            [Description("@")]
            alphabetical,
            [Description("?")]
            alphanumeric,
            [Description("-")]
            dash,
            [Description(".")]
            dot,
            [Description("/")]
            slash,
            [Description("")]
            empty
        }

        public string mainPrefix = "";             //label ie: 3B6
        public string fullPrefix = "";             // ie: 3B6.3B7
        public PrefixKind kind = PrefixKind.pfNone;    //kind
        public string country = "";                //country
        public string province = "";              //province
        public string city = "";                    //city
        public string dxcc_entity = "";                   //dxcc_entity
        public string cq = "";                     //cq_zone
        public string itu = "";                    //itu_zone
        public string continent = "";              //continent
        public string timeZone = "";               //time_zone
        public string latitude = "0.0";            //lat
        public string longitude = "0.0";           //long


        public bool isParent = false;
        public bool hasChildren = false;
        public List<PrefixData> children = new List<PrefixData>();
        // expanded masks
        HashSet<string> expandedMaskSetList = new HashSet<string>();    //: [[Set<String>]]
        public HashSet<string> primaryMaskSets = new HashSet<string>();        //: [[Set<String>]]
        public HashSet<string> secondaryMaskSets = new HashSet<string>();      //: [[Set<String>]]
        public List<String> rawMasks = new List<String>();

        bool adif = false;
        public string wae = "";
        public string wap = "";
        public string admin1 = "";
        public string admin2 = "";
        public string startDate = "";
        public string endDate = "";
        bool isIota = false; // implement
        string comment = "";

        int id = 1;
        // for debugging
        int maskCount = 0;
        int counter = 0;

        string[] alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public void SetMainPrefix(string fullPrefix)
        {
            if (fullPrefix.IndexOf(".") != -1)
            {
                mainPrefix = fullPrefix.Substring(0, fullPrefix.IndexOf("."));
            }
            else
            {
                mainPrefix = fullPrefix;
            }
        }

        public void SetDXCC(PrefixKind prefixKind)
        {
            kind = prefixKind;

            if (prefixKind == PrefixKind.pfDXCC)
            {
                adif = true;
                isParent = true;
            }
        }

        public void StoreMask(string mask)
        {
            rawMasks.Add(mask);
            ExpandMask(mask);
        }

        public void ExpandMask(string mask)
        {
        }

        public void ParseMask(string[] components)
        {
        }

        public HashSet<string> buildRange(string currentCharacter, string nextCharacter)
        {


            return new HashSet<string>();
        }

        public HashSet<string> getMetaMaskSet(string character)
        {
            return new HashSet<string>();
        }



    } // end class
}
